// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMMAND001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Dart.Tests;

public class PubGetSiblingTests(ITestOutputHelper outputHelper)
{
    // ---- WithPubGet ------------------------------------------------------------

    [Fact]
    public async Task WithPubGetCreatesSiblingResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var app = builder.AddDartApp("api", workspace.Path)
            .WithPubGet();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());
        Assert.Equal("api-pub-get", pubGet.Name);
        Assert.Equal("dart", pubGet.Command);
        Assert.Equal(app.Resource.WorkingDirectory, pubGet.WorkingDirectory);

        var args = await ArgumentEvaluator.GetArgumentListAsync(pubGet);
        Assert.Equal(["pub", "get"], args);

        Assert.True(pubGet.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestAnnotation));
        Assert.Null(manifestAnnotation.Callback);

        Assert.True(pubGet.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        Assert.Contains(relationships, r => r.Type == "Parent" && r.Resource == app.Resource);

        Assert.True(pubGet.TryGetAnnotationsOfType<RequiredCommandAnnotation>(out var commands));
        Assert.Contains(commands, a => a.Command == "dart");

        await PublishBeforeStartEventAsync(distributedApp);
        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == pubGet);
    }

    [Fact]
    public void WithPubGetIsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddDartApp("api", workspace.Path)
            .WithPubGet()
            .WithPubGet();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());
        Assert.Equal("api-pub-get", pubGet.Name);
    }

    [Fact]
    public void WithPubGetShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<DartAppResource> builder = null!;

        var action = () => builder.WithPubGet();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    // ---- Auto detection --------------------------------------------------------

    [Fact]
    public void AutoDetection_Pubspec_AddsPubGet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(Path.Combine(workspace.Path, "pubspec.yaml"), "name: api\n");

        builder.AddDartApp("api", workspace.Path);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());
        Assert.Equal("api-pub-get", pubGet.Name);
    }

    [Fact]
    public void AutoDetection_NoPubspec_DoesNotAddPubGet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddDartApp("api", workspace.Path);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Empty(appModel.Resources.OfType<DartPubGetResource>());
    }

    // ---- Sibling policy annotations ---------------------------------------------

    [Fact]
    public void PubGetResourceHasNameValidationPolicyAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddDartApp("api", workspace.Path)
            .WithPubGet();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());
        Assert.True(pubGet.TryGetLastAnnotation<NameValidationPolicyAnnotation>(out var policy));
        Assert.Same(NameValidationPolicyAnnotation.None, policy);
    }

    [Fact]
    public void PubGetResourceHasCertificateTrustScopeNone()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        builder.AddDartApp("api", workspace.Path)
            .WithPubGet();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());
        Assert.True(pubGet.TryGetLastAnnotation<CertificateAuthorityCollectionAnnotation>(out var certificates));
        Assert.Equal(CertificateTrustScope.None, certificates.Scope);
    }

    // ---- install: false ------------------------------------------------------------

    [Fact]
    public async Task WithPubGet_InstallFalse_CreatesSiblingWithExplicitStart()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(Path.Combine(workspace.Path, "pubspec.yaml"), "name: api\n");

        var app = builder.AddDartApp("api", workspace.Path)
            .WithPubGet(install: false);

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        await PublishBeforeStartEventAsync(distributedApp);

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());
        Assert.True(pubGet.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _));

        // The application must not wait for a sibling that the developer starts by hand.
        Assert.DoesNotContain(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == pubGet);
    }

    [Fact]
    public async Task WithPubGet_AfterInstallFalse_ReEnables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var app = builder.AddDartApp("api", workspace.Path)
            .WithPubGet(install: false)
            .WithPubGet();

        using var distributedApp = builder.Build();
        var appModel = distributedApp.Services.GetRequiredService<DistributedApplicationModel>();

        await PublishBeforeStartEventAsync(distributedApp);

        var pubGet = Assert.Single(appModel.Resources.OfType<DartPubGetResource>());

        // The two values are symmetric, so the second call turns the automatic run back on.
        Assert.False(pubGet.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _));
        Assert.Contains(app.Resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == pubGet);
    }

    // ---- Manifest ----------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_WithPubGet_DoesNotAlterMainManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var app = builder.AddDartApp("api", AppContext.BaseDirectory)
            .WithPubGet();

        var manifest = await ManifestUtils.GetManifest(app.Resource);

        var expected = """
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "dart",
              "args": [
                "run",
                "bin/main.dart"
              ]
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
