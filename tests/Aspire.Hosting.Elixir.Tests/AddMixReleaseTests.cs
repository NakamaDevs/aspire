// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Elixir.Tests;

/// <summary>
/// Covers <see cref="ElixirHostingExtensions.AddMixRelease"/>, which runs a release that Mix already built.
/// </summary>
public class AddMixReleaseTests(ITestOutputHelper outputHelper)
{
    // ---- Guards --------------------------------------------------------------

    [Fact]
    public void AddMixReleaseShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddMixRelease("api", "/src/rel/api");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMixReleaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddMixRelease(name, "/src/rel/api");

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMixReleaseShouldThrowWhenReleaseDirectoryIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var releaseDirectory = isNull ? null! : string.Empty;

        var action = () => builder.AddMixRelease("api", releaseDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(releaseDirectory), exception.ParamName);
    }

    // ---- Release name and command --------------------------------------------

    [Fact]
    public void AddMixRelease_DefaultsReleaseNameToDirectoryName()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");

        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddMixRelease("api", releaseDir.FullName);

        Assert.Equal("hello_world", app.Resource.ReleaseName);
    }

    [Fact]
    public void AddMixRelease_UsesBinStartCommand()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows uses the .bat launcher, which a separate test covers.");

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");

        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddMixRelease("api", releaseDir.FullName);

        Assert.Equal(Path.Combine(releaseDir.FullName, "bin", "hello_world"), app.Resource.Command);
        Assert.Equal(releaseDir.FullName, app.Resource.WorkingDirectory);
        Assert.Equal(["start"], GetArgs(app.Resource));
    }

    [Fact]
    public void AddMixRelease_UsesStartBatOnWindows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The .bat launcher only exists in a Windows release.");

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");

        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddMixRelease("api", releaseDir.FullName);

        Assert.Equal(Path.Combine(releaseDir.FullName, "bin", "hello_world.bat"), app.Resource.Command);
    }

    [Fact]
    public void AddMixRelease_DoesNotAddMixDepsSibling()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");

        // A release carries its own dependencies, so no Mix step must appear.
        File.WriteAllText(Path.Combine(releaseDir.FullName, "mix.exs"), "defmodule X do end");

        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMixRelease("api", releaseDir.FullName);

        Assert.DoesNotContain(builder.Resources, r => r.Name.Contains("mix", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(builder.Resources, r => r.Name is "api-deps" or "api-compile");
    }

    [Fact]
    public void AddMixRelease_SetsOtlpExporter()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");

        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddMixRelease("api", releaseDir.FullName);

        Assert.True(app.Resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out _));
        Assert.True(app.Resource.TryGetLastAnnotation<OtlpExporterAnnotation>(out _));
    }

    // ---- Manifest ------------------------------------------------------------

    [Fact]
    public async Task VerifyManifest_AddMixRelease()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");

        using var builder = TestDistributedApplicationBuilder.Create();
        var app = builder.AddMixRelease("api", releaseDir.FullName);

        var manifest = await ManifestUtils.GetManifest(app.Resource, manifestDirectory: releaseDir.FullName);

        Assert.Equal("executable.v0", manifest["type"]!.ToString());
        Assert.Equal(".", manifest["workingDirectory"]!.ToString());

        // The manifest carries the command as the model holds it, so the path stays absolute.
        var expectedSuffix = OperatingSystem.IsWindows() ? "/bin/hello_world.bat" : "/bin/hello_world";
        Assert.EndsWith(expectedSuffix, manifest["command"]!.ToString().Replace('\\', '/'), StringComparison.Ordinal);

        Assert.Equal(["start"], manifest["args"]!.AsArray().Select(a => a!.ToString()));
    }

    // ---- Publish -------------------------------------------------------------

    [Fact]
    public async Task VerifyPublish_AddMixRelease_CopiesReleaseWithoutBuildStage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var releaseDir = workspace.CreateDirectory("hello_world");
        var outputDir = workspace.CreateDirectory("output");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputDir.FullName, step: "publish-manifest");
        builder.AddMixRelease("api", releaseDir.FullName);

        builder.Build().Run();

        var content = await File.ReadAllTextAsync(Path.Combine(outputDir.FullName, "api.Dockerfile"), TestContext.Current.CancellationToken);

        // A prebuilt release never compiles, so the image must have exactly one stage.
        Assert.Equal(1, content.Split('\n').Count(l => l.StartsWith("FROM ", StringComparison.Ordinal)));
        Assert.DoesNotContain("mix ", content);
        Assert.DoesNotContain("AS build", content);

        await Verify(content);
    }

    private static string[] GetArgs(IResource resource)
    {
        var args = new List<object>();
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var annotations))
        {
            var context = new CommandLineArgsCallbackContext(args, CancellationToken.None);
            foreach (var annotation in annotations)
            {
                annotation.Callback(context).GetAwaiter().GetResult();
            }
        }

        return args.Select(a => a.ToString()!).ToArray();
    }
}
