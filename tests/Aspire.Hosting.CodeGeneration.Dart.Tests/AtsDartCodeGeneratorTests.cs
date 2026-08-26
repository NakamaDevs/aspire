// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;
using Aspire.Hosting.RemoteHost;
using Aspire.TestUtilities;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Dart.Tests;

public class AtsDartCodeGeneratorTests(ITestOutputHelper outputHelper)
{
    private readonly AtsDartCodeGenerator _generator = new();

    // The test types are compiled into this assembly via Compile Include
    private const string TestTypesAssemblyName = "Aspire.Hosting.CodeGeneration.Dart.Tests";

    [Fact]
    public void Language_ReturnsDart()
    {
        Assert.Equal("Dart", _generator.Language);
    }

    [Fact]
    public void GeneratedCode_HasAspireDartFile()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());

        Assert.Contains("aspire.dart", files.Keys);
        Assert.Contains("aspire_generated.dart", files.Keys);
        Assert.Contains("base.dart", files.Keys);
        Assert.Contains("transport.dart", files.Keys);
        Assert.Contains("aspire_runtime.dart", files.Keys);

        var entry = files["aspire.dart"];

        // The entry file owns the generated parts and re-exports the runtime files, so one import
        // brings the whole SDK into scope.
        Assert.Contains("import 'base.dart';", entry, StringComparison.Ordinal);
        Assert.Contains("import 'transport.dart';", entry, StringComparison.Ordinal);
        Assert.Contains("import 'aspire_runtime.dart';", entry, StringComparison.Ordinal);
        Assert.Contains("export 'base.dart';", entry, StringComparison.Ordinal);
        Assert.Contains("export 'transport.dart';", entry, StringComparison.Ordinal);
        Assert.Contains("export 'aspire_runtime.dart';", entry, StringComparison.Ordinal);
        Assert.Contains("part 'aspire_generated.dart';", entry, StringComparison.Ordinal);

        // Every generated part names the entry file, so the library holds one scope.
        foreach (var (name, content) in files)
        {
            if (name.StartsWith("aspire_generated", StringComparison.Ordinal))
            {
                Assert.Contains("part of 'aspire.dart';", content, StringComparison.Ordinal);
                Assert.Contains($"part '{name}';", entry, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void GeneratedCode_HasCreateBuilderFunction()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        var entry = files["aspire.dart"];

        Assert.Contains("Future<DistributedApplicationBuilder> createBuilder(", entry, StringComparison.Ordinal);
        Assert.Contains("AspireTransport? transport,", entry, StringComparison.Ordinal);
        Assert.Contains("transport ?? await AspireTransport.connect();", entry, StringComparison.Ordinal);
        Assert.Contains("'Aspire.Hosting/createBuilder',", entry, StringComparison.Ordinal);

        // `builder.build()` and `app.run()` are the two calls the scaffolded AppHost makes, so both
        // classes have to carry them.
        var generated = GenerateSource(CreateContextFromBothAssemblies());
        var builder = ExtractClass(generated, "DistributedApplicationBuilder");
        Assert.Contains("Future<DistributedApplication> build(", builder, StringComparison.Ordinal);

        var application = ExtractClass(generated, "DistributedApplication");
        Assert.Contains("> run(", application, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_EmitsTransportAndBaseVerbatim()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromTestAssembly());

        // The runtime files are copied without a change so the Dart tests under
        // Aspire.Hosting.CodeGeneration.Dart.DartTests cover exactly what ships.
        Assert.Equal(ReadResource("base.dart"), files["base.dart"]);
        Assert.Equal(ReadResource("transport.dart"), files["transport.dart"]);
        Assert.Equal(ReadResource("aspire_runtime.dart"), files["aspire_runtime.dart"]);
    }

    [Fact]
    public void GeneratedCode_UsesCamelCaseMethodNames()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        Assert.Contains("> addContainer(", generated, StringComparison.Ordinal);
        Assert.Contains("> withEnvironment(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("> AddContainer(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("> add_container(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("> with_environment(", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_SanitizesDartKeywordParameters()
    {
        // A reserved Dart word cannot be a method name or a parameter name. The generator appends a
        // trailing underscore, so no generated signature holds a bare keyword.
        HashSet<string> reserved = new(StringComparer.Ordinal)
        {
            "assert", "break", "case", "catch", "class", "const", "continue", "default", "do",
            "else", "enum", "extends", "false", "final", "finally", "for", "if", "in", "is", "new",
            "null", "rethrow", "return", "super", "switch", "this", "throw", "true", "try", "var",
            "void", "while", "with", "await", "yield"
        };

        var generated = GenerateSource(CreateContextFromBothAssemblies());
        var checkedSignatures = 0;

        foreach (var line in generated.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("Future<", StringComparison.Ordinal) || !trimmed.EndsWith(" async {", StringComparison.Ordinal))
            {
                continue;
            }

            var open = trimmed.IndexOf('(', StringComparison.Ordinal);
            var close = trimmed.LastIndexOf(')');
            if (open < 0 || close < open)
            {
                continue;
            }

            var beforeParameters = trimmed[..open];
            var methodName = beforeParameters[(beforeParameters.LastIndexOf(' ') + 1)..];
            Assert.False(reserved.Contains(methodName), $"Generated method name '{methodName}' is a reserved Dart word.");

            foreach (var parameter in trimmed[(open + 1)..close]
                .Replace("{", "", StringComparison.Ordinal)
                .Replace("}", "", StringComparison.Ordinal)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                // A parameter is "<type> <name>". Generic type arguments never hold a comma in the
                // generated code, so the split above is safe.
                var space = parameter.LastIndexOf(' ');
                if (space < 0)
                {
                    continue;
                }

                var name = parameter[(space + 1)..];
                Assert.False(reserved.Contains(name), $"Generated parameter '{name}' is a reserved Dart word in: {trimmed}");
            }

            checkedSignatures++;
        }

        Assert.True(checkedSignatures > 100, $"Expected many generated signatures, found {checkedSignatures}.");
    }

    [Fact]
    public void GeneratedCode_OptionalParametersAreNamed()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());

        // addTestRedis(builder, name, port = null) -> addTestRedis(String name, {num? port})
        Assert.Contains(
            "Future<TestRedisResource> addTestRedis(String name, {num? port}) async {",
            generated,
            StringComparison.Ordinal);

        // A named argument is only sent when the caller passed it, so the host keeps its default.
        Assert.Contains("if (port != null) {", generated, StringComparison.Ordinal);
        Assert.Contains("args['port'] = port;", generated, StringComparison.Ordinal);

        // A cancellation token is always a named parameter, even without a .NET default.
        var module = ExtractClass(generated, "TestRedisResource");
        Assert.Contains(
            "getStatusAsync({CancellationToken? cancellationToken}) async {",
            module,
            StringComparison.Ordinal);
        Assert.Contains(
            "waitForReadyAsync(num timeout, {CancellationToken? cancellationToken}) async {",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_ReturnsBuilderCapabilitiesReturnReceiverType()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());
        var module = ExtractClass(generated, "TestRedisResource");

        // A fluent capability returns the receiver, so a call chain stays on the same class. The
        // declared .NET return type is often an interface or a base type, and decoding into that
        // type would end the chain on a class that has no methods.
        Assert.Contains("Future<TestRedisResource> withPersistence(", module, StringComparison.Ordinal);
        Assert.Contains("Future<TestRedisResource> withEnvironment(", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Future<ResourceWithEnvironment> withEnvironment(", module, StringComparison.Ordinal);
        Assert.Contains(
            "return TestRedisResource(AspireRuntime.requireHandle(result, "
                + "'Aspire.Hosting.CodeGeneration.Dart.Tests/withPersistence'), transport);",
            module,
            StringComparison.Ordinal);

        // A factory method returns a different builder, so it keeps its declared return type.
        Assert.Contains("Future<TestDatabaseResource> addTestChildDatabase(", module, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput()
    {
        var atsContext = CreateContextFromTestAssembly();

        var files = _generator.GenerateDistributedApplication(atsContext);

        Assert.Contains("aspire.dart", files.Keys);
        Assert.Contains("aspire_generated.dart", files.Keys);
        Assert.Contains("transport.dart", files.Keys);
        Assert.Contains("base.dart", files.Keys);
        Assert.Contains("aspire_runtime.dart", files.Keys);

        await Verify(GenerateSource(atsContext), extension: "dart")
            .UseFileName("AtsGeneratedAspire");
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_IncludesCapabilities()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        Assert.NotEmpty(capabilities);
        Assert.Contains(capabilities, c => c.CapabilityId == $"{TestTypesAssemblyName}/addTestRedis");
        Assert.Contains(capabilities, c => c.CapabilityId == $"{TestTypesAssemblyName}/withPersistence");
        Assert.Contains(capabilities, c => c.CapabilityId == $"{TestTypesAssemblyName}/withOptionalString");
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_CapturesParameters()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var addTestRedis = capabilities.First(c => c.CapabilityId == $"{TestTypesAssemblyName}/addTestRedis");
        Assert.Equal(2, addTestRedis.Parameters.Count);
        Assert.Equal("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", addTestRedis.TargetTypeId);
        Assert.Contains(addTestRedis.Parameters, p => p.Name == "name" && p.Type?.TypeId == "string");
        Assert.Contains(addTestRedis.Parameters, p => p.Name == "port" && p.IsOptional);
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var addTestRedis = capabilities.First(c => c.CapabilityId == $"{TestTypesAssemblyName}/addTestRedis");
        Assert.Equal("addTestRedis", addTestRedis.MethodName);

        var withPersistence = capabilities.First(c => c.CapabilityId == $"{TestTypesAssemblyName}/withPersistence");
        Assert.Equal("withPersistence", withPersistence.MethodName);
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_IncludesExportedValues()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());

        // Dart has no nested namespace, so the path above the value name becomes the class name.
        Assert.Contains("abstract final class TestConfigs {", generated, StringComparison.Ordinal);
        Assert.Contains("abstract final class TestConfigsProfiles {", generated, StringComparison.Ordinal);

        var catalog = ExtractClass(generated, "TestConfigs");
        // `default` is a reserved Dart word, so the getter takes a trailing underscore.
        Assert.Contains("static TestConfigDto? get default_ =>", catalog, StringComparison.Ordinal);
        Assert.Contains("The default test configuration.", catalog, StringComparison.Ordinal);
        Assert.Contains("static String get unicodeGreeting =>", catalog, StringComparison.Ordinal);
        Assert.Contains("你好こんにちは", catalog, StringComparison.Ordinal);

        var profiles = ExtractClass(generated, "TestConfigsProfiles");
        Assert.Contains("static TestConfigDto? get development =>", profiles, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scanner_AddTestRedis_HasCorrectTypeMetadata()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var addTestRedis = capabilities.FirstOrDefault(c => c.CapabilityId == $"{TestTypesAssemblyName}/addTestRedis");
        Assert.NotNull(addTestRedis);

        await Verify(addTestRedis).UseFileName("AddTestRedisCapability");
    }

    [Fact]
    public async Task Scanner_HostingAssembly_AddContainerCapability()
    {
        var capabilities = ScanCapabilitiesFromHostingAssembly();

        var addContainer = capabilities.FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting/addContainer");
        Assert.NotNull(addContainer);

        await Verify(addContainer).UseFileName("HostingAddContainerCapability");
    }

    [Fact]
    public void Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var addTestRedis = capabilities.FirstOrDefault(c => c.CapabilityId == $"{TestTypesAssemblyName}/addTestRedis");
        Assert.NotNull(addTestRedis);
        Assert.True(addTestRedis.ReturnsBuilder,
            "addTestRedis returns IResourceBuilder<T> but ReturnsBuilder is false - fluent chaining won't work");

        var withPersistence = capabilities.FirstOrDefault(c => c.CapabilityId == $"{TestTypesAssemblyName}/withPersistence");
        Assert.NotNull(withPersistence);
        Assert.True(withPersistence.ReturnsBuilder,
            "withPersistence returns IResourceBuilder<T> but ReturnsBuilder is false - fluent chaining won't work");
    }

    [Fact]
    public async Task Scanner_WithOptionalString_HasCorrectExpandedTargets()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var withOptionalString = capabilities.FirstOrDefault(c => c.CapabilityId == $"{TestTypesAssemblyName}/withOptionalString");
        Assert.NotNull(withOptionalString);

        await Verify(withOptionalString).UseFileName("WithOptionalStringCapability");
    }

    [Fact]
    public async Task Scanner_WithPersistence_HasCorrectExpandedTargets()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var withPersistence = capabilities.FirstOrDefault(c => c.CapabilityId == $"{TestTypesAssemblyName}/withPersistence");
        Assert.NotNull(withPersistence);

        await Verify(withPersistence).UseFileName("WithPersistenceCapability");
    }

    [Fact]
    public void TwoPassScanning_DeduplicatesCapabilities()
    {
        var capabilities = ScanCapabilitiesFromBothAssemblies();

        var duplicates = capabilities
            .GroupBy(c => c.CapabilityId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        var testRedisModule = ExtractClass(generated, "TestRedisResource");

        Assert.Contains("> withEnvironment(", testRedisModule, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoPassScanning_MergesHandleTypesFromAllAssemblies()
    {
        var result = CreateContextFromBothAssemblies();

        var containerResourceType = result.HandleTypes
            .FirstOrDefault(t => t.AtsTypeId.Contains("ContainerResource") && !t.AtsTypeId.Contains("IContainer"));
        Assert.NotNull(containerResourceType);

        var testRedisType = result.HandleTypes
            .FirstOrDefault(t => t.AtsTypeId.Contains("TestRedisResource"));
        Assert.NotNull(testRedisType);

        var hasEnvironmentInterface = testRedisType.ImplementedInterfaces
            .Any(i => i.TypeId.Contains("IResourceWithEnvironment"));
        Assert.True(hasEnvironmentInterface,
            "TestRedisResource should implement IResourceWithEnvironment via ContainerResource");
    }

    [Fact]
    public void RuntimeType_ContainerResource_IsNotInterface()
    {
        var containerResourceType = typeof(ContainerResource);

        Assert.NotNull(containerResourceType);
        Assert.False(containerResourceType.IsInterface, "ContainerResource should NOT be an interface");
    }

    [Fact]
    [RequiresTools(["dart"])]
    public async Task GeneratedCode_AnalyzesCleanWithDart()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var modules = Path.Combine(workspace.Path, ".aspire", "modules");
        Directory.CreateDirectory(modules);

        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        foreach (var (name, content) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(modules, name), content);
        }

        // The scaffolded manifest declares no dependency, so `dart pub get` needs no network.
        var scaffold = new DartLanguageSupport().Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "AnalyzeApp",
            PortSeed = 1
        });

        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "pubspec.yaml"), scaffold["pubspec.yaml"]);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "apphost.dart"), scaffold["apphost.dart"]);

        var restore = await RunAsync("dart", workspace.Path, ["pub", "get"]);
        outputHelper.WriteLine(restore.Output);
        Assert.Equal(0, restore.ExitCode);

        // The analyzer skips a directory whose name starts with a dot, so the generated SDK is
        // named on the command line.
        var analyze = await RunAsync(
            "dart",
            workspace.Path,
            ["analyze", "--fatal-infos", Path.Combine(".aspire", "modules"), "apphost.dart"]);
        outputHelper.WriteLine(analyze.Output);
        Assert.Equal(0, analyze.ExitCode);
    }

    [Fact]
    [RequiresTools(["dart"])]
    public async Task GeneratedCode_ScaffoldedAppHostRunsUntilMissingSocket()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var modules = Path.Combine(workspace.Path, ".aspire", "modules");
        Directory.CreateDirectory(modules);

        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        foreach (var (name, content) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(modules, name), content);
        }

        var scaffold = new DartLanguageSupport().Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "RunApp",
            PortSeed = 2
        });

        foreach (var (name, content) in scaffold)
        {
            await File.WriteAllTextAsync(Path.Combine(workspace.Path, name), content);
        }

        var restore = await RunAsync("dart", workspace.Path, ["pub", "get"]);
        outputHelper.WriteLine(restore.Output);
        Assert.Equal(0, restore.ExitCode);

        // Without REMOTE_APP_HOST_SOCKET_PATH the transport stops at MISSING_SOCKET_PATH. Reaching
        // that error proves the whole generated library compiled and loaded.
        var run = await RunAsync(
            "dart",
            workspace.Path,
            ["run", "apphost.dart"],
            new Dictionary<string, string?> { ["REMOTE_APP_HOST_SOCKET_PATH"] = null });

        outputHelper.WriteLine(run.Output);
        Assert.NotEqual(0, run.ExitCode);
        Assert.Contains("MISSING_SOCKET_PATH", run.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Error:", run.Output, StringComparison.Ordinal);
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(
        string fileName,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in environment ?? new Dictionary<string, string?>())
        {
            if (value is null)
            {
                startInfo.Environment.Remove(name);
            }
            else
            {
                startInfo.Environment[name] = value;
            }
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}.");

        var output = new StringBuilder();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        output.Append(await standardOutput).Append(await standardError);

        return (process.ExitCode, output.ToString());
    }

    /// <summary>
    /// Joins every generated part file so a test can assert over the whole generated surface.
    /// </summary>
    private string GenerateSource(AtsContext context)
    {
        var files = _generator.GenerateDistributedApplication(context);

        return string.Join(
            "\n",
            files
                .Where(file => file.Key.StartsWith("aspire_generated", StringComparison.Ordinal))
                .OrderBy(file => file.Key, StringComparer.Ordinal)
                .Select(file => file.Value));
    }

    private static string ExtractClass(string source, string className)
    {
        var start = source.IndexOf($"class {className} ", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Generated code does not define {className}.");

        // Walk back over the documentation comment so the class body starts at its first line.
        var end = source.IndexOf("\n}\n", start, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..(end + 3)];
    }

    private static string ReadResource(string name)
    {
        var assembly = typeof(AtsDartCodeGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Aspire.Hosting.CodeGeneration.Dart.Resources.{name}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<AtsCapabilityInfo> ScanCapabilitiesFromTestAssembly()
    {
        var result = AtsCapabilityScanner.ScanAssembly(LoadTestAssembly());
        return result.Capabilities;
    }

    private static AtsContext CreateContextFromTestAssembly()
    {
        var result = AtsCapabilityScanner.ScanAssembly(LoadTestAssembly());
        return result.ToAtsContext();
    }

    private static Assembly LoadTestAssembly() => typeof(TestRedisResource).Assembly;

    private static List<AtsCapabilityInfo> ScanCapabilitiesFromHostingAssembly()
    {
        var hostingAssembly = typeof(DistributedApplication).Assembly;
        var result = AtsCapabilityScanner.ScanAssembly(hostingAssembly);
        return result.Capabilities;
    }

    private static List<AtsCapabilityInfo> ScanCapabilitiesFromBothAssemblies()
    {
        var (testAssembly, hostingAssembly) = LoadBothAssemblies();
        var result = AtsCapabilityScanner.ScanAssemblies([hostingAssembly, testAssembly]);
        return result.Capabilities;
    }

    private static AtsContext CreateContextFromBothAssemblies()
    {
        var (testAssembly, hostingAssembly) = LoadBothAssemblies();
        var result = AtsCapabilityScanner.ScanAssemblies([hostingAssembly, testAssembly]);
        return result.ToAtsContext();
    }

    private static (Assembly testAssembly, Assembly hostingAssembly) LoadBothAssemblies()
    {
        var testAssembly = typeof(TestRedisResource).Assembly;
        var hostingAssembly = typeof(DistributedApplication).Assembly;
        return (testAssembly, hostingAssembly);
    }
}
