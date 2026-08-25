// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREEXTENSION001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Elixir.Tests;

public class ElixirDebuggingTests
{
    [Fact]
    public async Task WithVSCodeDebugging_PopulatesElixirLaunchConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var appDirectory = builder.AppHostDirectory;

        var app = builder.AddElixirApp("api", appDirectory)
            .WithAppArgs("--port", 4000);

        var launchConfig = await CreateLaunchConfigurationAsync(
            app.Resource,
            environmentVariables: new Dictionary<string, string> { ["MIX_ENV"] = "dev" });

        Assert.Equal("elixir", launchConfig.Type);
        Assert.Equal(ExecutableLaunchMode.Debug, launchConfig.Mode);
        Assert.Equal(Path.GetFullPath(appDirectory), launchConfig.ProjectDir);
        Assert.Equal(Path.GetFullPath(appDirectory), launchConfig.WorkingDirectory);
        Assert.Equal("run", launchConfig.Task);
        Assert.Equal(["--no-halt", "--", "--port", "4000"], launchConfig.TaskArgs);
        Assert.Equal("dev", launchConfig.MixEnv);
    }

    [Fact]
    public async Task WithVSCodeDebugging_KeepsMixArgumentsInTheAppModel()
    {
        // The ElixirLS debug adapter supplies `mix` itself, so the launch configuration carries the
        // task and its arguments. The resource command line does not change, which keeps the
        // application model and the dashboard accurate during a debug session.
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        builder.Configuration["DEBUG_SESSION_INFO"] =
            """{"protocols_supported":["test"],"supported_launch_configurations":["elixir"]}""";
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithAppArgs("--config", "prod.exs");

        var application = builder.Build();

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(app.Resource, application.Services);

        Assert.Equal(["run", "--no-halt", "--", "--config", "prod.exs"], commandArguments);

        // The resource is debuggable, and it still declares no launch tool arguments, so nothing is
        // withheld from the command line.
        Assert.True(app.Resource.SupportsDebugging(builder.Configuration, out var debugAnnotation));
        Assert.Equal("elixir", debugAnnotation.LaunchConfigurationType);
        Assert.False(app.Resource.HasLaunchToolArgsOwnedBy(debugAnnotation));
    }

    [Fact]
    public async Task WithVSCodeDebugging_DoesNotRemoveMixArguments_WhenElixirLaunchConfigurationUnsupported()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        builder.Configuration["DEBUG_SESSION_INFO"] =
            """{"protocols_supported":["test"],"supported_launch_configurations":["python"]}""";
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithMixTask("phx.server")
            .WithAppArgs("--config", "prod.exs");

        var application = builder.Build();

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(app.Resource, application.Services);

        Assert.Equal(["phx.server", "--", "--config", "prod.exs"], commandArguments);

        // The IDE cannot start an "elixir" launch configuration, so Aspire runs a plain process.
        Assert.False(app.Resource.SupportsDebugging(builder.Configuration, out _));
    }

    [Fact]
    public void WithVSCodeDebugging_DoesNotAddAnnotationInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        Assert.False(app.Resource.HasAnnotationOfType<SupportsDebuggingAnnotation>());
    }

    [Fact]
    public async Task WithVSCodeDebugging_UsesPhxServerTaskForPhoenixApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal("phx.server", launchConfig.Task);
        Assert.Empty(launchConfig.TaskArgs);
        Assert.Null(launchConfig.MixEnv);
    }

    [Fact]
    public async Task WithVSCodeDebugging_PropagatesWorkingDirectoryOverride()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var overrideDirectory = Path.Combine(builder.AppHostDirectory, "umbrella", "apps", "api");

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithWorkingDirectory(overrideDirectory);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal(Path.GetFullPath(overrideDirectory), launchConfig.ProjectDir);
        Assert.Equal(Path.GetFullPath(overrideDirectory), launchConfig.WorkingDirectory);
    }

    [Fact]
    public void WithVSCodeDebugging_IsEnabledByDefaultInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var elixir = builder.AddElixirApp("api", builder.AppHostDirectory);
        var phoenix = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        Assert.Equal(
            "elixir",
            Assert.Single(elixir.Resource.Annotations.OfType<SupportsDebuggingAnnotation>()).LaunchConfigurationType);
        Assert.Equal(
            "elixir",
            Assert.Single(phoenix.Resource.Annotations.OfType<SupportsDebuggingAnnotation>()).LaunchConfigurationType);
    }

    [Fact]
    public void WithVSCodeDebugging_ThrowsOnNullBuilder()
    {
        IResourceBuilder<ElixirAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => builder.WithVSCodeDebugging());

        Assert.Equal(nameof(builder), exception.ParamName);
    }

    private static async Task<ElixirLaunchConfiguration> CreateLaunchConfigurationAsync(
        IResource resource,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        var callbackContext = LaunchConfigurationTestHelpers.CreateCallbackContext(
            resource,
            environmentVariables: environmentVariables);

        return Assert.IsType<ElixirLaunchConfiguration>(
            await LaunchConfigurationTestHelpers.InvokeLaunchConfigurationProducerAsync(resource, callbackContext));
    }
}
