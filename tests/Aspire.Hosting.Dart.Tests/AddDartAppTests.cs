// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMMAND001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class AddDartAppTests
{
    // ---- AddDartApp guards ---------------------------------------------------

    [Fact]
    public void AddDartAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddDartApp("api", "/src/dart-app");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDartAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddDartApp(name, "/src/dart-app");

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDartAppShouldThrowWhenAppDirectoryIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var appDirectory = isNull ? null! : string.Empty;

        var action = () => builder.AddDartApp("api", appDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(appDirectory), exception.ParamName);
    }

    // ---- DartAppResource constructor guards ----------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorDartAppResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string workingDirectory = "/src/dart-app";

        var action = () => new DartAppResource(name, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorDartAppResourceShouldThrowWhenWorkingDirectoryIsNull()
    {
        const string name = "api";

        var action = () => new DartAppResource(name, workingDirectory: null!);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("workingDirectory", exception.ParamName);
    }

    // ---- Command and default arguments ---------------------------------------

    [Fact]
    public void AddDartAppUsesDartAsCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory);

        Assert.Equal("dart", app.Resource.Command);
    }

    [Fact]
    public async Task AddDartAppDefaultArgsAreRunBinMainDart()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart"], args);
    }

    [Fact]
    public async Task AddDartApp_CustomEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory, entrypoint: "bin/server.dart");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/server.dart"], args);
    }

    // ---- Required commands ----------------------------------------------------

    [Fact]
    public void AddDartApp_HasRequiredCommandAnnotationForDart()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory);

        Assert.True(
            app.Resource.TryGetAnnotationsOfType<RequiredCommandAnnotation>(out var annotations),
            "DartAppResource should have at least one RequiredCommandAnnotation");
        Assert.Contains(annotations, a => a.Command == "dart");
    }

    // ---- Container files destination ------------------------------------------

    [Fact]
    public void DartAppResource_ImplementsIContainerFilesDestinationResource()
    {
        var resource = new DartAppResource("api", "/src/dart-app");

        Assert.IsAssignableFrom<IContainerFilesDestinationResource>(resource);
    }

    // ---- WithAppArgs -----------------------------------------------------------

    [Fact]
    public async Task WithAppArgsPassesArgsAfterEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithAppArgs("--port", "8080");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart", "--port", "8080"], args);
    }

    [Fact]
    public async Task WithAppArgsReplacesOnSecondCall()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithAppArgs("--port", "8080")
            .WithAppArgs("--port", "9090");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart", "--port", "9090"], args);
    }

    [Fact]
    public async Task WithAppArgs_AcceptsReferenceExpression()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var port = builder.AddParameter("port", "8080");

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithAppArgs("--port", ReferenceExpression.Create($"{port.Resource}"));

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart", "--port", "8080"], args);
    }

    [Fact]
    public void WithAppArgsShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<DartAppResource> builder = null!;

        var action = () => builder.WithAppArgs("--port", "8080");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    // ---- WithDartDefine ---------------------------------------------------------

    [Fact]
    public async Task WithDartDefine_AddsDefineBeforeEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithDartDefine("FLAVOR", "dev")
            .WithDartDefine("API_URL", "http://localhost:8080");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(
            ["run", "--define=FLAVOR=dev", "--define=API_URL=http://localhost:8080", "bin/main.dart"],
            args);
    }

    [Fact]
    public async Task WithDartDefine_SameKeyReplaces()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithDartDefine("FLAVOR", "dev")
            .WithDartDefine("FLAVOR", "prod");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--define=FLAVOR=prod", "bin/main.dart"], args);
    }

    // ---- Manifest ---------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_AddDartApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddDartApp("api", AppContext.BaseDirectory)
            .WithHttpEndpoint(port: 8080, env: "PORT");

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "dart",
              "args": [
                "run",
                "bin/main.dart"
              ],
              "env": {
                "PORT": "{api.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 8080,
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifest_WithAppArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddDartApp("api", AppContext.BaseDirectory)
            .WithAppArgs("--port", "8080");

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "dart",
              "args": [
                "run",
                "bin/main.dart",
                "--port",
                "8080"
              ]
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }
}
