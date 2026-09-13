// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;

namespace Aspire.Hosting.Dart.Tests;

/// <summary>
/// Starts the two Dart presets from a project that their own command-line tool scaffolds.
/// </summary>
/// <remarks>
/// Every test in this class carries the <c>outerloop</c> trait. Each one takes minutes, because it
/// scaffolds a complete project and fetches a large dependency tree from pub, so the tests stay out
/// of the normal test run.
/// </remarks>
[Trait("outerloop", "true")]
[RequiresTools(["dart"])]
public class DartOuterloopTests(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_timeout =
        TimeSpan.FromSeconds(PlatformDetection.IsRunningOnCI ? 900 : 600);

    /// <summary>The time that one scaffolding command is allowed to take.</summary>
    private static readonly TimeSpan s_scaffoldTimeout = TimeSpan.FromSeconds(600);

    /// <summary>
    /// Confirms that a scaffolded Serverpod server starts against the PostgreSQL database and the
    /// Redis cache that Aspire supplies, and that Aspire allocates the three Serverpod endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>serverpod create</c> writes a development configuration that points at the ports of its own
    /// Docker Compose file. <see cref="DartHostingExtensions.WithServerpodDatabase{T}"/> and
    /// <see cref="DartHostingExtensions.WithServerpodRedis{T}"/> set the
    /// <c>SERVERPOD_DATABASE_*</c> and <c>SERVERPOD_REDIS_*</c> variables, which win over that file.
    /// The server therefore reaches the containers that Aspire started.
    /// </para>
    /// <para>
    /// A Serverpod server answers status 200 on the root path of the API endpoint, so one request
    /// proves that the server started, opened the database, and listens on the allocated port.
    /// </para>
    /// </remarks>
    [Fact]
    [OuterloopTest("Scaffolds a complete Serverpod project and starts a PostgreSQL and a Redis container.")]
    [RequiresTools(["serverpod"])]
    [RequiresFeature(TestFeature.ContainerRuntime)]
    public async Task ServerpodAppStartsWithPostgresAndRedis()
    {
        const string ProjectName = "aspiretest";

        using var workspace = new TempScaffoldDirectory();
        using var cts = new CancellationTokenSource(s_timeout);

        // `serverpod create` writes a pub workspace with three packages. The server package is the one
        // that Aspire starts.
        RunTool(
            "serverpod",
            workspace.Path,
            ["create", "--no-interactive", "--no-analytics", "--template", "server", "--force", ProjectName],
            outputHelper);

        var serverDirectory = Path.Combine(workspace.Path, ProjectName, $"{ProjectName}_server");
        Assert.True(File.Exists(Path.Combine(serverDirectory, "pubspec.yaml")), $"'{serverDirectory}' holds no pubspec.yaml.");

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var database = builder.AddPostgres("pg").AddDatabase("appdb");
        var cache = builder.AddRedis("cache");

        var api = builder.AddServerpodApp($"{ProjectName}-server", serverDirectory)
            .WithServerpodDatabase(database)
            .WithServerpodRedis(cache);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(
            $"{ProjectName}-server", KnownResourceStates.Running, cts.Token);

        using var client = app.CreateHttpClient($"{ProjectName}-server", "api");

        // Serverpod answers 200 on the root path of the API server.
        var status = await GetStatusWithRetryAsync(client, "/", cts.Token);
        Assert.Equal(HttpStatusCode.OK, status);

        // The preset adds three endpoints. Insights is the second one, and Aspire must allocate it in
        // the same way as the API endpoint.
        Assert.True(api.GetEndpoint("insights").IsAllocated, "Aspire allocated no 'insights' endpoint.");

        await app.StopAsync(cts.Token);
    }

    /// <summary>
    /// Confirms that a scaffolded Jaspr site runs under <c>jaspr serve</c> on the port that Aspire
    /// allocated, and that it sends a page to the browser.
    /// </summary>
    /// <remarks>
    /// The site uses the static rendering mode, which is the Jaspr default and the mode that
    /// <see cref="DartHostingExtensions.AddJasprApp"/> reads from <c>pubspec.yaml</c>.
    /// </remarks>
    [Fact]
    [OuterloopTest("Scaffolds a Jaspr site and fetches its dependency tree from pub.")]
    [RequiresTools(["jaspr"])]
    public async Task JasprStaticSiteServesIndex()
    {
        const string SiteName = "aspiretestsite";

        using var workspace = new TempScaffoldDirectory();
        using var cts = new CancellationTokenSource(s_timeout);

        // The Aspire pub setup sibling resolves the dependencies, and the step below needs its own
        // resolution result, so the scaffolding command does not run `dart pub get` itself.
        RunTool(
            "jaspr",
            workspace.Path,
            ["create", "--mode", "static", "--routing", "multi-page", "--flutter", "none", "--no-pub-get", SiteName],
            outputHelper);

        var siteDirectory = Path.Combine(workspace.Path, SiteName);
        Assert.True(File.Exists(Path.Combine(siteDirectory, "pubspec.yaml")), $"'{siteDirectory}' holds no pubspec.yaml.");

        PreparePubDependencies(siteDirectory, outputHelper);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // AddJasprApp adds the HTTP endpoint and passes its port to `jaspr serve`.
        builder.AddJasprApp("web", siteDirectory);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync("web", KnownResourceStates.Running, cts.Token);

        using var client = app.CreateHttpClient("web", "http");

        var body = await GetStringWithRetryAsync(client, "/", cts.Token, expected: "<html");
        Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase);

        await app.StopAsync(cts.Token);
    }

    /// <summary>
    /// Resolves the pub dependencies of a scaffolded site, and repairs the one constraint that a
    /// template can pin above the installed SDK.
    /// </summary>
    /// <remarks>
    /// The Jaspr template pins <c>build_web_compilers</c>, and a release of that package can need a
    /// newer Dart SDK than the machine supplies. Pub reports the lower constraint that resolves, so
    /// the method relaxes the development dependency and resolves again. It skips the test when the
    /// second attempt also fails, because the installed SDK cannot run the scaffolded site at all.
    /// </remarks>
    private static void PreparePubDependencies(string siteDirectory, ITestOutputHelper outputHelper)
    {
        if (TryRunTool("dart", siteDirectory, ["pub", "get"], outputHelper, out var firstAttempt))
        {
            return;
        }

        // The caret constraint keeps every later 4.x release, so this relaxes the floor and never
        // holds a newer SDK back.
        if (!TryRunTool("dart", siteDirectory, ["pub", "add", "dev:build_web_compilers:^4.8.0"], outputHelper, out _))
        {
            Assert.Skip(
                "The scaffolded Jaspr site does not resolve against the installed Dart SDK: " + firstAttempt);
        }

        if (!TryRunTool("dart", siteDirectory, ["pub", "get"], outputHelper, out var secondAttempt))
        {
            Assert.Skip(
                "The scaffolded Jaspr site does not resolve against the installed Dart SDK: " + secondAttempt);
        }
    }

    /// <summary>
    /// Runs one command and fails the test when the command does not succeed.
    /// </summary>
    private static void RunTool(
        string fileName,
        string workingDirectory,
        string[] arguments,
        ITestOutputHelper outputHelper)
    {
        if (!TryRunTool(fileName, workingDirectory, arguments, outputHelper, out var output))
        {
            throw new InvalidOperationException(
                $"'{fileName} {string.Join(' ', arguments)}' failed:{Environment.NewLine}{output}");
        }
    }

    /// <summary>
    /// Runs one command and reports whether it succeeded, together with everything it wrote.
    /// </summary>
    private static bool TryRunTool(
        string fileName,
        string workingDirectory,
        string[] arguments,
        ITestOutputHelper outputHelper,
        out string output)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");

        var buffer = new StringBuilder();
        process.OutputDataReceived += (_, e) => buffer.AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => buffer.AppendLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit((int)s_scaffoldTimeout.TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"'{fileName} {string.Join(' ', arguments)}' did not complete in {s_scaffoldTimeout.TotalSeconds} seconds.");
        }

        process.WaitForExit();

        output = buffer.ToString();
        outputHelper.WriteLine($"$ {fileName} {string.Join(' ', arguments)}{Environment.NewLine}{output}");

        return process.ExitCode == 0;
    }

    /// <summary>Reads a path until the application answers with any status.</summary>
    private static async Task<HttpStatusCode> GetStatusWithRetryAsync(
        HttpClient client, string path, CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(15));

            try
            {
                var response = await client.GetAsync(path, attemptCts.Token);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.StatusCode;
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The Aspire proxy accepts a connection before the server listens, so a closed socket
                // and an attempt deadline both appear here.
                lastError = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"'{path}' never answered with status 200.", lastError);
    }

    /// <summary>Reads a path until the answer holds <paramref name="expected"/>.</summary>
    private static async Task<string> GetStringWithRetryAsync(
        HttpClient client, string path, CancellationToken cancellationToken, string expected)
    {
        string? lastBody = null;
        Exception? lastError = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(15));

            try
            {
                var response = await client.GetAsync(path, attemptCts.Token);
                lastBody = await response.Content.ReadAsStringAsync(attemptCts.Token);

                if (response.StatusCode == HttpStatusCode.OK
                    && lastBody.Contains(expected, StringComparison.OrdinalIgnoreCase))
                {
                    return lastBody;
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"'{path}' never held '{expected}'. Last body: '{lastBody}'.", lastError);
    }

    /// <summary>
    /// A temporary directory that a scaffolding command writes its project into.
    /// </summary>
    private sealed class TempScaffoldDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("aspire-dart-outerloop");

        public string Path => _directory.FullName;

        public void Dispose()
        {
            try
            {
                _directory.Delete(recursive: true);
            }
            catch (IOException)
            {
                // Best effort. A failure to remove a temporary directory must not fail a passing test.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort, as above.
            }
        }
    }
}
