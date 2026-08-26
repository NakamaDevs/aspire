// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Aspire.Hosting.Dart.Tests;

/// <summary>
/// Creates a temporary directory that holds a real pub package, so an integration test can start the
/// package with the <c>dart</c> command that the machine supplies.
/// </summary>
/// <remarks>
/// <para>
/// The class writes the smallest package that still exercises the integration: <c>pubspec.yaml</c>
/// and one file below <c>bin</c>. Two shapes are available. <see cref="CreateConsoleApp"/> prints one
/// line and then stops with exit code zero. <see cref="CreateServerApp"/> answers HTTP requests on
/// the port that <c>PORT</c> names.
/// </para>
/// <para>
/// Neither shape declares a pub dependency, so <c>dart pub get</c> needs no network access. The
/// server shape uses <c>dart:io</c> only, which the SDK supplies.
/// </para>
/// <para>
/// A <c>dart pub get</c> is still the slowest part of a test, so <see cref="CreateCopyOf"/> copies a
/// package that already holds <c>pubspec.lock</c> and <c>.dart_tool</c>.
/// </para>
/// </remarks>
public sealed class TempDartAppDirectory : IDisposable
{
    /// <summary>The default text that the server application returns from <c>GET /</c>.</summary>
    public const string DefaultRootResponse = "hello from dart";

    /// <summary>The line that both application shapes print when they start.</summary>
    public const string StartupMarker = "ASPIRE_DART_TEST_MARKER";

    /// <summary>The line that the second entrypoint of the console shape prints.</summary>
    public const string AlternateEntrypointMarker = "ASPIRE_DART_ALTERNATE_MARKER";

    /// <summary>The file that <see cref="CreateConsoleApp"/> writes beside the default entrypoint.</summary>
    public const string AlternateEntrypoint = "bin/alternate.dart";

    private readonly DirectoryInfo _directory;

    private TempDartAppDirectory(string appName)
    {
        AppName = appName;
        _directory = Directory.CreateTempSubdirectory("aspire-dart-tests");
        Directory.CreateDirectory(System.IO.Path.Combine(_directory.FullName, "bin"));
        Directory.CreateDirectory(System.IO.Path.Combine(_directory.FullName, "lib"));
    }

    /// <summary>The full path of the directory that holds <c>pubspec.yaml</c>.</summary>
    public string Path => _directory.FullName;

    /// <summary>The pub package name, which is also the name in the <c>package:</c> import.</summary>
    public string AppName { get; }

    /// <summary>
    /// Creates a package that prints <see cref="StartupMarker"/> and then stops with exit code zero.
    /// </summary>
    /// <remarks>
    /// The package also holds <see cref="AlternateEntrypoint"/>, which prints a different marker. A
    /// test that replaces the command line with <c>WithRunCommand</c> starts that second file, so the
    /// log proves which entrypoint ran.
    /// </remarks>
    public static TempDartAppDirectory CreateConsoleApp(string appName = "aspire_console_app")
    {
        var app = new TempDartAppDirectory(appName);

        app.WritePubspec();
        app.Write(
            System.IO.Path.Combine("bin", "main.dart"),
            $$"""
            void main(List<String> args) {
              print('{{StartupMarker}}');
            }
            """);

        app.Write(
            AlternateEntrypoint,
            $$"""
            void main(List<String> args) {
              print('{{AlternateEntrypointMarker}}');
            }
            """);

        return app;
    }

    /// <summary>
    /// Creates a package that serves HTTP with <c>dart:io</c> on the port that <c>PORT</c> names.
    /// </summary>
    /// <param name="rootResponse">The text that <c>GET /</c> returns.</param>
    /// <param name="appName">The pub package name.</param>
    /// <remarks>
    /// <c>GET /env/&lt;name&gt;</c> returns the value of the environment variable that the path names,
    /// so a test can read what Aspire put in the process environment. The server binds every
    /// interface, because the Aspire proxy does not always connect from the loopback address.
    /// </remarks>
    public static TempDartAppDirectory CreateServerApp(
        string rootResponse = DefaultRootResponse,
        string appName = "aspire_server_app")
    {
        var app = new TempDartAppDirectory(appName);

        app.WritePubspec();
        app.WriteResponseLibrary(rootResponse);
        app.Write(
            System.IO.Path.Combine("bin", "main.dart"),
            $$"""
            import 'dart:io';

            import 'package:{{appName}}/response.dart';

            Future<void> main(List<String> args) async {
              final port = int.parse(Platform.environment['PORT'] ?? '8080');

              // `anyIPv4` is necessary, because the Aspire proxy does not always connect from the
              // loopback address.
              final server = await HttpServer.bind(InternetAddress.anyIPv4, port);

              // The marker comes after the bind, so a test that waits for the marker knows that the
              // socket already accepts connections.
              print('{{StartupMarker}}');

              await for (final request in server) {
                final segments = request.uri.pathSegments;

                if (segments.length == 2 && segments[0] == 'env') {
                  request.response.write(Platform.environment[segments[1]] ?? '');
                } else if (segments.isEmpty) {
                  request.response.write(rootResponse);
                } else {
                  request.response.statusCode = HttpStatus.notFound;
                  request.response.write('not found');
                }

                await request.response.close();
              }
            }
            """);

        return app;
    }

    /// <summary>
    /// Copies a prepared package, including <c>pubspec.lock</c> and <c>.dart_tool</c>, into a new
    /// directory.
    /// </summary>
    /// <remarks>
    /// The copy keeps the resolved package configuration, so a test pays for no second resolution.
    /// Use this when a test writes to the source, because the prepared package is shared.
    /// </remarks>
    public static TempDartAppDirectory CreateCopyOf(TempDartAppDirectory source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var copy = new TempDartAppDirectory(source.AppName);
        CopyDirectory(new DirectoryInfo(source.Path), new DirectoryInfo(copy.Path));

        return copy;
    }

    /// <summary>Writes a file below the package directory and creates the parent directories.</summary>
    public string Write(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);

        return fullPath;
    }

    /// <summary>
    /// Writes a <c>.tool-versions</c> file, which is what the version detector reads when it selects
    /// the base image of the generated Dockerfile.
    /// </summary>
    public string WriteToolVersions(string dartVersion)
        => Write(".tool-versions", $"dart {dartVersion}\n");

    /// <summary>
    /// Replaces the library below <c>lib</c> that holds the answer of <c>GET /</c>.
    /// </summary>
    /// <remarks>
    /// The live-reload watcher looks at <c>lib</c> and <c>bin</c>, so a test that changes this file
    /// makes Aspire restart the resource.
    /// </remarks>
    public void WriteResponseLibrary(string rootResponse)
    {
        Write(
            System.IO.Path.Combine("lib", "response.dart"),
            $"""
            const String rootResponse = '{rootResponse}';
            """);
    }

    /// <summary>
    /// Runs one <c>dart</c> command in the package directory and fails the test when it does not
    /// succeed.
    /// </summary>
    /// <remarks>
    /// A test fixture calls this to resolve the dependencies one time. The test itself then starts the
    /// application through Aspire, and the Aspire pub setup sibling finds the work already done.
    /// </remarks>
    public void RunDart(params string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo("dart")
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

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start 'dart {string.Join(' ', arguments)}'.");

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => output.AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => output.AppendLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(milliseconds: 300_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"'dart {string.Join(' ', arguments)}' did not complete in 300 seconds.");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'dart {string.Join(' ', arguments)}' failed with exit code {process.ExitCode}:{Environment.NewLine}{output}");
        }
    }

    /// <summary>
    /// Writes the pub manifest. The package declares no dependency, so <c>dart pub get</c> works
    /// without network access.
    /// </summary>
    private void WritePubspec()
    {
        Write(
            "pubspec.yaml",
            $"""
            name: {AppName}
            description: A temporary package that an Aspire integration test starts.
            publish_to: none
            version: 0.1.0

            environment:
              sdk: '>=3.0.0 <4.0.0'

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
