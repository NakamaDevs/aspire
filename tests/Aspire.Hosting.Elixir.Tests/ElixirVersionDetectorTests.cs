// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Elixir.Tests;

public class ElixirVersionDetectorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void Detect_ReadsToolVersionsInAppDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "elixir 1.19.5-otp-28\nerlang 28.4.1\n");

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal("1.19.5", info.ElixirVersion);
        Assert.Equal("28.4.1", info.OtpVersion);
        Assert.Equal(ElixirVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_ReadsToolVersionsInParentDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "elixir 1.18.3-otp-27\nerlang 27.2\n");
        var appDirectory = workspace.CreateDirectory("apps").CreateSubdirectory("api").FullName;

        var info = ElixirVersionDetector.Detect(appDirectory);

        Assert.Equal("1.18.3", info.ElixirVersion);
        Assert.Equal("27.2", info.OtpVersion);
        Assert.Equal(ElixirVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_ParsesOtpSuffixFromElixirEntry()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "elixir 1.18.3-otp-27\n");

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal("1.18.3", info.ElixirVersion);
        Assert.Equal("27", info.OtpVersion);
        Assert.Equal(ElixirVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_ReadsErlangEntry()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "erlang 28.4.1\nelixir 1.19.2\n");

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal("1.19.2", info.ElixirVersion);
        Assert.Equal("28.4.1", info.OtpVersion);
        Assert.Equal(ElixirVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_FallsBackToMixExsRequirement_WhenNoToolVersions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, "mix.exs"),
            """
            defmodule Api.MixProject do
              use Mix.Project

              def project do
                [app: :api, version: "0.1.0", elixir: "~> 1.19"]
              end
            end
            """);

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal("1.19.0", info.ElixirVersion);
        Assert.Equal(ElixirVersionDetector.DefaultOtpVersion, info.OtpVersion);
        Assert.Equal(ElixirVersionSource.MixExs, info.Source);
    }

    [Fact]
    public void Detect_PrefersToolVersionsOverMixExs()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, ".tool-versions"),
            "elixir 1.19.5-otp-28\nerlang 28.4.1\n");
        File.WriteAllText(
            Path.Combine(workspace.Path, "mix.exs"),
            """
            defmodule Api.MixProject do
              def project, do: [app: :api, elixir: ">= 1.14.0"]
            end
            """);

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal("1.19.5", info.ElixirVersion);
        Assert.Equal("28.4.1", info.OtpVersion);
        Assert.Equal(ElixirVersionSource.ToolVersions, info.Source);
    }

    [Fact]
    public void Detect_ReturnsDefault_WhenNothingFound()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal(ElixirVersionDetector.DefaultElixirVersion, info.ElixirVersion);
        Assert.Equal(ElixirVersionDetector.DefaultOtpVersion, info.OtpVersion);
        Assert.Equal(ElixirVersionSource.Default, info.Source);
    }

    [Fact]
    public void Detect_ReturnsDefault_WhenMixExsHasNoElixirRequirement()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        File.WriteAllText(
            Path.Combine(workspace.Path, "mix.exs"),
            """
            defmodule Api.MixProject do
              def project, do: [app: :api, version: "0.1.0"]
            end
            """);

        var info = ElixirVersionDetector.Detect(workspace.Path);

        Assert.Equal(ElixirVersionDetector.DefaultElixirVersion, info.ElixirVersion);
        Assert.Equal(ElixirVersionDetector.DefaultOtpVersion, info.OtpVersion);
        Assert.Equal(ElixirVersionSource.Default, info.Source);
    }
}
