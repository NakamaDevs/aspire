// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMMAND001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Elixir.Tests;

public class AddElixirAppTests
{
    // ---- AddElixirApp guards -------------------------------------------------

    [Fact]
    public void AddElixirAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddElixirApp("api", "/src/elixir-app");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddElixirAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddElixirApp(name, "/src/elixir-app");

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddElixirAppShouldThrowWhenAppDirectoryIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var appDirectory = isNull ? null! : string.Empty;

        var action = () => builder.AddElixirApp("api", appDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(appDirectory), exception.ParamName);
    }

    // ---- ElixirAppResource constructor guards --------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorElixirAppResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string workingDirectory = "/src/elixir-app";

        var action = () => new ElixirAppResource(name, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorElixirAppResourceShouldThrowWhenWorkingDirectoryIsNull()
    {
        const string name = "api";

        var action = () => new ElixirAppResource(name, workingDirectory: null!);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("workingDirectory", exception.ParamName);
    }

    // ---- Command and default arguments ---------------------------------------

    [Fact]
    public void AddElixirAppUsesMixAsCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        Assert.Equal("mix", app.Resource.Command);
    }

    [Fact]
    public async Task AddElixirAppDefaultArgsAreRunNoHalt()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--no-halt"], args);
    }

    // ---- Required commands ----------------------------------------------------

    [Fact]
    public void AddElixirApp_HasRequiredCommandAnnotationForMixAndElixir()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        Assert.True(
            app.Resource.TryGetAnnotationsOfType<RequiredCommandAnnotation>(out var annotations),
            "ElixirAppResource should have at least one RequiredCommandAnnotation");
        Assert.Contains(annotations, a => a.Command == "mix");
        Assert.Contains(annotations, a => a.Command == "elixir");
    }

    // ---- MIX_ENV --------------------------------------------------------------

    // The MIX_ENV defaults now live in MixEnvAndTaskTests:
    // DefaultMixEnv_IsDevInRunMode and DefaultMixEnv_IsProdInPublishMode.

    // ---- Container files destination ------------------------------------------

    [Fact]
    public void ElixirAppResource_ImplementsIContainerFilesDestinationResource()
    {
        var resource = new ElixirAppResource("api", "/src/elixir-app");

        Assert.IsAssignableFrom<IContainerFilesDestinationResource>(resource);
    }

    // ---- WithAppArgs -----------------------------------------------------------

    [Fact]
    public async Task WithAppArgsPassesArgsAfterSeparator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithAppArgs("--port", "4000");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--no-halt", "--", "--port", "4000"], args);
    }

    [Fact]
    public async Task WithAppArgsReplacesOnSecondCall()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithAppArgs("--port", "4000")
            .WithAppArgs("--port", "5000");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--no-halt", "--", "--port", "5000"], args);
    }

    [Fact]
    public async Task WithAppArgs_AcceptsReferenceExpression()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var port = builder.AddParameter("port", "4000");

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithAppArgs("--port", ReferenceExpression.Create($"{port.Resource}"));

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "--no-halt", "--", "--port", "4000"], args);
    }

    [Fact]
    public void WithAppArgsShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<ElixirAppResource> builder = null!;

        var action = () => builder.WithAppArgs("--port", "4000");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    // ---- Manifest ---------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_AddElixirApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithHttpEndpoint(port: 4000, env: "PORT");

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "mix",
              "args": [
                "run",
                "--no-halt"
              ],
              "env": {
                "MIX_ENV": "dev",
                "PORT": "{api.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 4000,
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

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithAppArgs("--port", "4000");

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "mix",
              "args": [
                "run",
                "--no-halt",
                "--",
                "--port",
                "4000"
              ],
              "env": {
                "MIX_ENV": "dev"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }
}
