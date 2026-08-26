// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class AddServerpodAppTests
{
    // ---- Guards ----------------------------------------------------------------

    [Fact]
    public void AddServerpodAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddServerpodApp("api", "/src/api_server");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddServerpodAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddServerpodApp(name, "/src/api_server");

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    // ---- Command and arguments ---------------------------------------------------

    [Fact]
    public async Task AddServerpodAppUsesBinMainDartWithModeAndApplyMigrations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        Assert.Equal("dart", app.Resource.Command);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(
            ["run", "bin/main.dart", "--mode", "development", "--apply-migrations"],
            args);
    }

    // ---- Endpoints ----------------------------------------------------------------

    [Fact]
    public void AddServerpodApp_AddsApiInsightsAndWebEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        var endpoints = app.Resource.Annotations.OfType<EndpointAnnotation>().ToList();

        Assert.Equal(3, endpoints.Count);

        var api = Assert.Single(endpoints, e => e.Name == "api");
        Assert.Equal("http", api.UriScheme);
        Assert.Equal(8080, api.TargetPort);

        var insights = Assert.Single(endpoints, e => e.Name == "insights");
        Assert.Equal("http", insights.UriScheme);
        Assert.Equal(8081, insights.TargetPort);

        var web = Assert.Single(endpoints, e => e.Name == "web");
        Assert.Equal("http", web.UriScheme);
        Assert.Equal(8082, web.TargetPort);
    }

    // ---- Environment ---------------------------------------------------------------

    [Fact]
    public async Task AddServerpodApp_SetsRunModeDevelopment()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        // An environment variable wins over config/development.yaml, so the run mode is explicit.
        Assert.Equal("development", env["SERVERPOD_RUN_MODE"]);
    }

    [Fact]
    public async Task AddServerpodApp_SetsPublicHostAndPortFromEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        // Serverpod builds the URLs that it sends to a client from the public values.
        Assert.Equal("localhost", env["SERVERPOD_API_SERVER_PUBLIC_HOST"]);
        Assert.Equal("8080", env["SERVERPOD_API_SERVER_PUBLIC_PORT"]);
        Assert.Equal("http", env["SERVERPOD_API_SERVER_PUBLIC_SCHEME"]);

        Assert.Equal("localhost", env["SERVERPOD_INSIGHTS_SERVER_PUBLIC_HOST"]);
        Assert.Equal("8081", env["SERVERPOD_INSIGHTS_SERVER_PUBLIC_PORT"]);
        Assert.Equal("http", env["SERVERPOD_INSIGHTS_SERVER_PUBLIC_SCHEME"]);

        Assert.Equal("localhost", env["SERVERPOD_WEB_SERVER_PUBLIC_HOST"]);
        Assert.Equal("8082", env["SERVERPOD_WEB_SERVER_PUBLIC_PORT"]);
        Assert.Equal("http", env["SERVERPOD_WEB_SERVER_PUBLIC_SCHEME"]);
    }

    // ---- WithServerpodDatabase --------------------------------------------------------

    [Fact]
    public async Task WithServerpodDatabase_SetsDatabaseEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddPostgres("pg")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5432));
        var database = postgres.AddDatabase("appdb");

        var app = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithServerpodDatabase(database);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("localhost", env["SERVERPOD_DATABASE_HOST"]);
        Assert.Equal("5432", env["SERVERPOD_DATABASE_PORT"]);
        Assert.Equal("appdb", env["SERVERPOD_DATABASE_NAME"]);
        Assert.Equal("postgres", env["SERVERPOD_DATABASE_USER"]);
        Assert.False(string.IsNullOrEmpty(env["SERVERPOD_PASSWORD_database"]));
    }

    [Fact]
    public async Task WithServerpodDatabase_PasswordIsSecretReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        var app = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithServerpodDatabase(database);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Publish);

        // The password stays a parameter reference, so the manifest holds no secret value.
        Assert.Equal("{pg-password.value}", env["SERVERPOD_PASSWORD_database"]);
    }

    [Fact]
    public void WithServerpodDatabase_WaitsForDatabase()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        var app = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithServerpodDatabase(database);

        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == database.Resource);
    }

    [Fact]
    public void WithServerpodDatabase_ThrowsOnNullDatabase()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        var action = () => app.WithServerpodDatabase(null!);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("database", exception.ParamName);
    }

    // ---- WithServerpodRedis -------------------------------------------------------------

    [Fact]
    public async Task WithServerpodRedis_SetsRedisEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cache = builder.AddRedis("cache")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6379));

        var app = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithServerpodRedis(cache);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("true", env["SERVERPOD_REDIS_ENABLED"]);
        Assert.Equal("localhost", env["SERVERPOD_REDIS_HOST"]);
        Assert.Equal("6379", env["SERVERPOD_REDIS_PORT"]);
        Assert.False(string.IsNullOrEmpty(env["SERVERPOD_PASSWORD_redis"]));
    }

    // ---- WithServerpodMode ----------------------------------------------------------------

    [Fact]
    public async Task WithServerpodMode_OverridesRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithServerpodMode("staging");

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart", "--mode", "staging", "--apply-migrations"], args);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("staging", env["SERVERPOD_RUN_MODE"]);
    }

    // ---- WithApplyMigrations ----------------------------------------------------------------

    [Fact]
    public async Task WithApplyMigrations_False_RemovesFlag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory)
            .WithApplyMigrations(false);

        var args = await ArgumentEvaluator.GetArgumentListAsync(app.Resource);

        Assert.Equal(["run", "bin/main.dart", "--mode", "development"], args);
    }

    // ---- Service secret ----------------------------------------------------------------------

    [Fact]
    public async Task AddServerpodApp_ServiceSecretParameterInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal("{api-service-secret.value}", env["SERVERPOD_PASSWORD_serviceSecret"]);

        // The publish default is production, because a deployed server does not run in development.
        Assert.Equal("production", env["SERVERPOD_RUN_MODE"]);
    }

    [Fact]
    public async Task AddServerpodApp_NoServiceSecretInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        TestEndpointAllocator.AllocateEndpoints(app.Resource);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        // config/development.yaml holds the local value, so a generated parameter is not necessary.
        Assert.DoesNotContain("SERVERPOD_PASSWORD_serviceSecret", env.Keys);
    }

    // ---- Manifest ------------------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_AddServerpodApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddServerpodApp("api", AppContext.BaseDirectory);

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "dart",
              "args": [
                "run",
                "bin/main.dart",
                "--mode",
                "development",
                "--apply-migrations"
              ],
              "env": {
                "SERVERPOD_API_SERVER_PORT": "{api.bindings.api.targetPort}",
                "SERVERPOD_INSIGHTS_SERVER_PORT": "{api.bindings.insights.targetPort}",
                "SERVERPOD_WEB_SERVER_PORT": "{api.bindings.web.targetPort}",
                "SERVERPOD_RUN_MODE": "development",
                "SERVERPOD_API_SERVER_PUBLIC_HOST": "{api.bindings.api.host}",
                "SERVERPOD_API_SERVER_PUBLIC_PORT": "{api.bindings.api.port}",
                "SERVERPOD_API_SERVER_PUBLIC_SCHEME": "{api.bindings.api.scheme}",
                "SERVERPOD_INSIGHTS_SERVER_PUBLIC_HOST": "{api.bindings.insights.host}",
                "SERVERPOD_INSIGHTS_SERVER_PUBLIC_PORT": "{api.bindings.insights.port}",
                "SERVERPOD_INSIGHTS_SERVER_PUBLIC_SCHEME": "{api.bindings.insights.scheme}",
                "SERVERPOD_WEB_SERVER_PUBLIC_HOST": "{api.bindings.web.host}",
                "SERVERPOD_WEB_SERVER_PUBLIC_PORT": "{api.bindings.web.port}",
                "SERVERPOD_WEB_SERVER_PUBLIC_SCHEME": "{api.bindings.web.scheme}"
              },
              "bindings": {
                "api": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8080
                },
                "insights": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8081
                },
                "web": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8082
                }
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }
}
