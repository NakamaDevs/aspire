// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMMAND001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class AddJasprAppTests(ITestOutputHelper outputHelper)
{
    // ---- Guards -----------------------------------------------------------------

    [Fact]
    public void AddJasprAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddJasprApp("web", "/src/jaspr-app");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddJasprAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddJasprApp(name, "/src/jaspr-app");

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    // ---- Command and arguments ----------------------------------------------------

    [Fact]
    public async Task AddJasprAppUsesJasprServeWithPortFromEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddJasprApp("web", builder.AppHostDirectory);

        // The Jaspr command-line tool starts the application, so the command is not `dart`.
        Assert.Equal("jaspr", app.Resource.Command);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["serve", "-p", "8000"], args);
    }

    [Fact]
    public async Task WithJasprDevPorts_SetsWebAndProxyPorts()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddJasprApp("web", builder.AppHostDirectory)
            .WithJasprDevPorts(webPort: 5001, proxyPort: 5002);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        // The Jaspr defaults 5467 and 5567 collide when two instances run at the same time.
        Assert.Equal(["serve", "-p", "8000", "--web-port", "5001", "--proxy-port", "5002"], args);
    }

    // ---- Mode ------------------------------------------------------------------------

    [Fact]
    public void AddJasprApp_DetectsStaticModeFromPubspec()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        WritePubspec(workspace.Path, "jaspr:\n  mode: static\n");

        var app = builder.AddJasprApp("web", workspace.Path);

        Assert.True(app.Resource.TryGetLastAnnotation<JasprModeAnnotation>(out var annotation));
        Assert.Equal(JasprMode.Static, annotation.Mode);
    }

    [Fact]
    public void AddJasprApp_DetectsServerModeFromPubspec()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        WritePubspec(workspace.Path, "jaspr:\n  mode: server\n");

        var app = builder.AddJasprApp("web", workspace.Path);

        Assert.True(app.Resource.TryGetLastAnnotation<JasprModeAnnotation>(out var annotation));
        Assert.Equal(JasprMode.Server, annotation.Mode);
    }

    [Fact]
    public void AddJasprApp_DefaultsToStaticMode_WhenPubspecHasNoMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        WritePubspec(workspace.Path, "dependencies:\n  jaspr: ^0.23.0\n");

        var app = builder.AddJasprApp("web", workspace.Path);

        Assert.True(app.Resource.TryGetLastAnnotation<JasprModeAnnotation>(out var annotation));
        Assert.Equal(JasprMode.Static, annotation.Mode);
    }

    [Fact]
    public void WithJasprMode_Overrides()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        WritePubspec(workspace.Path, "jaspr:\n  mode: static\n");

        var app = builder.AddJasprApp("web", workspace.Path)
            .WithJasprMode(JasprMode.Client);

        Assert.True(app.Resource.TryGetLastAnnotation<JasprModeAnnotation>(out var annotation));
        Assert.Equal(JasprMode.Client, annotation.Mode);
    }

    // ---- Required commands --------------------------------------------------------------

    [Fact]
    public void AddJasprApp_HasRequiredCommandAnnotationForJaspr()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddJasprApp("web", builder.AppHostDirectory);

        Assert.True(
            app.Resource.TryGetAnnotationsOfType<RequiredCommandAnnotation>(out var annotations),
            "JasprAppResource should have at least one RequiredCommandAnnotation");
        Assert.Contains(annotations, a => a.Command == "jaspr");
    }

    // ---- Live reload -----------------------------------------------------------------------

    [Fact]
    public void AddJasprApp_MarksSelfReload()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddJasprApp("web", builder.AppHostDirectory);

        // `jaspr serve` watches the sources itself, so Aspire must not restart the process.
        Assert.True(app.Resource.TryGetLastAnnotation<JasprSelfReloadsAnnotation>(out _));
    }

    // ---- Manifest ---------------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_AddJasprApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddJasprApp("web", AppContext.BaseDirectory);

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "jaspr",
              "args": [
                "serve",
                "-p",
                "{web.bindings.http.targetPort}"
              ],
              "env": {
                "PORT": "{web.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }

    private static void WritePubspec(string directory, string content)
        => File.WriteAllText(Path.Combine(directory, "pubspec.yaml"), $"name: web\n{content}");
}
