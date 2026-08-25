// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Elixir.Tests;

public class ElixirReferenceTests
{
    // ---- WithReference ----------------------------------------------------------------

    [Fact]
    public async Task ElixirResourceSupportsWithReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var connectionString = builder.AddConnectionString("cache");
        builder.Configuration["ConnectionStrings:cache"] = "redis://localhost:6379";

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithReference(connectionString);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("redis://localhost:6379", env["ConnectionStrings__cache"]);
    }

    [Fact]
    public async Task ElixirResourceSupportsWithReference_ServiceDiscoveryEnv()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var backend = builder.AddContainer("backend", "example/backend")
            .WithHttpEndpoint(port: 8080, targetPort: 8080)
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 8080));

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithReference(backend.GetEndpoint("http"));

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("http://localhost:8080", env["services__backend__http__0"]);
    }

    // ---- WithEctoDatabase ---------------------------------------------------------------

    [Fact]
    public void WithEctoDatabase_ThrowsOnNullBuilder()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var database = builder.AddPostgres("pg").AddDatabase("appdb");
        IResourceBuilder<ElixirAppResource> appBuilder = null!;

        var action = () => appBuilder.WithEctoDatabase(database);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void WithEctoDatabase_ThrowsOnNullDatabase()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var action = () => app.WithEctoDatabase(null!);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("database", exception.ParamName);
    }

    [Fact]
    public async Task WithEctoDatabase_SetsDatabaseUrlFromPostgres()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddPostgres("pg")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5432));
        var database = postgres.AddDatabase("appdb");

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithEctoDatabase(database);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var databaseUrl = env["DATABASE_URL"];

        // Ecto accepts the postgresql:// URI form. The password is generated, so match the shape.
        Assert.StartsWith("postgresql://postgres:", databaseUrl);
        Assert.EndsWith("@localhost:5432/appdb", databaseUrl);
    }

    [Fact]
    public void WithEctoDatabase_WaitsForDatabase()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithEctoDatabase(database);

        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == database.Resource);
    }

    // ---- WithEctoMigrate -----------------------------------------------------------------

    [Fact]
    public async Task WithEctoMigrate_CreatesSiblingThatWaitsForDatabase()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithEctoDatabase(database)
            .WithEctoMigrate();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var migrate = Assert.Single(appModel.Resources.OfType<ElixirEctoMigrateResource>());
        Assert.Equal("api-ecto-migrate", migrate.Name);
        Assert.Equal("mix", migrate.Command);

        var args = await ArgumentEvaluator.GetArgumentListAsync(migrate);
        Assert.Equal(["ecto.migrate"], args);

        Assert.True(migrate.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        Assert.Contains(relationships, r => r.Type == "Parent" && r.Resource == app.Resource);

        await PublishBeforeStartEventAsync(distributedApp);

        // The migration cannot run before the database accepts connections.
        Assert.Contains(migrate.Annotations.OfType<WaitAnnotation>(), w => w.Resource == database.Resource);

        // The application must not start before the migration completes.
        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == migrate);
    }

    [Fact]
    public void WithEctoMigrate_IsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithEctoDatabase(database)
            .WithEctoMigrate()
            .WithEctoMigrate();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var migrate = Assert.Single(appModel.Resources.OfType<ElixirEctoMigrateResource>());
        Assert.Equal("api-ecto-migrate", migrate.Name);
    }

    [Fact]
    public void WithEctoMigrate_ExcludedFromManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithEctoDatabase(database)
            .WithEctoMigrate();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var migrate = Assert.Single(appModel.Resources.OfType<ElixirEctoMigrateResource>());

        Assert.True(migrate.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestAnnotation));
        Assert.Null(manifestAnnotation.Callback);
    }

    [Fact]
    public async Task WithEctoMigrate_SiblingReceivesDatabaseUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddPostgres("pg")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5432));
        var database = postgres.AddDatabase("appdb");

        // WithEctoMigrate runs before WithEctoDatabase, so the sibling has to read the database later.
        builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithEctoMigrate()
            .WithEctoDatabase(database);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var migrate = Assert.Single(appModel.Resources.OfType<ElixirEctoMigrateResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            migrate, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var databaseUrl = env["DATABASE_URL"];

        Assert.StartsWith("postgresql://postgres:", databaseUrl);
        Assert.EndsWith("@localhost:5432/appdb", databaseUrl);
    }

    // ---- Manifest --------------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_WithEctoDatabase()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithEctoDatabase(database);

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
                "ConnectionStrings__appdb": "{appdb.connectionString}",
                "APPDB_HOST": "{pg.bindings.tcp.host}",
                "APPDB_PORT": "{pg.bindings.tcp.port}",
                "APPDB_USERNAME": "postgres",
                "APPDB_PASSWORD": "{pg-password.value}",
                "APPDB_URI": "postgresql://postgres:{pg-password-uri-encoded.value}@{pg.bindings.tcp.host}:{pg.bindings.tcp.port}/appdb",
                "APPDB_JDBCCONNECTIONSTRING": "jdbc:postgresql://{pg.bindings.tcp.host}:{pg.bindings.tcp.port}/appdb",
                "APPDB_DATABASENAME": "appdb",
                "DATABASE_URL": "postgresql://postgres:{pg-password-uri-encoded.value}@{pg.bindings.tcp.host}:{pg.bindings.tcp.port}/appdb"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }

    private static async Task PublishBeforeStartEventAsync(DistributedApplication app)
    {
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        await eventing.PublishAsync(new BeforeStartEvent(app.Services, appModel), CancellationToken.None);
    }
}
