// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

/// <summary>
/// Covers the Dockerfile that <c>aspire publish</c> generates for a Dart application.
/// </summary>
public class DartPublishTests(ITestOutputHelper outputHelper)
{
    // ---- The executable shape ------------------------------------------------

    [Fact]
    public async Task VerifyPublish_DartApp_GeneratesTwoStageDockerfile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("api", sourceDir.FullName);

        builder.Build().Run();

        var dockerfilePath = Path.Combine(outputDir.FullName, "api.Dockerfile");
        Assert.True(File.Exists(dockerfilePath), "Dockerfile should be generated in publish mode");

        var content = await File.ReadAllTextAsync(dockerfilePath, TestContext.Current.CancellationToken);

        // The build stage compiles a native executable, and the runtime stage carries the Dart
        // runtime libraries and the executable only.
        Assert.Contains("RUN dart compile exe bin/main.dart -o /app/bin/server", content);
        Assert.Contains("FROM scratch", content);
        Assert.Contains("COPY --from=build /runtime/ /", content);

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_UsesVersionFromToolVersions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        File.WriteAllText(Path.Combine(sourceDir.FullName, ".tool-versions"), "dart 3.9.4\n");
        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("FROM docker.io/library/dart:3.9.4 AS build", content);
        Assert.DoesNotContain(DartVersionDetector.DefaultDartVersion, content);

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_RespectsDockerfileBaseImageAnnotation()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("api", sourceDir.FullName)
               .WithDockerfileBaseImage(
                   buildImage: "docker.io/library/dart:3.9.4-sdk",
                   runtimeImage: "debian:bookworm-slim");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        // A runtime image that is not scratch already holds a C library and a loader, so the image
        // needs no /runtime directory. It still needs the executable and the command.
        Assert.DoesNotContain("/runtime/", content);
        Assert.Contains("COPY --from=build /app/bin/server /app/bin/", content);
        Assert.Contains("""CMD ["/app/bin/server"]""", content);

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_WithPublishEntrypoint_Overrides()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("api", sourceDir.FullName, "bin/dev.dart")
               .WithPublishEntrypoint("bin/production.dart");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("RUN dart compile exe bin/production.dart -o /app/bin/server", content);
        Assert.DoesNotContain("bin/dev.dart", content);

        await Verify(content);
    }

    // ---- Authored Dockerfile -------------------------------------------------

    [Fact]
    public void VerifyPublish_SkipsDockerfileGeneration_WhenDockerfileExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        // A Serverpod project ships a Dockerfile, and that file is the contract of the repository.
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Dockerfile"), "FROM scratch");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        var app = builder.AddDartApp("api", sourceDir.FullName);

        Assert.False(app.Resource.TryGetLastAnnotation<DockerfileBuilderCallbackAnnotation>(out _),
            "No DockerfileBuilderCallbackAnnotation should be added when a Dockerfile already exists");
    }

    // ---- Container files -----------------------------------------------------

    [Fact]
    public async Task VerifyPublish_ContainerFiles_GeneratesCopyInstructions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(
            DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");

        var frontend = builder.AddResource(new DartFilesContainer("frontend", "node", "."))
            .PublishAsDockerFile(c =>
                c.WithDockerfileBuilder(".", ctx => ctx.Builder.From("scratch"))
                 .WithImageTag("deterministic-tag"))
            .WithAnnotation(new ContainerFilesSourceAnnotation { SourcePath = "/app/dist" });

        var api = builder.AddDartApp("api", sourceDir.FullName);
        api.PublishWithContainerFiles(frontend, "/app/static");

        builder.Build().Run();

        var dockerfile = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("frontend", dockerfile);
        Assert.Contains("COPY --from=", dockerfile);
        Assert.Contains("/app/dist", dockerfile);
        Assert.Contains("/app/static", dockerfile);
    }

    // ---- .dockerignore -------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_WritesDockerignore()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("api", sourceDir.FullName);

        builder.Build().Run();

        var ignorePath = Path.Combine(outputDir.FullName, "api.Dockerfile.dockerignore");
        Assert.True(File.Exists(ignorePath), "A .dockerignore should be generated next to the Dockerfile");

        var content = await File.ReadAllTextAsync(ignorePath, TestContext.Current.CancellationToken);

        Assert.Contains(".dart_tool", content);
        Assert.Contains("build", content);
        Assert.Contains(".git", content);

        await Verify(content);
    }

    // ---- Serverpod -----------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_Serverpod_ProductionModeAndMigrations()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "myapp_server");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddServerpodApp("api", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        // A deployed server must not read the development configuration, and it must bring the
        // schema of the database up to date before it serves.
        Assert.Contains("""CMD ["/app/bin/server","--mode","production","--apply-migrations"]""", content);
        Assert.Contains("ENV SERVERPOD_RUN_MODE=production", content);

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_Serverpod_RespectsModeAndMigrationOverrides()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "myapp_server");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddServerpodApp("api", sourceDir.FullName)
               .WithServerpodMode("staging")
               .WithApplyMigrations(false);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("""CMD ["/app/bin/server","--mode","staging"]""", content);
        Assert.Contains("ENV SERVERPOD_RUN_MODE=staging", content);
        Assert.DoesNotContain("--apply-migrations", content);
    }

    // ---- The static-site shape -----------------------------------------------

    [Fact]
    public async Task VerifyPublish_StaticSite_FlutterWeb()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("web", sourceDir.FullName)
               .WithStaticSiteBuild("flutter", ["build", "web", "--release"], "build/web");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("RUN flutter build web --release", content);
        Assert.Contains("FROM docker.io/library/nginx:alpine", content);
        Assert.Contains("COPY --from=build /app/build/web /usr/share/nginx/html", content);
        Assert.Contains("EXPOSE 80", content);

        // A static site holds no Dart process, so the image must not compile one.
        Assert.DoesNotContain("dart compile exe", content);

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_StaticSite_FlutterWeb_UsesFlutterBuildImage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");

        // The official dart image holds the SDK only, so `flutter build web` needs an image that
        // carries Flutter.
        builder.AddDartApp("web", sourceDir.FullName)
               .WithStaticSiteBuild(
                   "flutter",
                   ["build", "web", "--release"],
                   "build/web",
                   spaFallback: true,
                   buildImage: "ghcr.io/cirruslabs/flutter:stable");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("FROM ghcr.io/cirruslabs/flutter:stable AS build", content);
        Assert.Contains("RUN flutter build web --release", content);

        // The parameter changes the build stage only. The runtime stage stays the web server.
        Assert.Contains("FROM docker.io/library/nginx:alpine", content);
        Assert.DoesNotContain("docker.io/library/dart:", content);

        await Verify(content);
    }

    [Fact]
    public async Task VerifyPublish_StaticSite_DockerfileBaseImageWinsOverStaticSiteBuildImage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");

        // WithDockerfileBaseImage is the general override of both stage images, so it wins.
        builder.AddDartApp("web", sourceDir.FullName)
               .WithStaticSiteBuild("flutter", ["build", "web"], "build/web", buildImage: "ghcr.io/cirruslabs/flutter:stable")
               .WithDockerfileBaseImage(buildImage: "example.invalid/custom-flutter:1.0");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        Assert.Contains("FROM example.invalid/custom-flutter:1.0 AS build", content);
        Assert.DoesNotContain("ghcr.io/cirruslabs/flutter:stable", content);
    }

    [Fact]
    public async Task VerifyPublish_StaticSite_SpaFallback()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("web", sourceDir.FullName)
               .WithStaticSiteBuild("flutter", ["build", "web"], "build/web", spaFallback: true);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        // The browser owns the routes, so every path that names no file returns the one page.
        Assert.Contains("try_files $uri $uri/ /index.html;", content);
        Assert.Contains("/etc/nginx/conf.d/default.conf", content);

        await Verify(content);
    }

    [Fact]
    public void VerifyPublish_StaticSite_SetsEndpointTargetPort80()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var app = builder.AddDartApp("web", sourceDir.FullName)
                         .WithStaticSiteBuild("flutter", ["build", "web"], "build/web");

        // The default Nginx image binds port 80, so the endpoint must reach that port.
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());

        Assert.Equal("http", endpoint.Name);
        Assert.Equal(80, endpoint.TargetPort);
    }

    [Fact]
    public void VerifyPublish_StaticSite_LeavesTargetPortAloneInRunMode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddDartApp("web", sourceDir.FullName)
                         .WithHttpEndpoint(env: "PORT")
                         .WithStaticSiteBuild("flutter", ["build", "web"], "build/web");

        // In run mode the local process binds the port, and Nginx is not involved.
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());

        Assert.Null(endpoint.TargetPort);
    }

    [Fact]
    public async Task VerifyPublish_ShellQuote_HandlesValuesThatTheShellWouldRead()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddDartApp("web", sourceDir.FullName)
               .WithStaticSiteBuild("build.sh", ["--name", "it's alive", "--flag=$HOME"], "build/web");

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        // A value that holds a space, a single quote, or a dollar sign must reach the command as
        // text. An ordinary word stays without quotation marks, so the file is easy to read.
        Assert.Contains("""RUN build.sh --name 'it'\''s alive' '--flag=$HOME'""", content);

        await Verify(content);
    }

    [Fact]
    public void WithStaticSiteBuild_ThrowsOnEmptyCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddDartApp("web", builder.AppHostDirectory);

        Assert.Throws<ArgumentException>(() => app.WithStaticSiteBuild("", ["build"], "build/web"));
        Assert.Throws<ArgumentNullException>(() => app.WithStaticSiteBuild("flutter", null!, "build/web"));
        Assert.Throws<ArgumentException>(() => app.WithStaticSiteBuild("flutter", ["build"], ""));
    }

    // ---- Jaspr ---------------------------------------------------------------

    [Fact]
    public async Task Jaspr_StaticMode_UsesStaticSiteBuild()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app", jasprMode: "static");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddJasprApp("web", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        // `jaspr build` writes the pre-rendered pages into build/jaspr, and no Dart process is left.
        Assert.Contains("RUN dart pub global activate jaspr_cli", content);
        Assert.Contains("RUN jaspr build", content);
        Assert.Contains("FROM docker.io/library/nginx:alpine", content);
        Assert.Contains("COPY --from=build /app/build/jaspr /usr/share/nginx/html", content);

        await Verify(content);
    }

    [Fact]
    public async Task Jaspr_ServerMode_UsesCompiledExecutable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "web_app", jasprMode: "server");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddJasprApp("web", sourceDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "web.Dockerfile"), TestContext.Current.CancellationToken);

        // In server mode `jaspr build` compiles the server to build/jaspr/app and writes the browser
        // bundle into build/jaspr/web. The server reads the bundle from the directory that holds the
        // executable, so the image receives the complete directory.
        Assert.Contains("RUN jaspr build", content);
        Assert.Contains("FROM scratch", content);
        Assert.Contains("COPY --from=build /runtime/ /", content);
        Assert.Contains("COPY --from=build /app/build/jaspr/ /app/", content);
        Assert.Contains("""CMD ["/app/app"]""", content);
        Assert.DoesNotContain("nginx", content);

        await Verify(content);
    }

    [Fact]
    public void Jaspr_WithJasprMode_ReplacesTheShapeOfTheEarlierMode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");

        WritePubspec(sourceDir.FullName, "web_app", jasprMode: "static");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var app = builder.AddJasprApp("web", sourceDir.FullName)
                         .WithJasprMode(JasprMode.Server);

        // Only one shape may stay on the resource, and the server shape binds no Nginx port.
        Assert.Empty(app.Resource.Annotations.OfType<DartStaticSiteBuildAnnotation>());
        Assert.Single(app.Resource.Annotations.OfType<DartCompiledBuildAnnotation>());
        Assert.Null(Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>()).TargetPort);
    }

    // ---- Manifest ------------------------------------------------------------

    [Fact]
    public async Task AddDartAppProducesDockerfileResourceInManifest()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var outputDir = workspace.CreateDirectory("output");

        WritePubspec(sourceDir.FullName, "api_app");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        var app = builder.AddDartApp("api", sourceDir.FullName);

        var manifest = await ManifestUtils.GetManifest(app.Resource, manifestDirectory: sourceDir.FullName);

        var expectedManifest = """
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "api.Dockerfile"
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    /// <summary>
    /// Writes the smallest <c>pubspec.yaml</c> that makes the directory a pub package.
    /// </summary>
    private static void WritePubspec(string appDirectory, string packageName, string? jasprMode = null)
    {
        var pubspec = $"""
            name: {packageName}
            environment:
              sdk: ^3.9.0

            """;

        if (jasprMode is not null)
        {
            pubspec += $"""
                jaspr:
                  mode: {jasprMode}

                """;
        }

        File.WriteAllText(Path.Combine(appDirectory, "pubspec.yaml"), pubspec);
        File.WriteAllText(Path.Combine(appDirectory, "pubspec.lock"), "packages: {}\n");
    }

    // A minimal resource that provides container files, so the tests can call
    // PublishWithContainerFiles without a dependency on a real container integration.
    private sealed class DartFilesContainer(string name, string command, string workingDirectory)
        : ExecutableResource(name, command, workingDirectory), IResourceWithContainerFiles;
}
