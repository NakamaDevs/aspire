// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;
using Aspire.Hosting.RemoteHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.CodeGeneration.Elixir.Tests;

/// <summary>
/// Runs one round trip: a real RemoteHost JSON-RPC server in this process, the generated Elixir SDK
/// in a temporary directory, and an <c>apphost.exs</c> script that the <c>elixir</c> executable runs
/// against that server.
/// </summary>
/// <remarks>
/// <para>
/// The server is the production one. <see cref="RemoteHostServer"/> builds the host, and this class
/// replaces two scoped registrations with capturing factories so a test can read the objects the
/// dispatcher created. <c>HandleRegistry</c> and <c>CancellationTokenRegistry</c> are internal to
/// <c>Aspire.Hosting.RemoteHost</c>, which makes its internals visible only to its own test project,
/// so this class reaches them by reflection instead of by a new <c>InternalsVisibleTo</c>.
/// </para>
/// <para>
/// A connection scope disposes its <c>HandleRegistry</c> when the guest disconnects. Every script
/// therefore stops at a handshake: it prints <see cref="ReadyMarker"/> and waits for one line on
/// standard input. The test reads the handles while the guest is still connected, then releases the
/// script.
/// </para>
/// </remarks>
internal sealed class ElixirRoundTripHost : IAsyncDisposable
{
    /// <summary>The line a script prints when the host may inspect the model.</summary>
    internal const string ReadyMarker = "ASPIRE_TEST_READY";

    /// <summary>The prefix of a <c>key=value</c> line that a script reports to the test.</summary>
    internal const string ValuePrefix = "ASPIRE_TEST ";

    private static readonly Assembly s_remoteHostAssembly = typeof(RemoteHostServer).Assembly;
    private static readonly Type s_handleRegistryType = GetRemoteHostType("Aspire.Hosting.RemoteHost.Ats.HandleRegistry");
    private static readonly Type s_cancellationTokenRegistryType = GetRemoteHostType("Aspire.Hosting.RemoteHost.CancellationTokenRegistry");

    private static readonly MethodInfo s_containsMethod = s_handleRegistryType.GetMethod("Contains", [typeof(string)])!;
    private static readonly MethodInfo s_getObjectMethod = s_handleRegistryType
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method => method.Name == "GetObject" && !method.IsGenericMethod);
    private static readonly MethodInfo s_tryGetTokenMethod = s_cancellationTokenRegistryType.GetMethod("TryGetToken")!;

    private readonly IHost _host;
    private readonly ITestOutputHelper _outputHelper;
    private readonly ConcurrentBag<object> _handleRegistries;
    private readonly ConcurrentBag<object> _cancellationTokenRegistries;

    private ElixirRoundTripHost(
        IHost host,
        string socketPath,
        string authToken,
        ITestOutputHelper outputHelper,
        ConcurrentBag<object> handleRegistries,
        ConcurrentBag<object> cancellationTokenRegistries)
    {
        _host = host;
        _outputHelper = outputHelper;
        _handleRegistries = handleRegistries;
        _cancellationTokenRegistries = cancellationTokenRegistries;
        SocketPath = socketPath;
        AuthToken = authToken;
    }

    /// <summary>The Unix domain socket the guest connects to.</summary>
    public string SocketPath { get; }

    /// <summary>The token the guest sends in the <c>authenticate</c> request.</summary>
    public string AuthToken { get; }

    /// <summary>
    /// Starts the JSON-RPC server on a short socket path. macOS limits a Unix socket path to about
    /// 104 bytes and the system temporary directory is already long, so the path stays under
    /// <c>/tmp</c>.
    /// </summary>
    public static async Task<ElixirRoundTripHost> StartAsync(ITestOutputHelper outputHelper, CancellationToken cancellationToken = default)
    {
        var socketPath = $"/tmp/aspire-ex-{Guid.NewGuid():N}.sock";
        var authToken = Guid.NewGuid().ToString("N");

        var createBuilder = typeof(RemoteHostServer).GetMethod("CreateBuilder", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RemoteHostServer.CreateBuilder was not found.");

        string[] args =
        [
            $"REMOTE_APP_HOST_SOCKET_PATH={socketPath}",
            $"ASPIRE_REMOTE_APPHOST_TOKEN={authToken}",
            "Logging:LogLevel:Default=Warning"
        ];

        var applicationBuilder = (HostApplicationBuilder)createBuilder.Invoke(null, [args])!;

        var handleRegistries = new ConcurrentBag<object>();
        var cancellationTokenRegistries = new ConcurrentBag<object>();

        // A later registration of the same service type wins in Microsoft.Extensions.DependencyInjection,
        // so these replace the registrations that RemoteHostServer.ConfigureServices added.
        applicationBuilder.Services.AddScoped(s_handleRegistryType, _ =>
        {
            var registry = Activator.CreateInstance(s_handleRegistryType, nonPublic: true)!;
            handleRegistries.Add(registry);
            return registry;
        });

        applicationBuilder.Services.AddScoped(s_cancellationTokenRegistryType, _ =>
        {
            var registry = Activator.CreateInstance(s_cancellationTokenRegistryType, nonPublic: true)!;
            cancellationTokenRegistries.Add(registry);
            return registry;
        });

        var host = applicationBuilder.Build();
        var roundTripHost = new ElixirRoundTripHost(host, socketPath, authToken, outputHelper, handleRegistries, cancellationTokenRegistries);

        await host.StartAsync(cancellationToken);
        await roundTripHost.WaitForSocketAsync(cancellationToken);

        return roundTripHost;
    }

    /// <summary>
    /// Generates the Elixir SDK for the real <c>Aspire.Hosting</c> assembly plus the shared ATS test
    /// types, and writes every file into <paramref name="directory"/>.
    /// </summary>
    public static async Task GenerateSdkAsync(string directory, CancellationToken cancellationToken = default)
    {
        var testAssembly = typeof(TestRedisResource).Assembly;
        var hostingAssembly = typeof(DistributedApplication).Assembly;
        var context = AtsCapabilityScanner.ScanAssemblies([hostingAssembly, testAssembly]).ToAtsContext();

        var files = new AtsElixirCodeGenerator().GenerateDistributedApplication(context);

        foreach (var (name, content) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, name), content, cancellationToken);
        }
    }

    /// <summary>
    /// Returns the .NET object behind a handle identifier that the guest printed.
    /// </summary>
    public T GetHandleObject<T>(string handleId) where T : class
    {
        foreach (var registry in _handleRegistries)
        {
            if ((bool)s_containsMethod.Invoke(registry, [handleId])!)
            {
                var value = s_getObjectMethod.Invoke(registry, [handleId])!;
                if (value is T typed)
                {
                    return typed;
                }

                throw new InvalidOperationException(
                    $"Handle '{handleId}' holds {value.GetType().FullName}, and the test expected {typeof(T).FullName}.");
            }
        }

        throw new InvalidOperationException($"Handle '{handleId}' is not registered in any connection scope.");
    }

    /// <summary>
    /// Returns whether the host cancelled the cancellation token that the guest created.
    /// </summary>
    public bool IsCancellationRequested(string tokenId)
    {
        foreach (var registry in _cancellationTokenRegistries)
        {
            var arguments = new object?[] { tokenId, null };
            if ((bool)s_tryGetTokenMethod.Invoke(registry, arguments)!)
            {
                return ((CancellationToken)arguments[1]!).IsCancellationRequested;
            }
        }

        throw new InvalidOperationException($"Cancellation token '{tokenId}' is not registered in any connection scope.");
    }

    /// <summary>
    /// Starts <c>elixir &lt;scriptFileName&gt;</c> in <paramref name="workingDirectory"/> with the
    /// socket path and the authentication token in the environment.
    /// </summary>
    public ElixirScriptRun StartScript(string workingDirectory, string scriptFileName)
    {
        var startInfo = new ProcessStartInfo("elixir")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(scriptFileName);
        startInfo.Environment["REMOTE_APP_HOST_SOCKET_PATH"] = SocketPath;
        startInfo.Environment["ASPIRE_REMOTE_APPHOST_TOKEN"] = AuthToken;

        return new ElixirScriptRun(startInfo, _outputHelper);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"Failed to stop the RemoteHost server: {ex}");
        }

        _host.Dispose();

        try
        {
            if (File.Exists(SocketPath))
            {
                File.Delete(SocketPath);
            }
        }
        catch (IOException)
        {
            // The server deletes the socket file on disposal, so a race here is not a test failure.
        }
    }

    private async Task WaitForSocketAsync(CancellationToken cancellationToken)
    {
        // JsonRpcServer binds the socket in a background service, so the guest can start only after
        // the socket file exists.
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!File.Exists(SocketPath))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"The RemoteHost server did not create the socket at {SocketPath}.");
            }

            await Task.Delay(25, cancellationToken);
        }
    }

    private static Type GetRemoteHostType(string fullName)
    {
        return s_remoteHostAssembly.GetType(fullName)
            ?? throw new InvalidOperationException($"{fullName} was not found in Aspire.Hosting.RemoteHost.");
    }
}

/// <summary>
/// One running <c>apphost.exs</c>. The run stops at the ready marker until <see cref="Release"/>.
/// </summary>
internal sealed class ElixirScriptRun : IAsyncDisposable
{
    private readonly Process _process;
    private readonly ITestOutputHelper _outputHelper;
    private readonly StringBuilder _standardOutput = new();
    private readonly StringBuilder _standardError = new();
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task<int> _exit;
    private bool _released;

    public ElixirScriptRun(ProcessStartInfo startInfo, ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        _process.OutputDataReceived += (_, e) => OnStandardOutput(e.Data);
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_standardError)
                {
                    _standardError.AppendLine(e.Data);
                }
            }
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException("Failed to start elixir.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _exit = WaitForExitCoreAsync();
    }

    /// <summary>The captured standard output and standard error.</summary>
    public string Output
    {
        get
        {
            string output;
            string error;
            lock (_standardOutput)
            {
                output = _standardOutput.ToString();
            }

            lock (_standardError)
            {
                error = _standardError.ToString();
            }

            return $"--- stdout ---\n{output}--- stderr ---\n{error}";
        }
    }

    /// <summary>
    /// Returns a value that the script reported with an <c>ASPIRE_TEST key=value</c> line.
    /// </summary>
    public string Value(string key)
    {
        if (_values.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"The script did not report '{key}'.\n{Output}");
    }

    /// <summary>
    /// Waits until the script prints the ready marker. It throws with the captured output when the
    /// script exits first or does not reach the marker in time.
    /// </summary>
    public async Task WaitForReadyAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(_ready.Task, _exit, Task.Delay(timeout));

        if (completed == _ready.Task)
        {
            return;
        }

        if (completed == _exit)
        {
            throw new InvalidOperationException(
                $"The Elixir script exited with code {await _exit} before it reported '{ElixirRoundTripHost.ReadyMarker}'.\n{Output}");
        }

        Kill();
        throw new TimeoutException(
            $"The Elixir script did not report '{ElixirRoundTripHost.ReadyMarker}' in {timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s.\n{Output}");
    }

    /// <summary>Lets the script continue past the handshake.</summary>
    public void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;

        try
        {
            _process.StandardInput.WriteLine();
            _process.StandardInput.Flush();
        }
        catch (IOException)
        {
            // The script already exited. The exit-code assertion reports the real failure.
        }
    }

    /// <summary>Waits for the process and returns its exit code.</summary>
    public async Task<int> WaitForExitAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(_exit, Task.Delay(timeout));
        if (completed != _exit)
        {
            Kill();
            throw new TimeoutException(
                $"The Elixir script did not exit in {timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s.\n{Output}");
        }

        return await _exit;
    }

    public async ValueTask DisposeAsync()
    {
        Kill();

        try
        {
            await _exit.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            _outputHelper.WriteLine("The Elixir script did not exit after it was killed.");
        }

        _outputHelper.WriteLine(Output);
        _process.Dispose();
    }

    private void OnStandardOutput(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_standardOutput)
        {
            _standardOutput.AppendLine(line);
        }

        if (string.Equals(line.Trim(), ElixirRoundTripHost.ReadyMarker, StringComparison.Ordinal))
        {
            _ready.TrySetResult();
            return;
        }

        if (line.StartsWith(ElixirRoundTripHost.ValuePrefix, StringComparison.Ordinal))
        {
            var pair = line[ElixirRoundTripHost.ValuePrefix.Length..];
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0)
            {
                _values[pair[..separator]] = pair[(separator + 1)..];
            }
        }
    }

    private async Task<int> WaitForExitCoreAsync()
    {
        await _process.WaitForExitAsync();

        // WaitForExitAsync can return before the redirected readers drain, and the assertions read
        // the reported values, so wait for the streams too.
        _process.WaitForExit();
        return _process.ExitCode;
    }

    private void Kill()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
    }
}
