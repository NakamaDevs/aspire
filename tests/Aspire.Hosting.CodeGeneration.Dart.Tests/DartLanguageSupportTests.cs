// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Dart.Tests;

public class DartLanguageSupportTests(ITestOutputHelper outputHelper)
{
    private readonly DartLanguageSupport _languageSupport = new();

    [Fact]
    public void Language_ReturnsDart()
    {
        Assert.Equal("dart", _languageSupport.Language);
    }

    [Fact]
    public void Scaffold_CreatesApphostDartPubspecAndRunJson()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "DartApp"
        });

        Assert.Collection(
            files.Keys.Order(StringComparer.Ordinal),
            key => Assert.Equal(".gitignore", key),
            key => Assert.Equal("apphost.dart", key),
            key => Assert.Equal("apphost.run.json", key),
            key => Assert.Equal("pubspec.yaml", key));

        Assert.Contains(".aspire/", files[".gitignore"], StringComparison.Ordinal);
        Assert.Contains(".dart_tool/", files[".gitignore"], StringComparison.Ordinal);
    }

    [Fact]
    public void Scaffold_ApphostDartImportsGeneratedModule()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "DartApp"
        });

        var appHost = files["apphost.dart"];

        // AtsDartCodeGenerator emits .aspire/modules/aspire.dart as the single library entry point.
        Assert.Contains("import '.aspire/modules/aspire.dart';", appHost, StringComparison.Ordinal);
        Assert.Contains("Future<void> main(List<String> args) async {", appHost, StringComparison.Ordinal);
        Assert.Contains("final builder = await createBuilder(args);", appHost, StringComparison.Ordinal);
        Assert.Contains("final app = await builder.build();", appHost, StringComparison.Ordinal);
        Assert.Contains("await app.run();", appHost, StringComparison.Ordinal);
    }

    [Fact]
    public void Scaffold_PubspecHasNoDependencies()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _languageSupport.Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "DartApp"
        });

        var pubspec = files["pubspec.yaml"];

        // The generated SDK uses the Dart SDK libraries only, so `dart pub get` needs no network.
        Assert.Contains("name: apphost", pubspec, StringComparison.Ordinal);
        Assert.Contains("sdk: ^3.8.0", pubspec, StringComparison.Ordinal);
        Assert.DoesNotContain("dependencies:", pubspec, StringComparison.Ordinal);
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
    public void Detect_ReturnsDartAppHostWhenMarkerAndPubspecExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        File.WriteAllText(Path.Combine(workspace.Path, "apphost.dart"), "// marker");
        File.WriteAllText(Path.Combine(workspace.Path, "pubspec.yaml"), "name: apphost");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.True(result.IsValid);
        Assert.Equal("dart", result.Language);
        Assert.Equal("apphost.dart", result.AppHostFile);
    }

    [Fact]
    public void Detect_RequiresPubspec()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // `dart run` resolves packages through pubspec.yaml, so an AppHost file on its own is not
        // a runnable Dart AppHost.
        File.WriteAllText(Path.Combine(workspace.Path, "apphost.dart"), "// marker");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Detect_DoesNotTreatTypeScriptAppHostAsDart()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        File.WriteAllText(Path.Combine(workspace.Path, "apphost.ts"), "// typescript");

        var result = _languageSupport.Detect(workspace.Path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetRuntimeSpec_UsesDartRun()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        Assert.Equal("dart", runtimeSpec.Language);
        Assert.Equal("Dart", runtimeSpec.DisplayName);
        Assert.Equal("Dart", runtimeSpec.CodeGenLanguage);
        Assert.Equal(["apphost.dart"], runtimeSpec.DetectionPatterns);
        Assert.Equal("dart", runtimeSpec.ExtensionLaunchCapability);
        Assert.Equal("dart", runtimeSpec.Execute.Command);
        Assert.Equal(["run", "{appHostFile}"], runtimeSpec.Execute.Args);

        // Watch mode arrives with D2.6.
        Assert.Null(runtimeSpec.WatchExecute);
        Assert.Null(runtimeSpec.PreExecute);
        Assert.Null(runtimeSpec.Initialize);
    }

    [Fact]
    public void GetRuntimeSpec_InstallsWithPubGet()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        var installDependencies = Assert.IsType<CommandSpec>(runtimeSpec.InstallDependencies);
        Assert.Equal("dart", installDependencies.Command);
        Assert.Equal(["pub", "get"], installDependencies.Args);
    }

    [Fact]
    public void GetRuntimeSpec_SetsSslCertFile()
    {
        var runtimeSpec = _languageSupport.GetRuntimeSpec();

        // The Dart VM reads the PEM bundle that SSL_CERT_FILE names.
        Assert.Equal("SSL_CERT_FILE", _languageSupport.CertificateBundleEnvironmentVariable);
        Assert.Equal("SSL_CERT_FILE", runtimeSpec.CertificateBundleEnvironmentVariable);
    }

    private static int GetPort(string url) => new Uri(url).Port;

    private const int WindowsEphemeralPortMin = 49152;

    private static void AssertPortInRange(int port, int minInclusive, int maxExclusive)
    {
        Assert.InRange(port, minInclusive, maxExclusive - 1);
        Assert.True(port < WindowsEphemeralPortMin, $"Expected port {port} to be below the Windows ephemeral range.");
    }
}
