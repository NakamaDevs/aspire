// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Hex1b.Input;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for the Aspire CLI with an Elixir polyglot AppHost.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
/// <remarks>
/// Elixir support is behind the <c>experimentalPolyglot:elixir</c> feature flag, so every test
/// enables it before it scaffolds. The container image adds Erlang/OTP and Elixir to the shared
/// polyglot base, because the CLI runs <c>apphost.exs</c> with <c>elixir</c> and runs an
/// <c>add_elixir_app</c> resource with <c>mix</c>.
/// </remarks>
public sealed class ElixirPolyglotTests(ITestOutputHelper output)
{
    /// <summary>
    /// The package prefixes the local hive must carry for an Elixir AppHost that also uses the
    /// JavaScript integration.
    /// </summary>
    private static readonly string[] s_javaScriptPackagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Elixir.", "Aspire.Hosting.JavaScript."];

    private static readonly string[] s_elixirPackagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Elixir.", "Aspire.Hosting.Elixir."];

    private static readonly string[] s_codeGenerationPackagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Elixir."];

    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task CreateElixirAppHost_ScaffoldsAndRestores()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_codeGenerationPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotElixir, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalElixirSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language elixir --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.exs", timeout: TimeSpan.FromMinutes(2));
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

        // The generator writes the loader, the base types, the transport and the runtime beside the
        // generated capability modules. The AppHost requires only aspire.ex, which loads the rest.
        var modulesDirectory = Path.Combine(projectRoot, ".aspire", "modules");

        foreach (var fileName in new[] { "aspire.ex", "base.ex", "transport.ex", "aspire_runtime.ex", "watch.exs" })
        {
            var path = Path.Combine(modulesDirectory, fileName);
            Assert.True(File.Exists(path), $"Expected 'aspire restore' to generate '{path}'.");
        }

        Assert.NotEmpty(Directory.GetFiles(modulesDirectory, "aspire_generated*.ex"));

        var appHostContent = await File.ReadAllTextAsync(Path.Combine(projectRoot, "apphost.exs"), TestContext.Current.CancellationToken);
        Assert.Contains("Aspire.create_builder!()", appHostContent, StringComparison.Ordinal);
    }

    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task CreateElixirAppHostWithViteApp()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_javaScriptPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotElixir, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalElixirSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language elixir --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.exs", timeout: TimeSpan.FromMinutes(2));
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
            Path.Combine(projectRoot, "apphost.exs"),
            """
            # Aspire Elixir AppHost
            # For more information, see: https://aspire.dev

            Code.require_file(".aspire/modules/aspire.ex", __DIR__)

            builder = Aspire.create_builder!()

            Aspire.DistributedApplicationBuilder.add_vite_app!(builder, "viteapp", "./viteapp")

            builder
            |> Aspire.build!()
            |> Aspire.run!()
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
    /// Runs an Elixir application from an Elixir AppHost.
    /// </summary>
    /// <remarks>
    /// The ticket asked for a Phoenix application here. <c>add_phoenix_app</c> needs a generated
    /// Phoenix project and a PostgreSQL database, which the CLI E2E container does not carry, so this
    /// test uses <c>add_elixir_app</c> on a project that <c>mix new</c> generates instead. That covers
    /// the same path: the Elixir AppHost creates an Elixir resource, and the CLI starts it with
    /// <c>mix run --no-halt</c>.
    /// </remarks>
    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task CreateElixirAppHostWithElixirApp()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_elixirPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotElixir, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalElixirSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language elixir --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.exs", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        var projectRoot = workspace.WorkspaceRoot.FullName;

        if (localChannel is not null)
        {
            CliE2ETestHelpers.WriteLocalChannelSettings(projectRoot, localChannel.SdkVersion);
        }

        // `mix new` produces the smallest project that `mix run --no-halt` keeps alive.
        await auto.TypeAsync("mix new elixirapp");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

        await auto.TypeAsync("aspire add Aspire.Hosting.Elixir");
        await auto.EnterAsync();
        await auto.WaitForAspireAddSuccessAsync(counter, TimeSpan.FromMinutes(2));

        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "apphost.exs"),
            """
            # Aspire Elixir AppHost
            # For more information, see: https://aspire.dev

            Code.require_file(".aspire/modules/aspire.ex", __DIR__)

            builder = Aspire.create_builder!()

            Aspire.DistributedApplicationBuilder.add_elixir_app!(builder, "elixirapp", "./elixirapp")

            builder
            |> Aspire.build!()
            |> Aspire.run!()
            """,
            TestContext.Current.CancellationToken);

        await auto.AspireStartAsync(counter, startTimeout: TimeSpan.FromMinutes(5));

        await auto.TypeAsync("aspire wait elixirapp --status up --timeout 300");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(6));

        await auto.AssertResourcesExistAsync(counter, "elixirapp");

        await auto.AspireStopAsync(counter);
    }

    /// <summary>
    /// Starts the generated <c>watch.exs</c> script and confirms that it restarts the AppHost after a
    /// change to <c>apphost.exs</c>.
    /// </summary>
    /// <remarks>
    /// This mirrors the TypeScript <c>aspire:dev</c> watch test. TypeScript runs nodemon from a
    /// package.json script; Elixir has no package manager, so the generator emits
    /// <c>.aspire/modules/watch.exs</c> and the CLI runs it as the watch command. The script starts
    /// the AppHost, so the AppHost stops at once without a CLI socket, and the script then waits for a
    /// file change. That is the state this test drives: it edits <c>apphost.exs</c> from the mounted
    /// workspace and waits for the restart line.
    /// </remarks>
    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task GeneratedAspireDevScript_StartsWatchMode_Elixir()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_codeGenerationPackagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotElixir, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalElixirSupportAsync(counter);

        await auto.TypeAsync($"aspire init --language elixir --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.exs", timeout: TimeSpan.FromMinutes(2));
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

        var watchScriptPath = Path.Combine(projectRoot, ".aspire", "modules", "watch.exs");
        Assert.True(File.Exists(watchScriptPath), $"Expected 'aspire restore' to generate '{watchScriptPath}'.");

        await auto.TypeAsync("elixir .aspire/modules/watch.exs apphost.exs");
        await auto.EnterAsync();

        // Without a CLI socket the AppHost stops at once and the watcher reports that it waits.
        await auto.WaitUntilAsync(
            s => s.ContainsText("[aspire-watch] apphost stopped with status"),
            timeout: TimeSpan.FromMinutes(2),
            description: "watch mode to start and report the stopped AppHost");

        var appHostPath = Path.Combine(projectRoot, "apphost.exs");
        var appHostContent = await File.ReadAllTextAsync(appHostPath, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            appHostPath,
            appHostContent + "\n# touched by GeneratedAspireDevScript_StartsWatchMode_Elixir\n",
            TestContext.Current.CancellationToken);

        await auto.WaitUntilAsync(
            s => s.ContainsText("[aspire-watch] restarting: apphost.exs"),
            timeout: TimeSpan.FromMinutes(2),
            description: "watch mode to restart the AppHost after the file change");

        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForAnyPromptAsync(counter);
    }
}
