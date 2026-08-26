// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class DartReferenceTests
{
    // ---- WithReference ----------------------------------------------------------------

    [Fact]
    public async Task DartResourceSupportsWithReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var connectionString = builder.AddConnectionString("cache");
        builder.Configuration["ConnectionStrings:cache"] = "redis://localhost:6379";

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithReference(connectionString);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("redis://localhost:6379", env["ConnectionStrings__cache"]);
    }

    [Fact]
    public async Task DartResourceSupportsWithReference_ServiceDiscoveryEnv()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var backend = builder.AddContainer("backend", "example/backend")
            .WithHttpEndpoint(port: 8080, targetPort: 8080)
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 8080));

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithReference(backend.GetEndpoint("http"));

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("http://localhost:8080", env["services__backend__http__0"]);
    }

    [Fact]
    public async Task WithReference_Serverpod_InjectsApiAndWebButNotInsights()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serverpod = builder.AddServerpodApp("backend", builder.AppHostDirectory);

        // Service discovery reads the allocated endpoint, so the test allocates the three of them.
        foreach (var name in new[] { "api", "insights", "web" })
        {
            serverpod.WithEndpoint(name, e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", e.TargetPort!.Value));
        }

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithReference(serverpod);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("http://localhost:8080", env["services__backend__api__0"]);
        Assert.Equal("http://localhost:8082", env["services__backend__web__0"]);

        // The Insights server carries the service protocol of the Serverpod tools, so a client of the
        // application never calls it.
        Assert.DoesNotContain("services__backend__insights__0", env.Keys);
    }

    // ---- Manifest ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_WithReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var connectionString = builder.AddConnectionString("cache");

        var app = builder.AddDartApp("api", AppContext.BaseDirectory)
            .WithReference(connectionString);

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
                "ConnectionStrings__cache": "{cache.connectionString}"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }
}
