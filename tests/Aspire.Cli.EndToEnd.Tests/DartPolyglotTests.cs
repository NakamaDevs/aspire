// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Hex1b.Input;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for the Aspire CLI with a Dart polyglot AppHost.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
/// <remarks>
/// Dart support is behind the <c>experimentalPolyglot:dart</c> feature flag, so every test enables it
/// before it scaffolds. The container image adds the Dart SDK to the shared polyglot base, because
/// the CLI runs <c>apphost.dart</c> with <c>dart run</c>, restores it with <c>dart pub get</c>, and
/// runs an <c>addDartApp</c> resource with <c>dart run</c>.
/// </remarks>
public sealed class DartPolyglotTests(ITestOutputHelper output)
{
    /// <summary>
    /// The package prefixes the local hive must carry for a Dart AppHost that also uses the
    /// JavaScript integration.
    /// </summary>
    private static readonly string[] s_javaScriptPackagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Dart.", "Aspire.Hosting.JavaScript."];

    private static readonly string[] s_dartPackagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Dart.", "Aspire.Hosting.Dart."];

    private static readonly string[] s_codeGenerationPackagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Dart."];

    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task CreateDartAppHost_ScaffoldsAndRestores()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_codeGenerationPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotDart, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalDartSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language dart --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.dart", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        var projectRoot = workspace.WorkspaceRoot.FullName;

        if (localChannel is not null)
        {
            CliE2ETestHelpers.WriteLocalChannelSettings(projectRoot, localChannel.SdkVersion);
        }

        GitIgnoreAssertions.AssertContainsEntry(projectRoot, ".aspire/");

        await auto.TypeAsync("aspire restore");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("SDK code restored successfully", timeout: TimeSpan.FromMinutes(3));
        await auto.WaitForSuccessPromptAsync(counter);

        // The generator writes the base types, the transport, the runtime and the watcher beside the
        // generated capability parts. The AppHost imports only aspire.dart, which loads the rest.
        var modulesDirectory = Path.Combine(projectRoot, ".aspire", "modules");

        foreach (var fileName in new[] { "aspire.dart", "base.dart", "transport.dart", "aspire_runtime.dart", "watch.dart" })
        {
            var path = Path.Combine(modulesDirectory, fileName);
            Assert.True(File.Exists(path), $"Expected 'aspire restore' to generate '{path}'.");
        }

        Assert.NotEmpty(Directory.GetFiles(modulesDirectory, "aspire_generated*.dart"));

        var appHostContent = await File.ReadAllTextAsync(Path.Combine(projectRoot, "apphost.dart"), TestContext.Current.CancellationToken);
        Assert.Contains("await createBuilder(args)", appHostContent, StringComparison.Ordinal);

        // `dart run` resolves the generated SDK through the package manifest, so the scaffold writes
        // pubspec.yaml as well.
        Assert.True(
            File.Exists(Path.Combine(projectRoot, "pubspec.yaml")),
            "Expected 'aspire init --language dart' to create pubspec.yaml.");
    }

    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task CreateDartAppHostWithViteApp()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_javaScriptPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotDart, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalDartSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language dart --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.dart", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        var projectRoot = workspace.WorkspaceRoot.FullName;

        if (localChannel is not null)
        {
            CliE2ETestHelpers.WriteLocalChannelSettings(projectRoot, localChannel.SdkVersion);
        }

        await auto.TypeAsync("npm create -y vite@latest viteapp -- --template vanilla-ts --no-interactive");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

        await auto.TypeAsync("cd viteapp && npm install && cd ..");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

        await auto.TypeAsync("aspire add Aspire.Hosting.JavaScript");
        await auto.EnterAsync();
        await auto.WaitForAspireAddSuccessAsync(counter, TimeSpan.FromMinutes(2));

        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "apphost.dart"),
            """
            // Aspire Dart AppHost
            // For more information, see: https://aspire.dev

            import '.aspire/modules/aspire.dart';

            Future<void> main(List<String> args) async {
              final builder = await createBuilder(args);

              await builder.addViteApp('viteapp', './viteapp');

              final app = await builder.build();
              await app.run();
            }
            """,
            TestContext.Current.CancellationToken);

        await auto.TypeAsync(CliE2EAutomatorHelpers.GetAspireRunCommand());
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            if (s.ContainsText("Select an AppHost to use:"))
            {
                throw new InvalidOperationException(
                    "Unexpected apphost selection prompt detected! " +
                    "This indicates multiple apphosts were incorrectly detected.");
            }

            return s.ContainsText("Press CTRL+C to stop the AppHost and exit.");
        }, timeout: CliE2EAutomatorHelpers.AspireRunReadyTimeout, description: "Press CTRL+C message (aspire run started)");

        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Runs a Dart application from a Dart AppHost.
    /// </summary>
    /// <remarks>
    /// <c>dart create</c> makes the smallest project that <c>addDartApp</c> can run. The generated
    /// <c>bin/&lt;name&gt;.dart</c> prints and exits, and <c>aspire wait</c> accepts only <c>healthy</c>,
    /// <c>up</c> and <c>down</c>, so the test replaces the entrypoint with a program that keeps
    /// running. That covers the same path as the Elixir test: the Dart AppHost creates a Dart
    /// resource, and the CLI starts it with <c>dart run</c>.
    /// </remarks>
    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task CreateDartAppHostWithDartApp()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_dartPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotDart, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalDartSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language dart --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.dart", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        var projectRoot = workspace.WorkspaceRoot.FullName;

        if (localChannel is not null)
        {
            CliE2ETestHelpers.WriteLocalChannelSettings(projectRoot, localChannel.SdkVersion);
        }

        // `dart create --template console` produces bin/dartapp.dart, which is the default entrypoint
        // that AddDartApp uses.
        await auto.TypeAsync("dart create --template console --force dartapp");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

        // The template entrypoint prints and exits, so the resource never reaches `up`. This program
        // keeps the process alive instead.
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "dartapp", "bin", "dartapp.dart"),
            """
            import 'dart:async';

            Future<void> main(List<String> args) async {
              print('dartapp started');
              Timer.periodic(const Duration(seconds: 5), (_) => print('dartapp alive'));
              await Completer<void>().future;
            }
            """,
            TestContext.Current.CancellationToken);

        await auto.TypeAsync("aspire add Aspire.Hosting.Dart");
        await auto.EnterAsync();
        await auto.WaitForAspireAddSuccessAsync(counter, TimeSpan.FromMinutes(2));

        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "apphost.dart"),
            """
            // Aspire Dart AppHost
            // For more information, see: https://aspire.dev

            import '.aspire/modules/aspire.dart';

            Future<void> main(List<String> args) async {
              final builder = await createBuilder(args);

              await builder.addDartApp('dartapp', './dartapp', entrypoint: 'bin/dartapp.dart');

              final app = await builder.build();
              await app.run();
            }
            """,
            TestContext.Current.CancellationToken);

        await auto.AspireStartAsync(counter, startTimeout: TimeSpan.FromMinutes(5));

        await auto.TypeAsync("aspire wait dartapp --status up --timeout 300");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(6));

        await auto.AssertResourcesExistAsync(counter, "dartapp");

        await auto.AspireStopAsync(counter);
    }

    /// <summary>
    /// Starts the generated <c>watch.dart</c> script and confirms that it restarts the AppHost after a
    /// change to <c>apphost.dart</c>.
    /// </summary>
    /// <remarks>
    /// This mirrors the TypeScript <c>aspire:dev</c> watch test. TypeScript runs nodemon from a
    /// package.json script; the Dart SDK has no watcher, so the generator emits
    /// <c>.aspire/modules/watch.dart</c> and the CLI runs it as the watch command. The script starts
    /// the AppHost, so the AppHost stops at once without a CLI socket, and the script then waits for a
    /// file change. That is the state this test drives: it edits <c>apphost.dart</c> from the mounted
    /// workspace and waits for the restart line.
    /// </remarks>
    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task GeneratedAspireDevScript_StartsWatchMode_Dart()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_codeGenerationPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotDart, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalDartSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language dart --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.dart", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        var projectRoot = workspace.WorkspaceRoot.FullName;

        if (localChannel is not null)
        {
            CliE2ETestHelpers.WriteLocalChannelSettings(projectRoot, localChannel.SdkVersion);
        }

        await auto.TypeAsync("aspire restore");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("SDK code restored successfully", timeout: TimeSpan.FromMinutes(3));
        await auto.WaitForSuccessPromptAsync(counter);

        var watchScriptPath = Path.Combine(projectRoot, ".aspire", "modules", "watch.dart");
        Assert.True(File.Exists(watchScriptPath), $"Expected 'aspire restore' to generate '{watchScriptPath}'.");

        await auto.TypeAsync("dart run .aspire/modules/watch.dart apphost.dart");
        await auto.EnterAsync();

        // Without a CLI socket the AppHost stops at once and the watcher reports that it waits.
        await auto.WaitUntilAsync(
            s => s.ContainsText("[aspire-watch] apphost stopped with status"),
            timeout: TimeSpan.FromMinutes(2),
            description: "watch mode to start and report the stopped AppHost");

        var appHostPath = Path.Combine(projectRoot, "apphost.dart");
        var appHostContent = await File.ReadAllTextAsync(appHostPath, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            appHostPath,
            appHostContent + "\n// touched by GeneratedAspireDevScript_StartsWatchMode_Dart\n",
            TestContext.Current.CancellationToken);

        await auto.WaitUntilAsync(
            s => s.ContainsText("[aspire-watch] restarting: apphost.dart"),
            timeout: TimeSpan.FromMinutes(2),
            description: "watch mode to restart the AppHost after the file change");

        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForAnyPromptAsync(counter);
    }
}
