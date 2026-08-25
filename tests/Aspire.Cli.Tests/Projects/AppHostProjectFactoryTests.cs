// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Projects;

public class AppHostProjectFactoryTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void AppHostProjectFactory_DetectsElixirAppHost()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.EnabledFeatures = [KnownFeatures.ExperimentalPolyglotElixir];
        });

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IAppHostProjectFactory>();

        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.exs"));
        File.WriteAllText(appHostFile.FullName, "# test apphost");

        var project = factory.TryGetProject(appHostFile);

        Assert.NotNull(project);
        Assert.IsType<GuestAppHostProject>(project);
    }

    [Fact]
    public void AppHostProjectFactory_ReturnsNullForElixirAppHost_WhenFeatureDisabled()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IAppHostProjectFactory>();

        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.exs"));
        File.WriteAllText(appHostFile.FullName, "# test apphost");

        Assert.Null(factory.TryGetProject(appHostFile));
    }
}
