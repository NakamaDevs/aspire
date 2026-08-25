// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Aspire.Hosting.Elixir.Tests;

/// <summary>
/// Creates a temporary directory that holds a real Mix project, so an integration test can start the
/// project with the <c>mix</c> command that the machine supplies.
/// </summary>
/// <remarks>
/// <para>
/// The class writes the smallest project that still exercises the integration: <c>mix.exs</c>, one
/// module below <c>lib</c>, and an optional <c>.tool-versions</c> file. Two shapes are available.
/// <see cref="CreateConsoleApp"/> prints one line and then stops the virtual machine with exit code
/// zero. <see cref="CreateServerApp"/> answers HTTP requests on the port that <c>PORT</c> names.
/// </para>
/// <para>
/// A Hex fetch is the slowest part of a test, so <see cref="CreateCopyOf"/> copies a project that
/// already holds <c>deps</c> and <c>_build</c>. Mix then compiles the one changed module only.
/// </para>
/// </remarks>
public sealed class TempElixirAppDirectory : IDisposable
{
    /// <summary>The default text that the server application returns from <c>GET /</c>.</summary>
    public const string DefaultRootResponse = "hello from elixir";

    /// <summary>The line that both application shapes print when they start.</summary>
    public const string StartupMarker = "ASPIRE_ELIXIR_TEST_MARKER";

    private readonly DirectoryInfo _directory;

    private TempElixirAppDirectory(string appName)
    {
        AppName = appName;
        _directory = Directory.CreateTempSubdirectory("aspire-elixir-tests");
        Directory.CreateDirectory(System.IO.Path.Combine(_directory.FullName, "lib"));
    }

    /// <summary>The full path of the directory that holds <c>mix.exs</c>.</summary>
    public string Path => _directory.FullName;

    /// <summary>The Mix application name, which is also the atom in the <c>app:</c> key.</summary>
    public string AppName { get; }

    /// <summary>The Elixir module name that matches <see cref="AppName"/>.</summary>
    public string ModuleName => ToModuleName(AppName);

    /// <summary>
    /// Creates a project that prints <see cref="StartupMarker"/> and then stops with exit code zero.
    /// </summary>
    /// <remarks>
    /// The project has no dependencies, so <c>mix deps.get</c> needs no network access. The default
    /// command line of <c>AddElixirApp</c> is <c>mix run --no-halt</c>, which keeps the virtual
    /// machine alive. The application therefore calls <c>System.stop/1</c> from a separate process.
    /// </remarks>
    public static TempElixirAppDirectory CreateConsoleApp(string appName = "aspire_console_app")
    {
        var app = new TempElixirAppDirectory(appName);

        app.WriteMixExs(deps: "[]");
        app.Write(
            System.IO.Path.Combine("lib", $"{appName}.ex"),
            $$"""
            defmodule {{app.ModuleName}} do
              use Application

              @impl true
              def start(_type, _args) do
                IO.puts("{{StartupMarker}}")

                # `mix run --no-halt` keeps the virtual machine alive, so the application stops it.
                spawn(fn ->
                  Process.sleep(200)
                  System.stop(0)
                end)

                Supervisor.start_link([], strategy: :one_for_one, name: {{app.ModuleName}}.Supervisor)
              end
            end
            """);

        return app;
    }

    /// <summary>
    /// Creates a project that serves HTTP with Bandit and Plug on the port that <c>PORT</c> names.
    /// </summary>
    /// <param name="rootResponse">The text that <c>GET /</c> returns.</param>
    /// <param name="appName">The Mix application name.</param>
    /// <param name="extraDeps">
    /// More Mix dependency entries, for example <c>{:opentelemetry, "~> 1.5"}</c>. The caller supplies
    /// the complete entry text without the enclosing list.
    /// </param>
    /// <param name="extraApplications">More OTP applications for the <c>extra_applications</c> key.</param>
    /// <param name="extraModuleCode">Elixir source that the class appends after the two modules.</param>
    /// <param name="startupCode">Elixir source that runs at the start of <c>start/2</c>.</param>
    /// <remarks>
    /// <c>GET /env/:name</c> returns the value of the environment variable that <c>:name</c> holds, so a
    /// test can read what Aspire put in the process environment. Bandit listens on every interface,
    /// because the Aspire proxy does not always connect from the loopback address.
    /// </remarks>
    public static TempElixirAppDirectory CreateServerApp(
        string rootResponse = DefaultRootResponse,
        string appName = "aspire_server_app",
        string extraDeps = "",
        string extraApplications = "",
        string extraModuleCode = "",
        string startupCode = "")
    {
        var app = new TempElixirAppDirectory(appName);

        app.WriteMixExs(
            deps: $"[{{:bandit, \"~> 1.5\"}}{extraDeps}]",
            extraApplications: extraApplications);
        app.WriteServerModule(rootResponse, extraModuleCode, startupCode);

        return app;
    }

    /// <summary>
    /// Copies a prepared project, including <c>deps</c> and <c>_build</c>, into a new directory.
    /// </summary>
    /// <remarks>
    /// The copy keeps the compiled dependencies, so a test that changes the application module pays for
    /// the compilation of that module only. Use this when a test writes to the source, because the
    /// prepared project is shared.
    /// </remarks>
    public static TempElixirAppDirectory CreateCopyOf(TempElixirAppDirectory source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return CreateCopyOfDirectory(source.Path, source.AppName);
    }

    /// <summary>
    /// Copies a Mix project that the test does not own, for example a project of the playground.
    /// </summary>
    /// <param name="sourceDirectory">The directory that holds <c>mix.exs</c>.</param>
    /// <param name="appName">The Mix application name of that project.</param>
    /// <remarks>
    /// A test must never write to a project that another part of the repository owns. The copy also
    /// carries <c>deps</c> and <c>_build</c>, so the test does not fetch or compile them again.
    /// </remarks>
    public static TempElixirAppDirectory CreateCopyOfDirectory(string sourceDirectory, string appName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDirectory);
        ArgumentException.ThrowIfNullOrEmpty(appName);

        var copy = new TempElixirAppDirectory(appName);
        CopyDirectory(new DirectoryInfo(sourceDirectory), new DirectoryInfo(copy.Path));

        return copy;
    }

    /// <summary>Writes a file below the project directory and creates the parent directories.</summary>
    public string Write(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);

        return fullPath;
    }

    /// <summary>
    /// Writes a <c>.tool-versions</c> file, which is what the version detector reads when it selects the
    /// base images of the generated Dockerfile.
    /// </summary>
    public string WriteToolVersions(string elixirVersion, string erlangVersion)
        => Write(".tool-versions", $"elixir {elixirVersion}\nerlang {erlangVersion}\n");

    /// <summary>Replaces the application module, for example to change the response of <c>GET /</c>.</summary>
    public void WriteServerModule(string rootResponse, string extraModuleCode = "", string startupCode = "")
    {
        Write(
            System.IO.Path.Combine("lib", $"{AppName}.ex"),
            $$"""
            defmodule {{ModuleName}} do
              use Application

              @impl true
              def start(_type, _args) do
                {{startupCode}}
                port = String.to_integer(System.get_env("PORT") || "4000")

                # `ip: :any` is necessary, because the Aspire proxy does not always connect from the
                # loopback address.
                children = [{Bandit, plug: {{ModuleName}}.Router, ip: :any, port: port}]

                result = Supervisor.start_link(children, strategy: :one_for_one, name: {{ModuleName}}.Supervisor)

                # The marker comes after the supervisor starts, so a test that waits for the marker
                # knows that the socket already accepts connections.
                IO.puts("{{StartupMarker}}")
                result
              end
            end

            defmodule {{ModuleName}}.Router do
              use Plug.Router

              plug(:match)
              plug(:dispatch)

              get "/" do
                send_resp(conn, 200, "{{rootResponse}}")
              end

              get "/env/:name" do
                send_resp(conn, 200, System.get_env(name) || "")
              end

              match _ do
                send_resp(conn, 404, "not found")
              end
            end

            {{extraModuleCode}}
            """);
    }

    /// <summary>
    /// Runs one Mix task in the project directory and fails the test when the task does not succeed.
    /// </summary>
    /// <remarks>
    /// A test fixture calls this to fetch and compile the dependencies one time. The test itself then
    /// starts the application through Aspire, and the Aspire setup siblings find the work already done.
    /// </remarks>
    public void RunMix(params string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo("mix")
        {
            WorkingDirectory = Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // The Aspire resources also run with MIX_ENV=dev in run mode, so the fixture must prepare the
        // same environment. A different value makes Mix compile the project a second time.
        startInfo.Environment["MIX_ENV"] = "dev";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start 'mix {string.Join(' ', arguments)}'.");

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => output.AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => output.AppendLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(milliseconds: 300_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"'mix {string.Join(' ', arguments)}' did not complete in 300 seconds.");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'mix {string.Join(' ', arguments)}' failed with exit code {process.ExitCode}:{Environment.NewLine}{output}");
        }
    }

    private void WriteMixExs(string deps, string extraApplications = "")
    {
        // The application tuple is built first, because a raw interpolated string cannot hold a literal
        // brace directly in front of an interpolation hole.
        var applicationModule = $"{{{ModuleName}, []}}";

        Write(
            "mix.exs",
            $$"""
            defmodule {{ModuleName}}.MixProject do
              use Mix.Project

              def project do
                [app: :{{AppName}}, version: "0.1.0", elixir: "~> 1.14", deps: deps()]
              end

              def application do
                [extra_applications: [:logger{{extraApplications}}], mod: {{applicationModule}}]
              end

              defp deps do
                {{deps}}
              end
            end
            """);
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        destination.Create();

        foreach (var file in source.EnumerateFiles())
        {
            file.CopyTo(System.IO.Path.Combine(destination.FullName, file.Name), overwrite: true);
        }

        foreach (var directory in source.EnumerateDirectories())
        {
            CopyDirectory(directory, new DirectoryInfo(System.IO.Path.Combine(destination.FullName, directory.Name)));
        }
    }

    /// <summary>Turns a snake case application name into the Elixir module name.</summary>
    private static string ToModuleName(string appName)
    {
        var parts = appName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

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
