// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Resources;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Regression coverage for https://github.com/microsoft/aspire/issues/16513.
/// Polyglot (non-.NET) AppHosts run under the generic "aspire-managed" host process, so the AppHost
/// must report its real path over the backchannel. <c>AtsElixirCodeGenerator</c> forwards
/// <c>ASPIRE_APPHOST_FILEPATH</c> and <c>ASPIRE_PROJECT_DIRECTORY</c> through <c>create_builder</c>.
/// This test starts the AppHost in the background and stops it from an unrelated working directory
/// with <c>aspire stop --apphost &lt;directory&gt;</c>. If the forwarding regresses, the stop command
/// cannot match the running guest AppHost and this test fails.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class ElixirPolyglotApphostDirectoryTests(ITestOutputHelper output)
{
    private static readonly string[] s_packagePrefixes =
        ["Aspire.Hosting.CodeGeneration.Elixir.", "Aspire.Hosting.JavaScript."];

    [Fact]
    [CaptureWorkspaceOnFailure]
    public async Task StopElixirPolyglotAppHostUsingApphostDirectory()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        var localChannel = CliE2ETestHelpers.PrepareLocalChannel(repoRoot, strategy, s_packagePrefixes);
        var channelArgument = localChannel is not null ? " --channel local" : string.Empty;

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.PolyglotElixir, mountDockerSocket: true, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.EnableExperimentalElixirSupportAsync(counter);

        // Put the AppHost in a subdirectory so the test can leave that directory and resolve the
        // AppHost again with `--apphost exapp`. That forces discovery through the path the running
        // host reported over the backchannel instead of through the working directory.
        await auto.TypeAsync("mkdir exapp && cd exapp");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync($"aspire init --language elixir --non-interactive{channelArgument}");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.exs", timeout: TimeSpan.FromMinutes(2));
        await auto.DeclineAgentInitPromptAsync(counter);

        var projectRoot = Path.Combine(workspace.WorkspaceRoot.FullName, "exapp");

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

        // `aspire start` returns to the prompt once the AppHost reports that it started.
        await auto.TypeAsync("aspire start");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync(RunCommandStrings.AppHostStartedSuccessfully, timeout: TimeSpan.FromMinutes(3));
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.ClearScreenAsync(counter);

        // Leave the AppHost directory so `aspire stop` can find the running guest AppHost only
        // through the AppHost path it reported over the backchannel.
        await auto.TypeAsync("cd ..");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("aspire stop --non-interactive --apphost exapp");
        await auto.EnterAsync();
        await auto.WaitUntilAppHostStoppedSuccessfullyAsync(timeout: TimeSpan.FromMinutes(1));
        await auto.WaitForSuccessPromptAsync(counter);
    }
}
