// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREEXTENSION001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class DartDebuggingTests
{
    [Fact]
    public async Task WithVSCodeDebugging_PopulatesDartLaunchConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var appDirectory = builder.AppHostDirectory;

        var app = builder.AddDartApp("api", appDirectory)
            .WithDartDefine("FLAVOR", "dev")
            .WithDartRunArgs("--enable-asserts")
            .WithAppArgs("--port", 8080);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal("dart", launchConfig.Type);
        Assert.Equal(ExecutableLaunchMode.Debug, launchConfig.Mode);

        // Dart-Code resolves a relative program against the workspace folder, so the value is absolute.
        Assert.Equal(Path.Combine(Path.GetFullPath(appDirectory), "bin", "main.dart"), launchConfig.Program);
        Assert.Equal(Path.GetFullPath(appDirectory), launchConfig.Cwd);
        Assert.Equal(Path.GetFullPath(appDirectory), launchConfig.WorkingDirectory);

        // The options of `dart run` reach the adapter through toolArgs, and the program arguments
        // reach it through args.
        Assert.Equal(["--define=FLAVOR=dev", "--enable-asserts"], launchConfig.ToolArgs);
        Assert.Equal(["--port", "8080"], launchConfig.Args);
    }

    [Fact]
    public async Task WithVSCodeDebugging_KeepsArgumentsInTheAppModel()
    {
        // The Dart-Code debug adapter supplies `dart` itself, so the launch configuration carries the
        // entrypoint and the arguments. The resource command line does not change, which keeps the
        // application model and the dashboard accurate during a debug session.
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        builder.Configuration["DEBUG_SESSION_INFO"] =
            """{"protocols_supported":["test"],"supported_launch_configurations":["dart"]}""";
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithAppArgs("--config", "prod.yaml");

        var application = builder.Build();

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(app.Resource, application.Services);

        Assert.Equal(["run", "bin/main.dart", "--config", "prod.yaml"], commandArguments);

        // The resource is debuggable, and it still declares no launch tool arguments, so nothing is
        // withheld from the command line.
        Assert.True(app.Resource.SupportsDebugging(builder.Configuration, out var debugAnnotation));
        Assert.Equal("dart", debugAnnotation.LaunchConfigurationType);
        Assert.False(app.Resource.HasLaunchToolArgsOwnedBy(debugAnnotation));
    }

    [Fact]
    public void WithVSCodeDebugging_DoesNotAddAnnotationInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var app = builder.AddDartApp("api", builder.AppHostDirectory);

        Assert.False(app.Resource.HasAnnotationOfType<SupportsDebuggingAnnotation>());
    }

    [Fact]
    public async Task WithVSCodeDebugging_UsesServerpodEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(builder.AppHostDirectory), "bin", "main.dart"),
            launchConfig.Program);

        // The mode and the migration option come from a WithArgs callback and not from an
        // annotation, so the launch configuration carries neither. The debug session starts the same
        // file, which is what the developer sets breakpoints in.
        Assert.Empty(launchConfig.ToolArgs);
    }

    [Fact]
    public void WithVSCodeDebugging_SkipsNonDartRunCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // A tool that is not the Dart SDK has no contract with the Dart-Code debug adapter.
        var frog = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithRunCommand("dart_frog", "dev");

        var jaspr = builder.AddJasprApp("web", builder.AppHostDirectory);

        Assert.False(frog.Resource.HasAnnotationOfType<SupportsDebuggingAnnotation>());
        Assert.False(jaspr.Resource.HasAnnotationOfType<SupportsDebuggingAnnotation>());

        // A run command that is the Dart SDK keeps the debug support.
        var dart = builder.AddDartApp("worker", builder.AppHostDirectory)
            .WithRunCommand("dart", "run", "bin/worker.dart");

        Assert.True(dart.Resource.HasAnnotationOfType<SupportsDebuggingAnnotation>());
    }

    [Fact]
    public async Task WithVmService_AddsFlag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithVmService();

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        // Without a port the VM selects a free one, so the option carries no value.
        Assert.Equal(["run", "--enable-vm-service", "bin/main.dart"], commandArguments);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal(["--enable-vm-service"], launchConfig.ToolArgs);
    }

    [Fact]
    public async Task WithVmService_WithPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithVmService(8181);

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--enable-vm-service=8181", "bin/main.dart"], commandArguments);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal(["--enable-vm-service=8181"], launchConfig.ToolArgs);
    }

    [Fact]
    public void WithVSCodeDebugging_ThrowsOnNullBuilder()
    {
        IResourceBuilder<DartAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => builder.WithVSCodeDebugging());

        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public async Task WithVSCodeDebugging_ResolvesArgumentsThatAreNotText()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var token = builder.AddParameter("token", "s3cret", secret: true);

        // WithAppArgs takes an object, so an argument can be a parameter. The launch configuration
        // must carry the value of the parameter, not the name of its CLR type.
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithAppArgs("--token", token.Resource);

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal(["--token", "s3cret"], launchConfig.Args);

        // The command line resolves the same value, so a debug session and a plain run agree.
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart", "--token", "s3cret"], commandArguments);
    }

    [Fact]
    public async Task WithVSCodeDebugging_SplitsDartRunCommandLine()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // `dart run` reads its own options before the file name and gives everything after the file
        // name to the program, so the launch configuration must split the command line the same way.
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithRunCommand("dart", "run", "--enable-asserts", "bin/worker.dart", "--queue", "jobs");

        var launchConfig = await CreateLaunchConfigurationAsync(app.Resource);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(builder.AppHostDirectory), "bin", "worker.dart"),
            launchConfig.Program);
        Assert.Equal(["--enable-asserts"], launchConfig.ToolArgs);
        Assert.Equal(["--queue", "jobs"], launchConfig.Args);
    }

    private static async Task<DartLaunchConfiguration> CreateLaunchConfigurationAsync(
        IResource resource,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        var callbackContext = LaunchConfigurationTestHelpers.CreateCallbackContext(
            resource,
            environmentVariables: environmentVariables);

        return Assert.IsType<DartLaunchConfiguration>(
            await LaunchConfigurationTestHelpers.InvokeLaunchConfigurationProducerAsync(resource, callbackContext));
    }
}
