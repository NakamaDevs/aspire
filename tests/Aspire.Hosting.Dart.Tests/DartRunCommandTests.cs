// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMMAND001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class DartRunCommandTests
{
    // ---- WithRunCommand ------------------------------------------------------------

    [Fact]
    public async Task WithRunCommand_ReplacesCommandAndArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithRunCommand("dart_frog", "dev", "--port", "8080");

        Assert.Equal("dart_frog", app.Resource.Command);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        // The command line no longer holds `run` or the entrypoint.
        Assert.Equal(["dev", "--port", "8080"], args);
    }

    [Fact]
    public async Task WithRunCommand_KeepsAppArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithRunCommand("dart_frog", "dev")
            .WithAppArgs("--verbose");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["dev", "--verbose"], args);
    }

    [Fact]
    public void WithRunCommand_AddsRequiredCommandAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithRunCommand("dart_frog", "dev");

        Assert.True(app.Resource.TryGetAnnotationsOfType<RequiredCommandAnnotation>(out var annotations));
        Assert.Contains(annotations, a => a.Command == "dart_frog");
    }

    [Fact]
    public async Task WithRunCommand_DoesNotApplyDartDefines()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithDartDefine("FLAVOR", "dev")
            .WithRunCommand("dart_frog", "dev");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        // `--define` is an option of `dart run`, so another command must not receive it.
        Assert.Equal(["dev"], args);
    }

    [Fact]
    public async Task WithRunCommand_Dart_AppliesDefinesAfterRun()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithDartDefine("FLAVOR", "dev")
            .WithDartRunArgs("--enable-asserts")
            .WithRunCommand("dart", "run", "bin/server.dart")
            .WithAppArgs("--port", "8080");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        // `dart run` accepts its own options directly after `run` and nowhere else.
        Assert.Equal(
            ["run", "--define=FLAVOR=dev", "--enable-asserts", "bin/server.dart", "--port", "8080"],
            args);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithRunCommand_ThrowsOnNullOrEmptyCommand(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory);
        var command = isNull ? null! : string.Empty;

        var action = () => app.WithRunCommand(command);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(command), exception.ParamName);
    }

    // ---- WithEntrypoint --------------------------------------------------------------

    [Fact]
    public async Task WithEntrypoint_ReplacesEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithEntrypoint("bin/server.dart");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/server.dart"], args);
    }

    // ---- WithDartRunArgs --------------------------------------------------------------

    [Fact]
    public async Task WithDartRunArgs_InsertsBeforeEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithDartRunArgs("--enable-asserts", "--observe")
            .WithAppArgs("--port", "8080");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(
            ["run", "--enable-asserts", "--observe", "bin/main.dart", "--port", "8080"],
            args);
    }

    // ---- Presets keep the generic methods ------------------------------------------------

    [Fact]
    public async Task Presets_InheritGenericMethods()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // A Serverpod server is a Dart application, so every generic method still works on it.
        var serverpod = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithDartDefine("FLAVOR", "dev")
            .WithAppArgs("--verbose");

        var serverpodArgs = await ArgumentEvaluator.GetArgumentListAsync(serverpod.Resource);

        Assert.Equal(
            ["run", "--define=FLAVOR=dev", "bin/main.dart", "--verbose", "--mode", "development", "--apply-migrations"],
            serverpodArgs);

        // WithRunCommand replaces the command line of a preset as well.
        var jaspr = builder.AddJasprApp("web", builder.AppHostDirectory)
            .WithRunCommand("dart_frog", "dev");

        TestEndpointAllocator.AllocateEndpoints(jaspr.Resource);

        Assert.Equal("dart_frog", jaspr.Resource.Command);

        var jasprArgs = await ArgumentEvaluator.GetArgumentListAsync(jaspr.Resource);

        Assert.Equal(["dev"], jasprArgs);
    }

    // ---- Manifest ---------------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_WithRunCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddDartApp("api", AppContext.BaseDirectory)
            .WithRunCommand("dart_frog", "dev");

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "dart_frog",
              "args": [
                "dev"
              ]
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }
}
