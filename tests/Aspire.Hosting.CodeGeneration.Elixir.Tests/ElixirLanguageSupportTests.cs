// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Elixir.Tests;

public class ElixirLanguageSupportTests(ITestOutputHelper outputHelper)
{
    private readonly ElixirLanguageSupport _languageSupport = new();

    [Fact]
    public void Language_ReturnsElixir()
    {
        Assert.Equal("elixir", _languageSupport.Language);
    }

    [Fact]
    public void Scaffold_CreatesApphostExsAndRunJson()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "ElixirApp"
        });

        Assert.Collection(
            files.Keys.Order(StringComparer.Ordinal),
            key => Assert.Equal(".gitignore", key),
            key => Assert.Equal("apphost.exs", key),
            key => Assert.Equal("apphost.run.json", key));

        // A mix project is not scaffolded: `elixir apphost.exs` needs no build and no dependency.
        Assert.DoesNotContain("mix.exs", files.Keys);
        Assert.Contains(".aspire/", files[".gitignore"], StringComparison.Ordinal);
        Assert.Contains("_build/", files[".gitignore"], StringComparison.Ordinal);
    }

    [Fact]
    public void Scaffold_ApphostExsRequiresGeneratedModule()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "ElixirApp"
        });

        var appHost = files["apphost.exs"];

        // The generator emits .aspire/modules/aspire.ex as the single loader entry point.
        Assert.Contains("Code.require_file(\".aspire/modules/aspire.ex\", __DIR__)", appHost, StringComparison.Ordinal);
        Assert.Contains("builder = Aspire.create_builder!()", appHost, StringComparison.Ordinal);
        Assert.Contains("|> Aspire.build!()", appHost, StringComparison.Ordinal);
        Assert.Contains("|> Aspire.run!()", appHost, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16626)]
    [InlineData(55571)]
    public void Scaffold_GeneratesProfilePortsOutsideWindowsEphemeralRange(int? portSeed)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "PortsApp",
            PortSeed = portSeed
        });

        var appHostRunJson = JsonNode.Parse(files["apphost.run.json"])!.AsObject();
        var httpsProfile = appHostRunJson["profiles"]!["https"]!.AsObject();
        var applicationUrls = httpsProfile["applicationUrl"]!.GetValue<string>().Split(';', StringSplitOptions.RemoveEmptyEntries);
        var environmentVariables = httpsProfile["environmentVariables"]!.AsObject();

        Assert.Equal(2, applicationUrls.Length);

        var httpsPort = GetPort(applicationUrls.Single(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));
        var httpPort = GetPort(applicationUrls.Single(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)));
        var otlpHttpsPort = GetPort(environmentVariables["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"]!.GetValue<string>());
        var resourceServiceHttpsPort = GetPort(environmentVariables["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"]!.GetValue<string>());

        AssertPortInRange(httpPort, 15000, 15300);
        AssertPortInRange(httpsPort, 17000, 17300);
        AssertPortInRange(otlpHttpsPort, 21000, 21300);
        AssertPortInRange(resourceServiceHttpsPort, 22000, 22300);
    }

    [Fact]
    public void Scaffold_IsDeterministicWithPortSeed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var request = new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "SeedApp",
            PortSeed = 4242
        };

        var first = _languageSupport.Scaffold(request);
        var second = _languageSupport.Scaffold(request);

        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), second.Keys.Order(StringComparer.Ordinal));
        foreach (var key in first.Keys)
        {
            Assert.Equal(first[key], second[key]);
        }
    }

    [Fact]
    public void Detect_ReturnsElixirAppHostWhenMarkerExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        File.WriteAllText(Path.Combine(workspace.Path, "apphost.exs"), "# marker");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.True(result.IsValid);
        Assert.Equal("elixir", result.Language);
        Assert.Equal("apphost.exs", result.AppHostFile);
    }

    [Fact]
    public void Detect_DoesNotTreatTypeScriptAppHostAsElixir()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        File.WriteAllText(Path.Combine(workspace.Path, "apphost.ts"), "// typescript");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Detect_ReturnsNotFoundForEmptyDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var result = _languageSupport.Detect(workspace.Path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetRuntimeSpec_UsesElixirCommandWithAppHostFile()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        Assert.Equal("elixir", runtimeSpec.Language);
        Assert.Equal("Elixir", runtimeSpec.DisplayName);
        Assert.Equal("Elixir", runtimeSpec.CodeGenLanguage);
        Assert.Equal(["apphost.exs"], runtimeSpec.DetectionPatterns);
        Assert.Equal("elixir", runtimeSpec.ExtensionLaunchCapability);
        Assert.Equal("elixir", runtimeSpec.Execute.Command);
        Assert.Equal(["{appHostFile}"], runtimeSpec.Execute.Args);
    }

    [Fact]
    public void GetRuntimeSpec_SetsSslCertFileAsCertificateBundleVariable()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        // Erlang's TLS stack reads the PEM bundle that SSL_CERT_FILE names.
        Assert.Equal("SSL_CERT_FILE", _languageSupport.CertificateBundleEnvironmentVariable);
        Assert.Equal("SSL_CERT_FILE", runtimeSpec.CertificateBundleEnvironmentVariable);
    }

    [Fact]
    public void GetRuntimeSpec_HasNoInstallStep()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        Assert.Null(runtimeSpec.InstallDependencies);
        Assert.Null(runtimeSpec.Initialize);
        Assert.Null(runtimeSpec.PreExecute);
        // Watch mode arrives with M2.8.
        Assert.Null(runtimeSpec.WatchExecute);
    }

    private static int GetPort(string url) => new Uri(url).Port;

    private const int WindowsEphemeralPortMin = 49152;

    private static void AssertPortInRange(int port, int minInclusive, int maxExclusive)
    {
        Assert.InRange(port, minInclusive, maxExclusive - 1);
        Assert.True(port < WindowsEphemeralPortMin, $"Expected port {port} to be below the Windows ephemeral range.");
    }
}
