// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart.Tests;

public class DartVersionDetectorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void Detect_ReadsToolVersionsDart()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "dart 3.12.2\n");

        var info = DartVersionDetector.Detect(workspace.Path);

        Assert.Equal("3.12.2", info.Version);
        Assert.Equal(DartVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_IgnoresFlutterLineAndFallsThrough()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // A `flutter` entry pins the Flutter SDK. The Dart version that ships inside that SDK is not
        // in the file, so the detector must continue with the next source.
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "flutter 3.44.8-stable\n");
        File.WriteAllText(
            Path.Combine(workspace.Path, "pubspec.yaml"),
            """
            name: api

            environment:
              sdk: ^3.10.0
            """);

        var info = DartVersionDetector.Detect(workspace.Path);

        Assert.Equal("3.10.0", info.Version);
        Assert.Equal(DartVersionSource.Pubspec, info.Source);
    }

    [Fact]
    public void Detect_ReadsToolVersionsInParentDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "dart 3.11.4\n");
        var appDirectory = workspace.CreateDirectory("apps").CreateSubdirectory("api").FullName;

        var info = DartVersionDetector.Detect(appDirectory);

        Assert.Equal("3.11.4", info.Version);
        Assert.Equal(DartVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_FallsBackToPubspecSdkConstraint_Caret()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, "pubspec.yaml"),
            """
            name: api

            environment:
              sdk: ^3.10.0

            dependencies:
              shelf: ^1.4.0
            """);

        var info = DartVersionDetector.Detect(workspace.Path);

        Assert.Equal("3.10.0", info.Version);
        Assert.Equal(DartVersionSource.Pubspec, info.Source);
    }

    [Fact]
    public void Detect_FallsBackToPubspecSdkConstraint_Range()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, "pubspec.yaml"),
            """
            name: api

            environment:
              sdk: '>=3.10 <4.0.0'
            """);

        var info = DartVersionDetector.Detect(workspace.Path);

        // The lower bound gives two parts only, so the detector pads it to a full version.
        Assert.Equal("3.10.0", info.Version);
        Assert.Equal(DartVersionSource.Pubspec, info.Source);
    }

    [Fact]
    public void Detect_PrefersToolVersionsOverPubspec()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "dart 3.12.2\n");
        File.WriteAllText(
            Path.Combine(workspace.Path, "pubspec.yaml"),
            """
            name: api

            environment:
              sdk: ^3.10.0
            """);

        var info = DartVersionDetector.Detect(workspace.Path);

        Assert.Equal("3.12.2", info.Version);
        Assert.Equal(DartVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_ReturnsDefault_WhenNothingFound()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var info = DartVersionDetector.Detect(workspace.Path);

        Assert.Equal(DartVersionDetector.DefaultDartVersion, info.Version);
        Assert.Equal(DartVersionSource.Default, info.Source);
    }
}
