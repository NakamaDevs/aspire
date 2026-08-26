// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Elixir.Tests;

public class ElixirOtlpTests
{
    // ---- OpenTelemetry environment ---------------------------------------------------

    [Fact]
    public async Task AddElixirApp_SetsOtlpExporterEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:4317";

        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        using var distributedApp = builder.Build();
        var serviceProvider = distributedApp.Services.GetRequiredService<IServiceProvider>();

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, serviceProvider: serviceProvider);

        // The Erlang OTLP exporter reads the endpoint and the protocol from the environment.
        Assert.Equal("http://localhost:4317", env["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        Assert.Equal("grpc", env["OTEL_EXPORTER_OTLP_PROTOCOL"]);
    }

    [Fact]
    public async Task AddElixirApp_SetsOtelServiceName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:4317";

        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        using var distributedApp = builder.Build();
        var serviceProvider = distributedApp.Services.GetRequiredService<IServiceProvider>();

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            app.Resource, serviceProvider: serviceProvider);

        // The orchestrator replaces the template with the resource name when it starts the resource.
        Assert.Contains("otel-service-name", env["OTEL_SERVICE_NAME"]);
        Assert.Contains("service.instance.id", env["OTEL_RESOURCE_ATTRIBUTES"]);
    }

    [Fact]
    public async Task MixDepsResource_DoesNotReceiveOtlpVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:4317";

        builder.AddElixirApp("api", builder.AppHostDirectory).WithMixDeps();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();
        var serviceProvider = distributedApp.Services.GetRequiredService<IServiceProvider>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            deps, serviceProvider: serviceProvider);

        // `mix deps.get` produces no telemetry, so the setup sibling stays out of the dashboard.
        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_ENDPOINT", env.Keys);
        Assert.DoesNotContain("OTEL_SERVICE_NAME", env.Keys);
    }

    // ---- Certificate trust --------------------------------------------------------------

    [Fact]
    public async Task AddElixirApp_CertificateTrust_SetsOtelExporterCertificate()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var bundle = ReferenceExpression.Create($"/etc/ssl/aspire/bundle.crt");
        var env = await InvokeCertificateTrustCallbackAsync(app, bundle, CertificateTrustScope.Append);

        Assert.Same(bundle, env["OTEL_EXPORTER_OTLP_CERTIFICATE"]);
    }

    [Fact]
    public async Task AddElixirApp_CertificateTrust_SetsSslCertFile()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var bundle = ReferenceExpression.Create($"/etc/ssl/aspire/bundle.crt");
        var env = await InvokeCertificateTrustCallbackAsync(app, bundle, CertificateTrustScope.System);

        Assert.Same(bundle, env["SSL_CERT_FILE"]);

        // The Erlang :ssl application replaces its trust set with the cacertfile, so an Append bundle,
        // which holds the custom authorities only, must not become SSL_CERT_FILE.
        var appendEnv = await InvokeCertificateTrustCallbackAsync(app, bundle, CertificateTrustScope.Append);

        Assert.DoesNotContain("SSL_CERT_FILE", appendEnv.Keys);
    }

    [Fact]
    public async Task AddElixirApp_CertificateTrust_NotAppliedInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        var bundle = ReferenceExpression.Create($"/etc/ssl/aspire/bundle.crt");
        var env = await InvokeCertificateTrustCallbackAsync(
            app, bundle, CertificateTrustScope.System, DistributedApplicationOperation.Publish);

        // Aspire applies custom certificate trust in run mode only.
        Assert.Empty(env);
    }

    private static async Task<Dictionary<string, object>> InvokeCertificateTrustCallbackAsync(
        IResourceBuilder<ElixirAppResource> app,
        ReferenceExpression bundle,
        CertificateTrustScope scope,
        DistributedApplicationOperation operation = DistributedApplicationOperation.Run)
    {
        Assert.True(app.Resource.TryGetLastAnnotation<CertificateTrustConfigurationCallbackAnnotation>(out var annotation));

        var environmentVariables = new Dictionary<string, object>();
        var context = new CertificateTrustConfigurationCallbackAnnotationContext
        {
            ExecutionContext = new DistributedApplicationExecutionContext(operation),
            Resource = app.Resource,
            Arguments = [],
            EnvironmentVariables = environmentVariables,
            CertificateBundlePath = bundle,
            CertificateDirectoriesPath = ReferenceExpression.Create($"/etc/ssl/aspire/certs"),
            Scope = scope,
            CancellationToken = default,
        };

        await annotation.Callback(context);

        return environmentVariables;
    }
}
