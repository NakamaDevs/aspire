// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Elixir.Tests;

/// <summary>
/// Covers the Dockerfile that <c>aspire publish</c> generates for an Elixir or Phoenix application.
/// </summary>
public class ElixirPublishTests(ITestOutputHelper outputHelper)
{
    // ---- Versions ------------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_GeneratesDockerfile_WithVersionsFromToolVersions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        File.WriteAllText(Path.Combine(sourceDir.FullName, ".tool-versions"), "elixir 1.18.4-otp-27\nerlang 27.3.4\n");
        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName);

        builder.Build().Run();

        var dockerfilePath = Path.Combine(outputDir.FullName, "api.Dockerfile");
        Assert.True(File.Exists(dockerfilePath), "Dockerfile should be generated in publish mode");

        await Verify(await File.ReadAllTextAsync(dockerfilePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifyPublish_UsesDefaultVersions_WhenNothingDetected()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains($"elixir:{ElixirVersionDetector.DefaultElixirVersion}-otp-28-slim", content);
        Assert.Contains("erlang:28-slim", content);

        await Verify(content);
    }

    // ---- Release name --------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_ReleaseName_FromMixExsApp()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "hello_world");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("mix release 'hello_world'", content);
        Assert.Contains("/app/_build/prod/rel/hello_world", content);
        Assert.Contains("""CMD ["/app/bin/hello_world","start"]""", content);
    }

    [Fact]
    public async Task VerifyPublish_ReleaseName_FallsBackToResourceName()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("my-api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "my-api.Dockerfile"), TestContext.Current.CancellationToken);

        // A release name is an Erlang atom, so the hyphen of the resource name becomes an underscore.
        Assert.Contains("mix release 'my_api'", content);
        Assert.Contains("""CMD ["/app/bin/my_api","start"]""", content);
    }

    [Fact]
    public async Task VerifyPublish_WithReleaseName_Overrides()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "hello_world");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName)
               .WithReleaseName("custom_release");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("mix release 'custom_release'", content);
        Assert.DoesNotContain("hello_world", content);
    }

    // ---- Base images ---------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_RespectsDockerfileBaseImageAnnotation()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName)
               .WithDockerfileBaseImage(
                   buildImage: "hexpm/elixir:1.18.4-erlang-27.3.4-alpine-3.21.3",
                   runtimeImage: "alpine:3.21.3");

        builder.Build().Run();

        await Verify(await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken));
    }

    // ---- Runtime stage -------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_RuntimeStage_HasNonRootUser()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("groupadd --system --gid 999 app && useradd --system --gid 999 --uid 999 --no-create-home app", content);
        Assert.Contains("COPY --from=build --chown=app:app", content);
        Assert.Contains("USER app", content);
    }

    [Fact]
    public async Task VerifyPublish_SetsMixEnvProd()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        // Both stages need MIX_ENV: the build stage compiles for prod, and the release reads it at boot.
        var lines = content.Split('\n').Where(l => l.StartsWith("ENV MIX_ENV=", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal("ENV MIX_ENV=prod", l.TrimEnd('\r')));
    }

    // ---- Authored Dockerfile -------------------------------------------------

    [Fact]
    public void VerifyPublish_SkipsDockerfileGeneration_WhenDockerfileExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        // Pre-existing Dockerfile — the generator must leave it alone.
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Dockerfile"), "FROM scratch");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        var app = builder.AddElixirApp("api", sourceDir.FullName);

        Assert.False(app.Resource.TryGetLastAnnotation<DockerfileBuilderCallbackAnnotation>(out _),
            "No DockerfileBuilderCallbackAnnotation should be added when a Dockerfile already exists");
    }

    // ---- Container files -----------------------------------------------------

    [Fact]
    public async Task VerifyPublish_ContainerFiles_GeneratesFromAndCopyInstructions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(
            DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");

        var frontend = builder.AddResource(new ElixirFilesContainer("frontend", "node", "."))
            .PublishAsDockerFile(c =>
                c.WithDockerfileBuilder(".", ctx => ctx.Builder.From("scratch"))
                 .WithImageTag("deterministic-tag"))
            .WithAnnotation(new ContainerFilesSourceAnnotation { SourcePath = "/app/dist" });

        var api = builder.AddElixirApp("api", sourceDir.FullName);
        api.PublishWithContainerFiles(frontend, "/app/static");

        builder.Build().Run();

        var dockerfile = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("frontend", dockerfile);
        Assert.Contains("COPY --from=", dockerfile);
        Assert.Contains("/app/dist", dockerfile);
        Assert.Contains("/app/static", dockerfile);
    }

    [Fact]
    public async Task VerifyPublish_ContainerFiles_MultipleSourcesAllPresent()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(
            DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");

        var frontend = builder.AddResource(new ElixirFilesContainer("frontend", "node", "."))
            .PublishAsDockerFile(c =>
                c.WithDockerfileBuilder(".", ctx => ctx.Builder.From("scratch"))
                 .WithImageTag("frontend-tag"))
            .WithAnnotation(new ContainerFilesSourceAnnotation { SourcePath = "/app/dist" });

        var assets = builder.AddResource(new ElixirFilesContainer("assets", "node", "."))
            .PublishAsDockerFile(c =>
                c.WithDockerfileBuilder(".", ctx => ctx.Builder.From("scratch"))
                 .WithImageTag("assets-tag"))
            .WithAnnotation(new ContainerFilesSourceAnnotation { SourcePath = "/app/public" });

        var api = builder.AddElixirApp("api", sourceDir.FullName);
        api.PublishWithContainerFiles(frontend, "/app/static");
        api.PublishWithContainerFiles(assets, "/app/public");

        builder.Build().Run();

        var dockerfile = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("frontend", dockerfile);
        Assert.Contains("assets", dockerfile);
        Assert.Contains("/app/dist", dockerfile);
        Assert.Contains("/app/public", dockerfile);
    }

    // ---- Shell quoting -------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_ShellQuote_HandlesEmbeddedSingleQuotes()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName)
               .WithReleaseName("it's_alive");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        await Verify(content);
    }

    // ---- .dockerignore -------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_WritesDockerignore()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddElixirApp("api", sourceDir.FullName);

        builder.Build().Run();

        var ignorePath = Path.Combine(outputDir.FullName, "api.Dockerfile.dockerignore");
        Assert.True(File.Exists(ignorePath), "A .dockerignore should be generated next to the Dockerfile");

        var content = await File.ReadAllTextAsync(ignorePath, TestContext.Current.CancellationToken);

        Assert.Contains("_build", content);
        Assert.Contains("deps", content);
        Assert.Contains(".elixir_ls", content);
        Assert.Contains(".git", content);

        await Verify(content);
    }

    // ---- Manifest ------------------------------------------------------------

    [Fact]
    public async Task AddElixirAppProducesDockerfileResourceInManifest()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        var app = builder.AddElixirApp("api", sourceDir.FullName);

        var manifest = await ManifestUtils.GetManifest(app.Resource, manifestDirectory: sourceDir.FullName);

        var expectedManifest = """
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "api.Dockerfile"
              },
              "env": {
                "MIX_ENV": "prod"
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    // ---- Phoenix (M1.11) -----------------------------------------------------

    [Fact]
    public async Task VerifyPublish_Phoenix_RunsAssetsDeploy_WhenAssetsDirExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "web_app");
        Directory.CreateDirectory(Path.Combine(sourceDir.FullName, "assets"));

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddPhoenixApp("web", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("RUN mix assets.deploy", content);
        Assert.True(
            content.IndexOf("RUN mix compile", StringComparison.Ordinal) < content.IndexOf("RUN mix assets.deploy", StringComparison.Ordinal),
            "assets.deploy must run after compile");
        Assert.True(
            content.IndexOf("RUN mix assets.deploy", StringComparison.Ordinal) < content.IndexOf("RUN mix release", StringComparison.Ordinal),
            "assets.deploy must run before release");

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_Phoenix_SkipsAssetsDeploy_WhenNoAssetsDir()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddPhoenixApp("web", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.DoesNotContain("assets.deploy", content);
        Assert.DoesNotContain("npm ci", content);
    }

    [Fact]
    public async Task VerifyPublish_Phoenix_InstallsNode_WhenAssetsPackageJsonExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "web_app");
        var assetsDir = Directory.CreateDirectory(Path.Combine(sourceDir.FullName, "assets"));
        File.WriteAllText(Path.Combine(assetsDir.FullName, "package.json"), """{ "name": "assets" }""");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddPhoenixApp("web", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("nodejs npm", content);
        Assert.Contains("RUN npm ci --prefix assets", content);
        Assert.True(
            content.IndexOf("RUN npm ci --prefix assets", StringComparison.Ordinal) < content.IndexOf("RUN mix assets.deploy", StringComparison.Ordinal),
            "npm ci must run before assets.deploy");

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_Phoenix_SetsPhxServerAndPort()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        var app = builder.AddPhoenixApp("web", sourceDir.FullName);

        var manifest = await ManifestUtils.GetManifest(app.Resource, manifestDirectory: sourceDir.FullName);
        var env = manifest["env"]!;

        // PHX_SERVER tells the release to start the endpoint; PORT carries the allocated target port.
        Assert.Equal("true", env["PHX_SERVER"]!.ToString());
        Assert.Equal("{web.bindings.http.targetPort}", env["PORT"]!.ToString());

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        // The image also carries PHX_SERVER so the release starts the endpoint outside Aspire.
        Assert.Contains("ENV PHX_SERVER=true", content);
    }

    [Fact]
    public async Task VerifyPublish_Phoenix_SecretKeyBaseIsParameter()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WriteMixExs(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        var app = builder.AddPhoenixApp("web", sourceDir.FullName);

        var manifest = await ManifestUtils.GetManifest(app.Resource, manifestDirectory: sourceDir.FullName);

        Assert.Equal("{web-secret-key-base.value}", manifest["env"]!["SECRET_KEY_BASE"]!.ToString());

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        // The secret is supplied at run time, so it must never be baked into a layer.
        Assert.DoesNotContain("SECRET_KEY_BASE", content);
    }

    // ---- WithPhoenixHealthCheck ----------------------------------------------

    [Fact]
    public void WithPhoenixHealthCheck_AddsHttpHealthCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddPhoenixApp("web", AppContext.BaseDirectory)
                         .WithPhoenixHealthCheck();

        Assert.True(app.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations));
        Assert.NotEmpty(annotations);
    }

    [Fact]
    public void WithPhoenixHealthCheck_ThrowsOnNullBuilder()
    {
        IResourceBuilder<PhoenixAppResource> builder = null!;

        var action = () => builder.WithPhoenixHealthCheck();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    private static void WriteMixExs(string appDirectory, string appName)
    {
        File.WriteAllText(Path.Combine(appDirectory, "mix.exs"), $$"""
            defmodule Example.MixProject do
              use Mix.Project

              def project do
                [
                  app: :{{appName}},
                  version: "0.1.0"
                ]
              end
            end
            """);

        File.WriteAllText(Path.Combine(appDirectory, "mix.lock"), "%{}\n");
        Directory.CreateDirectory(Path.Combine(appDirectory, "config"));
    }

    // A minimal resource that provides container files, so the tests can call
    // PublishWithContainerFiles without a dependency on a real container integration.
    private sealed class ElixirFilesContainer(string name, string command, string workingDirectory)
        : ExecutableResource(name, command, workingDirectory), IResourceWithContainerFiles;
}
