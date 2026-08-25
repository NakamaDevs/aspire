// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Elixir.Tests;

public class MixEnvAndTaskTests
{
    // ---- WithMixEnv ------------------------------------------------------------

    [Fact]
    public async Task WithMixEnv_SetsEnvironmentVariable()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithMixEnv("test");

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("test", env["MIX_ENV"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithMixEnv_ThrowsOnNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);
        var env = isNull ? null! : string.Empty;

        var action = () => app.WithMixEnv(env);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(env), exception.ParamName);
    }

    [Fact]
    public async Task DefaultMixEnv_IsDevInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("dev", env["MIX_ENV"]);
    }

    [Fact]
    public async Task DefaultMixEnv_IsProdInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal("prod", env["MIX_ENV"]);
    }

    // ---- WithMixTask -------------------------------------------------------------

    [Fact]
    public async Task WithMixTask_ReplacesDefaultArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithMixTask("phx.server");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["phx.server"], args);
    }

    [Fact]
    public async Task WithMixTask_KeepsAppArgsAfterSeparator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithMixTask("release", "my_app")
            .WithAppArgs("--port", "4000");

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["release", "my_app", "--", "--port", "4000"], args);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithMixTask_ThrowsOnNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);
        var task = isNull ? null! : string.Empty;

        var action = () => app.WithMixTask(task);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(task), exception.ParamName);
    }

    // ---- Erlang VM flags ----------------------------------------------------------

    [Fact]
    public async Task WithErlFlags_SetsErlFlags()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithErlFlags("+S 4:4");

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("+S 4:4", env["ERL_FLAGS"]);
    }

    [Fact]
    public async Task WithElixirErlOptions_SetsElixirErlOptions()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithElixirErlOptions("+K true");

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("+K true", env["ELIXIR_ERL_OPTIONS"]);
    }

    // ---- WithNodeName --------------------------------------------------------------

    [Fact]
    public async Task WithNodeName_SetsSnameAndCookie()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cookie = builder.AddParameter("cookie", "s3cret", secret: true);

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithNodeName("api", cookie);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("-sname api -setcookie s3cret", env["ELIXIR_ERL_OPTIONS"]);
        Assert.Equal("api", env["RELEASE_NODE"]);
        Assert.Equal("s3cret", env["RELEASE_COOKIE"]);
    }

    [Fact]
    public void WithNodeName_CookieIsSecretParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithNodeName("api");

        Assert.True(app.Resource.TryGetLastAnnotation<ElixirNodeNameAnnotation>(out var nodeName));
        Assert.Equal("api", nodeName.NodeName);
        Assert.Equal("api-cookie", nodeName.Cookie.Name);
        Assert.True(nodeName.Cookie.Secret);
    }

    [Fact]
    public async Task WithNodeName_AppendsToExistingErlOptions()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cookie = builder.AddParameter("cookie", "s3cret", secret: true);

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithElixirErlOptions("+K true")
            .WithNodeName("api", cookie);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("+K true -sname api -setcookie s3cret", env["ELIXIR_ERL_OPTIONS"]);
    }

    [Fact]
    public async Task WithElixirErlOptions_KeepsBracesLiteral()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cookie = builder.AddParameter("cookie", "s3cret", secret: true);

        // The value reaches a reference expression, which treats a brace as a placeholder. Without an
        // escape the cookie would take the place of {0}.
        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithElixirErlOptions("-eval {0}")
            .WithNodeName("api", cookie);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(app.Resource);

        Assert.Equal("-eval {0} -sname api -setcookie s3cret", env["ELIXIR_ERL_OPTIONS"]);
    }

    // ---- Manifest ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_WithMixTask()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithMixTask("phx.server");

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
                "MIX_ENV": "dev"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifest_WithMixEnv()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithMixEnv("test");

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
                "MIX_ENV": "test"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }
}
