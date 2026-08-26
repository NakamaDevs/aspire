// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Elixir.Tests;

public class AddPhoenixAppTests(ITestOutputHelper outputHelper)
{
    // ---- Guards -------------------------------------------------------------------

    [Fact]
    public void AddPhoenixAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddPhoenixApp("web", "/src/phoenix-app");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPhoenixAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddPhoenixApp(name, "/src/phoenix-app");

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    // ---- Mix task ------------------------------------------------------------------

    [Fact]
    public async Task AddPhoenixAppUsesMixPhxServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        Assert.Equal("mix", app.Resource.Command);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["phx.server"], args);
    }

    [Fact]
    public async Task AddPhoenixApp_WithMixTaskOverridesPhxServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory)
            .WithMixTask("run", "--no-halt");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--no-halt"], args);
    }

    // ---- Endpoint ------------------------------------------------------------------

    [Fact]
    public async Task AddPhoenixApp_AddsHttpEndpointWithPortEnv()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);
        var app = builder.AddPhoenixApp("web", AppContext.BaseDirectory);

        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("http", endpoint.Name);
        Assert.Equal("http", endpoint.UriScheme);

        // TargetPortEnvironmentVariable is internal to Aspire.Hosting, so read the wiring from the
        // manifest, which is the public description of the same binding.
        var manifest = await ManifestUtils.GetManifest(app.Resource);

        Assert.Equal("{web.bindings.http.targetPort}", manifest["env"]?["PORT"]?.ToString());
    }

    // ---- Environment ----------------------------------------------------------------

    [Fact]
    public async Task AddPhoenixApp_SetsPhxServerTrue()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("true", env["PHX_SERVER"]);
    }

    [Fact]
    public async Task AddPhoenixApp_SetsPhxHostFromEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("localhost", env["PHX_HOST"]);
    }

    // ---- SECRET_KEY_BASE --------------------------------------------------------------

    [Fact]
    public async Task AddPhoenixApp_DoesNotSetSecretKeyBaseInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.DoesNotContain("SECRET_KEY_BASE", env.Keys);
    }

    [Fact]
    public async Task AddPhoenixApp_SetsSecretKeyBaseParameterInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal("{web-secret-key-base.value}", env["SECRET_KEY_BASE"]);
    }

    // ---- Mix setup siblings -----------------------------------------------------------

    [Fact]
    public void AddPhoenixApp_InheritsMixDepsSibling()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(Path.Combine(workspace.Path, "mix.exs"), "defmodule Web.MixProject do\nend\n");

        builder.AddPhoenixApp("web", workspace.Path);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.Equal("web-mix-deps", deps.Name);
    }

    // ---- Manifest ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_AddPhoenixApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddPhoenixApp("web", AppContext.BaseDirectory);

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "mix",
              "args": [
                "phx.server"
              ],
              "env": {
                "MIX_ENV": "dev",
                "PORT": "{web.bindings.http.targetPort}",
                "PHX_SERVER": "true",
                "PHX_HOST": "{web.bindings.http.host}"
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
}
