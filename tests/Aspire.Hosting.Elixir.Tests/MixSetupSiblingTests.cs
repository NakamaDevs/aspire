// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Elixir.Tests;

public class MixSetupSiblingTests(ITestOutputHelper outputHelper)
{
    // ---- WithMixDeps -----------------------------------------------------------

    [Fact]
    public async Task WithMixDepsCreatesSiblingResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var app = builder.AddElixirApp("api", workspace.Path)
            .WithMixDeps();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.Equal("api-mix-deps", deps.Name);
        Assert.Equal("mix", deps.Command);
        Assert.Equal(app.Resource.WorkingDirectory, deps.WorkingDirectory);

        var args = await ArgumentEvaluator.GetArgumentListAsync(deps);
        Assert.Equal(["deps.get"], args);

        Assert.True(deps.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestAnnotation));
        Assert.Null(manifestAnnotation.Callback);

        Assert.True(deps.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        Assert.Contains(relationships, r => r.Type == "Parent" && r.Resource == app.Resource);

        await PublishBeforeStartEventAsync(distributedApp);
        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == deps);
    }

    [Fact]
    public void WithMixDepsIsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddElixirApp("api", workspace.Path)
            .WithMixDeps()
            .WithMixDeps();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.Equal("api-mix-deps", deps.Name);
    }

    [Fact]
    public void WithMixDepsShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<ElixirAppResource> builder = null!;

        var action = () => builder.WithMixDeps();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public async Task WithMixDeps_ThenWithMixCompile_CompileWaitsForDeps()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var app = builder.AddElixirApp("api", workspace.Path)
            .WithMixDeps()
            .WithMixCompile();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        await PublishBeforeStartEventAsync(distributedApp);

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        var compile = Assert.Single(appModel.Resources.OfType<ElixirMixCompileResource>());

        Assert.Contains(compile.Annotations.OfType<WaitAnnotation>(), w => w.Resource == deps);
        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == compile);
    }

    // ---- WithMixCompile --------------------------------------------------------

    [Fact]
    public async Task WithMixCompileCreatesSiblingResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var app = builder.AddElixirApp("api", workspace.Path)
            .WithMixCompile();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var compile = Assert.Single(appModel.Resources.OfType<ElixirMixCompileResource>());
        Assert.Equal("api-mix-compile", compile.Name);
        Assert.Equal("mix", compile.Command);

        var args = await ArgumentEvaluator.GetArgumentListAsync(compile);
        Assert.Equal(["compile"], args);

        Assert.True(compile.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        Assert.Contains(relationships, r => r.Type == "Parent" && r.Resource == app.Resource);

        await PublishBeforeStartEventAsync(distributedApp);
        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == compile);
    }

    [Fact]
    public void WithMixCompileIsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddElixirApp("api", workspace.Path)
            .WithMixCompile()
            .WithMixCompile();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var compile = Assert.Single(appModel.Resources.OfType<ElixirMixCompileResource>());
        Assert.Equal("api-mix-compile", compile.Name);
    }

    // ---- Auto detection --------------------------------------------------------

    [Fact]
    public void AutoDetection_MixExs_AddsMixDeps()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(Path.Combine(workspace.Path, "mix.exs"), "defmodule Api.MixProject do\nend\n");

        builder.AddElixirApp("api", workspace.Path);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.Equal("api-mix-deps", deps.Name);
    }

    [Fact]
    public void AutoDetection_NoMixExs_DoesNotAddMixDeps()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddElixirApp("api", workspace.Path);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Empty(appModel.Resources.OfType<ElixirMixDepsResource>());
    }

    // ---- Sibling policy annotations ---------------------------------------------

    [Fact]
    public void MixDepsResourceHasNameValidationPolicyAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddElixirApp("api", workspace.Path)
            .WithMixDeps();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.True(deps.TryGetLastAnnotation<NameValidationPolicyAnnotation>(out var policy));
        Assert.Same(NameValidationPolicyAnnotation.None, policy);
    }

    [Fact]
    public void MixDepsResourceHasCertificateTrustScopeNone()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddElixirApp("api", workspace.Path)
            .WithMixDeps();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.True(deps.TryGetLastAnnotation<CertificateAuthorityCollectionAnnotation>(out var certificates));
        Assert.Equal(CertificateTrustScope.None, certificates.Scope);
    }

    // ---- Manifest ----------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_WithMixDeps_DoesNotAlterMainManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithMixDeps();

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
                "MIX_ENV": "dev"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifest_WithMixCompile_DoesNotAlterMainManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddElixirApp("api", AppContext.BaseDirectory)
            .WithMixCompile();

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
                "MIX_ENV": "dev"
              }
            }
            """;
        Assert.Equal(expected, manifest.ToString());
    }

    // ---- install: false ------------------------------------------------------------

    [Fact]
    public async Task WithMixDeps_InstallFalse_CreatesSiblingWithExplicitStart()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(Path.Combine(workspace.Path, "mix.exs"), "defmodule Api.MixProject do\nend\n");

        var app = builder.AddElixirApp("api", workspace.Path)
            .WithMixDeps(install: false);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        await PublishBeforeStartEventAsync(distributedApp);

        var deps = Assert.Single(appModel.Resources.OfType<ElixirMixDepsResource>());
        Assert.True(deps.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _));

        // The application must not wait for a sibling that the developer starts by hand.
        Assert.DoesNotContain(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == deps);
    }

    private static async Task PublishBeforeStartEventAsync(DistributedApplication app)
    {
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        await eventing.PublishAsync(new BeforeStartEvent(app.Services, appModel), CancellationToken.None);
    }
}
