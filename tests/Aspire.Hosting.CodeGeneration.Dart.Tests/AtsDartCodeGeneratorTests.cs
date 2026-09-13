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
    public void GeneratedCode_RunClosesTransport()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());
        var run = ExtractMethod(ExtractClass(generated, "DistributedApplication"), "run");

        // The socket subscription keeps the Dart event loop alive, so `dart run apphost.dart` never
        // exits after the AppHost stops. `run` therefore closes the transport, and it also closes it
        // when the call fails.
        Assert.Contains("try {", run, StringComparison.Ordinal);
        Assert.Contains("await transport.invokeCapability('Aspire.Hosting/run', args);", run, StringComparison.Ordinal);
        Assert.Contains("} finally {", run, StringComparison.Ordinal);
        Assert.Contains("await transport.close();", run, StringComparison.Ordinal);

        // No other capability closes the connection, because the script keeps calling the host.
        var addContainer = ExtractMethod(generated, "addContainer");
        Assert.DoesNotContain("transport.close()", addContainer, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_ConcreteClassImplementsInterfaceHandles()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        // Dart has no structural subtyping, so a concrete class only satisfies an interface
        // parameter when it names the interface class. TestRedisResource reaches
        // IResourceWithEnvironment through ContainerResource, so the whole chain appears.
        Assert.Equal(
            "class TestRedisResource extends AspireObject implements "
                + "ComputeResource, ExpressionValue, Resource, ResourceWithArgs, "
                + "ResourceWithConnectionString, ResourceWithEndpoints, ResourceWithEnvironment, "
                + "ResourceWithWaitSupport {",
            ClassDeclaration(generated, "TestRedisResource"));

        // The real hosting assembly gives the same result for a container.
        Assert.Equal(
            "class ContainerResource extends AspireObject implements "
                + "ComputeResource, Resource, ResourceWithArgs, ResourceWithEndpoints, "
                + "ResourceWithEnvironment, ResourceWithWaitSupport {",
            ClassDeclaration(generated, "ContainerResource"));

        // Every method the interface declares also exists on the class, so `implements` compiles.
        var container = ExtractClass(generated, "ContainerResource");
        Assert.Contains("Future<ContainerResource> waitFor(Resource dependency", container, StringComparison.Ordinal);
        Assert.Contains("Future<ContainerResource> withEnvironment(", container, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_InterfaceTypesAreAbstractClasses()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        // An interface handle type is abstract, so nothing instantiates a value that carries no
        // concrete .NET type.
        Assert.Equal(
            "abstract class Resource extends AspireObject {",
            ClassDeclaration(generated, "Resource"));
        Assert.Equal(
            "abstract class ResourceWithConnectionString extends AspireObject implements "
                + "ExpressionValue, Resource {",
            ClassDeclaration(generated, "ResourceWithConnectionString"));

        // A decoder still has to build a value for a capability that returns the interface. The
        // generator emits one private subclass for that, and the subclass adds no member.
        Assert.Contains(
            "class _ResourceWithConnectionStringImpl extends ResourceWithConnectionString {",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "const _ResourceWithConnectionStringImpl(super.handle, super.transport);",
            generated,
            StringComparison.Ordinal);

        // AddConnectionString declares IResourceWithConnectionString, so its decoder names the
        // implementation class while the signature keeps the interface.
        var addConnectionString = ExtractMethod(generated, "addConnectionString");
        Assert.StartsWith("> addConnectionString(", addConnectionString, StringComparison.Ordinal);
        Assert.Contains("return _ResourceWithConnectionStringImpl(", addConnectionString, StringComparison.Ordinal);
        Assert.Contains(
            "Future<ResourceWithConnectionString> addConnectionString(",
            generated,
            StringComparison.Ordinal);
    }

    [Fact]
    [RequiresTools(["dart"])]
    public async Task GeneratedCode_InterfaceParameterAcceptsConcreteClass()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var modules = Path.Combine(workspace.Path, ".aspire", "modules");
        Directory.CreateDirectory(modules);

        // The real Aspire.Hosting assembly holds waitFor(IResource) and
        // withReference(IResourceWithConnectionString | ...), so it proves the interface parameters
        // of the shipped surface.
        var files = _generator.GenerateDistributedApplication(CreateContextFromHostingAssembly());
        foreach (var (name, content) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(modules, name), content);
        }

        var scaffold = new DartLanguageSupport().Scaffold(new ScaffoldRequest
        {
            TargetPath = workspace.Path,
            ProjectName = "InterfaceApp",
            PortSeed = 3
        });

        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "pubspec.yaml"), scaffold["pubspec.yaml"]);

        // A concrete resource passes where the capability declares an interface, and a value that
        // the host returned as an interface passes as well.
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "apphost.dart"), """
            import '.aspire/modules/aspire.dart';

            Future<void> main(List<String> args) async {
              final DistributedApplicationBuilder builder = await createBuilder(args);

              final ResourceWithConnectionString db = await builder.addConnectionString('db');
              final ContainerResource cache = await builder.addContainer('cache', 'redis:7.4');
              final ContainerResource api = await builder.addContainer('api', 'img');

              await api.withReference(db);
              await api.waitFor(cache);
              await api.waitFor(db);

              final DistributedApplication app = await builder.build();
              await app.run();
            }

            """);

        var restore = await RunAsync("dart", workspace.Path, ["pub", "get"]);
        outputHelper.WriteLine(restore.Output);
        Assert.Equal(0, restore.ExitCode);

        var analyze = await RunAsync(
            "dart",
            workspace.Path,
            ["analyze", "--fatal-infos", Path.Combine(".aspire", "modules"), "apphost.dart"]);
        outputHelper.WriteLine(analyze.Output);
        Assert.Equal(0, analyze.ExitCode);
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

    // ── D2.4: data objects, enums, unions, collections, cancellation ─────────

    [Fact]
    public void Generate_AspireDtoType_GeneratesClass()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var dto = ExtractClass(generated, "TestConfigDto");

        // A data object knows its own wire form, so AspireMarshal.encode can send it without a
        // conversion at the call site.
        Assert.Contains("class TestConfigDto implements AspireWireValue {", generated, StringComparison.Ordinal);
        Assert.Contains("const TestConfigDto({", dto, StringComparison.Ordinal);
        Assert.Contains("final String? name;", dto, StringComparison.Ordinal);
        Assert.Contains("final num? port;", dto, StringComparison.Ordinal);
        Assert.Contains("final bool? enabled;", dto, StringComparison.Ordinal);

        // The host marshals a data object with the camelCase naming policy, so the wire form uses
        // the camelCase name and leaves out the null properties.
        Assert.Contains("name: AspireRuntime.asString(json['name']),", dto, StringComparison.Ordinal);
        Assert.Contains("json['name'] = name;", dto, StringComparison.Ordinal);
        Assert.Contains("Object? toWire() => toJson();", dto, StringComparison.Ordinal);

        // The property documentation reaches the class dartdoc above the declaration.
        Assert.Contains("/// ## Properties", generated, StringComparison.Ordinal);
        Assert.Contains("/// * [name] — The name of the test config.", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_NestedDtoType_GeneratesCorrectTypes()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var dto = ExtractClass(generated, "TestNestedDto");

        // A nested data object decodes into its own class.
        Assert.Contains("final TestConfigDto? config;", dto, StringComparison.Ordinal);
        Assert.Contains("config: TestConfigDto.fromWire(json['config']),", dto, StringComparison.Ordinal);

        // A data object carries its collections by value, so no collection handle appears here.
        Assert.Contains("final List<String?>? tags;", dto, StringComparison.Ordinal);
        Assert.Contains("final Map<String, num?>? counts;", dto, StringComparison.Ordinal);
        Assert.DoesNotContain("AspireList<", dto, StringComparison.Ordinal);
        Assert.DoesNotContain("AspireDict<", dto, StringComparison.Ordinal);

        var deeplyNested = ExtractClass(generated, "TestDeeplyNestedDto");
        Assert.Contains("final Map<String, List<TestConfigDto?>?>? nestedData;", deeplyNested, StringComparison.Ordinal);
        Assert.Contains("final List<Map<String, String?>?>? metadataArray;", deeplyNested, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_EnumType_GeneratesEnum()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var enumType = ExtractEnum(generated, "TestResourceStatus");

        Assert.Contains("enum TestResourceStatus implements AspireWireValue {", generated, StringComparison.Ordinal);
        Assert.Contains("pending('Pending'),", enumType, StringComparison.Ordinal);
        Assert.Contains("failed('Failed');", enumType, StringComparison.Ordinal);

        // The wire form is the .NET member name in both directions.
        Assert.Contains("final String wireName;", enumType, StringComparison.Ordinal);
        Assert.Contains("String toWire() => wireName;", enumType, StringComparison.Ordinal);
        Assert.Contains("static TestResourceStatus? fromWire(Object? wire) {", enumType, StringComparison.Ordinal);

        // The class dartdoc above the declaration lists every value with the documentation of its
        // member.
        Assert.Contains("/// ## Values", generated, StringComparison.Ordinal);
        Assert.Contains("/// * [pending] — The resource is pending.", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_EnumType_ToWireRejectsUnknown()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var enumType = ExtractEnum(generated, "TestResourceStatus");

        // toWireOf accepts a value of the enum or a string that names one, and it throws an
        // ArgumentError that lists every value the enum accepts.
        Assert.Contains("static String toWireOf(Object? value) {", enumType, StringComparison.Ordinal);
        Assert.Contains("throw ArgumentError.value(", enumType, StringComparison.Ordinal);
        Assert.Contains(
            "'TestResourceStatus does not accept it. It accepts: pending, running, stopped, failed',",
            enumType,
            StringComparison.Ordinal);

        // An enum argument goes through the validating form, never through the wire name directly.
        Assert.Contains("args['status'] = TestResourceStatus.toWireOf(status);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("args['status'] = status.toWire();", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void AspireUnion_InterfaceHandleInput_GeneratesUnionGuard()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var module = ExtractClass(generated, "TestRedisResource");

        // withUnionDependency accepts a string or any builder of IResourceWithConnectionString. A
        // Dart class never extends another generated class, so the guard names the interface and
        // every concrete wrapper that implements it.
        Assert.Contains(
            "args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => "
                + "value is ResourceWithConnectionString || value is String || value is TestRedisResource, "
                + "const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], "
                + "'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');",
            module,
            StringComparison.Ordinal);

        // The guard raises an ArgumentError that names every accepted class.
        var runtime = ReadResource("aspire_runtime.dart");
        Assert.Contains("static Object? requireUnion(", runtime, StringComparison.Ordinal);
        Assert.Contains("throw ArgumentError.value(", runtime, StringComparison.Ordinal);
        Assert.Contains("It accepts: ${accepted.join(', ')}", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_MethodWithCancellationToken_GeneratesNamedParameter()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var module = ExtractClass(generated, "TestRedisResource");

        // A CancellationToken is always a named parameter, even when the .NET parameter has no
        // default, so the caller can stop the call with CancellationToken.cancel.
        Assert.Contains(
            "Future<String?> getStatusAsync({CancellationToken? cancellationToken}) async {",
            module,
            StringComparison.Ordinal);
        Assert.Contains("args['cancellationToken'] = cancellationToken;", module, StringComparison.Ordinal);

        // A token beside a required parameter keeps the required parameter positional.
        Assert.Contains(
            "Future<bool?> waitForReadyAsync(num timeout, {CancellationToken? cancellationToken}) async {",
            module,
            StringComparison.Ordinal);

        // CancellationToken.create and cancel come from base.dart without a change.
        var baseDart = ReadResource("base.dart");
        Assert.Contains("factory CancellationToken.create()", baseDart, StringComparison.Ordinal);
        Assert.Contains("Future<bool> cancel([AspireTransport? transport])", baseDart, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_FlattensSingleOptionalDtoOptionsParameter()
    {
        // withHttpCommand has one optional "options" data object, so the properties of the object
        // become named parameters and the caller writes them directly.
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        Assert.Contains(
            "withHttpCommand(String path, String displayName, {CommandOptions? commandOptions,",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("options['commandName'] = commandName;", generated, StringComparison.Ordinal);
        Assert.Contains("args['options'] = options;", generated, StringComparison.Ordinal);

        // The map is only sent when the caller passed at least one property.
        Assert.Contains("if (options.isNotEmpty) {", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_DoesNotFlattenWhenOptionsCoexistsWithCancellationToken()
    {
        // promptInput has an "options" data object and a cancellation token. Dart renders a token
        // as its own named parameter, so the token is never flattened into the options map: it
        // stays a separate argument beside it.
        var generated = GenerateSource(CreateContextFromBothAssemblies());
        var module = ExtractClass(generated, "InteractionService");
        var promptInput = ExtractMethod(module, "promptInput");

        Assert.Contains("{CancellationToken? cancellationToken,", promptInput, StringComparison.Ordinal);
        Assert.Contains("args['cancellationToken'] = cancellationToken;", promptInput, StringComparison.Ordinal);
        Assert.DoesNotContain("options['CancellationToken']", promptInput, StringComparison.Ordinal);
        Assert.DoesNotContain("options['cancellationToken']", promptInput, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_CollectionIntrinsics_GenerateTypedListAndDict()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        var runtime = files["aspire_runtime.dart"];

        // The wrappers are hand written in the runtime file and they carry the element type, so a
        // caller reads a typed value instead of Object?.
        Assert.Contains("class AspireList<T> extends AspireObject {", runtime, StringComparison.Ordinal);
        Assert.Contains("class AspireDict<T> extends AspireObject {", runtime, StringComparison.Ordinal);
        Assert.Contains("final T Function(Object?) decoder;", runtime, StringComparison.Ordinal);
        Assert.Contains("Future<List<T>> toList() async {", runtime, StringComparison.Ordinal);
        Assert.Contains("Future<Map<String, T>> toMap() async {", runtime, StringComparison.Ordinal);

        foreach (var capabilityId in new[]
        {
            "Aspire.Hosting/List.toArray", "Aspire.Hosting/List.length", "Aspire.Hosting/List.get",
            "Aspire.Hosting/List.add", "Aspire.Hosting/List.set", "Aspire.Hosting/List.insert",
            "Aspire.Hosting/List.indexOf", "Aspire.Hosting/List.removeAt", "Aspire.Hosting/List.clear",
            "Aspire.Hosting/Dict.toObject", "Aspire.Hosting/Dict.count", "Aspire.Hosting/Dict.get",
            "Aspire.Hosting/Dict.set", "Aspire.Hosting/Dict.has", "Aspire.Hosting/Dict.remove",
            "Aspire.Hosting/Dict.keys", "Aspire.Hosting/Dict.values", "Aspire.Hosting/Dict.clear"
        })
        {
            var name = capabilityId["Aspire.Hosting/".Length..].Split('.')[1];
            Assert.Contains($"'{capabilityId[..capabilityId.LastIndexOf('.')]}.$name'", runtime, StringComparison.Ordinal);
        }

        // Every capability the wrappers call has to exist in the hosting assembly.
        var capabilityIds = ScanCapabilitiesFromHostingAssembly().Select(c => c.CapabilityId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Aspire.Hosting/List.length", capabilityIds);
        Assert.Contains("Aspire.Hosting/Dict.count", capabilityIds);

        // The wrappers name the target argument the way the scanner registered it.
        Assert.Contains("'list': handle,", runtime, StringComparison.Ordinal);
        Assert.Contains("'dict': handle,", runtime, StringComparison.Ordinal);

        // A generated getter supplies the element decoder, so the wrapper is typed.
        var generated = GenerateSource(CreateContextFromTestAssembly());
        Assert.Contains("Future<AspireList<String?>> getTags() async {", generated, StringComparison.Ordinal);
        Assert.Contains("Future<AspireDict<String?>> getMetadata() async {", generated, StringComparison.Ordinal);
    }

    // ── D2.4: context types, callbacks, reference expressions ────────────────

    [Fact]
    public void GenerateDistributedApplication_WithContextType_GeneratesPropertyCapabilities()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var nameGetter = capabilities.FirstOrDefault(c =>
            c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name");
        Assert.NotNull(nameGetter);
        Assert.Equal(AtsCapabilityKind.PropertyGetter, nameGetter.CapabilityKind);
        Assert.Equal("context", Assert.Single(nameGetter.Parameters).Name);

        var nameSetter = capabilities.FirstOrDefault(c =>
            c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName");
        Assert.NotNull(nameSetter);
        Assert.Equal(AtsCapabilityKind.PropertySetter, nameSetter.CapabilityKind);
        Assert.Equal(2, nameSetter.Parameters.Count);

        // A getter becomes a typed accessor and a setter returns the context, so a caller can chain
        // the write back.
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var module = ExtractClass(generated, "TestCallbackContext");

        Assert.Contains("Future<String?> name() async {", module, StringComparison.Ordinal);
        Assert.Contains("Future<TestCallbackContext> setName(String value) async {", module, StringComparison.Ordinal);
        Assert.Contains("Future<num?> value() async {", module, StringComparison.Ordinal);

        // A collection property arrives as a typed AspireList or AspireDict.
        var collections = ExtractClass(generated, "TestMutableCollectionContext");
        Assert.Contains("Future<AspireList<String?>> tags() async {", collections, StringComparison.Ordinal);
        Assert.Contains("Future<AspireDict<num?>> counts() async {", collections, StringComparison.Ordinal);
        Assert.Contains(
            "Future<TestMutableCollectionContext> setTags(AspireList<String?> value) async {",
            collections,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_TypedCallbackDecodesContextHandle()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());
        var module = ExtractClass(generated, "TestRedisResource");

        // Every callback shape gets one typedef, so the caller sees a typed function instead of a
        // bare Function.
        Assert.Contains(
            "typedef TestEnvironmentCallback = FutureOr<void> Function(TestEnvironmentContext arg);",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "Future<TestRedisResource> testWithEnvironmentCallback(TestEnvironmentCallback callback) async {",
            module,
            StringComparison.Ordinal);

        // The wrapper decodes the raw handle into the context class before the caller function runs.
        Assert.Contains(
            "final TestEnvironmentContext p0 = TestEnvironmentContext(AspireRuntime.requireHandle(a0, "
                + "'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);",
            module,
            StringComparison.Ordinal);

        // A callback that returns a value sends the value back instead of a write-back map.
        Assert.Contains("return await validator(p0);", module, StringComparison.Ordinal);

        // An optional callback closes over a local, because a closure never promotes a captured
        // variable to a non-null type.
        Assert.Contains("final TestCallback callbackCallback = callback;", module, StringComparison.Ordinal);

        // A multi-argument callback decodes every argument.
        Assert.Contains(
            "args['callback'] = (Object? a0, Object? a1) async {",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_DtoCallbacksReturnMutatedArguments()
    {
        // A generated data object never changes in place, so a callback that receives one returns
        // the changed object and the wrapper puts it in the positional write-back map.
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        Assert.Contains(
            "typedef ResourceUrlAnnotationCallback = FutureOr<ResourceUrlAnnotation?> "
                + "Function(ResourceUrlAnnotation? obj);",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "final ResourceUrlAnnotation? p0 = ResourceUrlAnnotation.fromWire(a0);",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "final ResourceUrlAnnotation? changed = await callback(p0);",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("'p0': (changed ?? p0)?.toJson(),", generated, StringComparison.Ordinal);

        // A callback with no data object argument returns null, and the transport then echoes the
        // arguments the host sent.
        Assert.Contains("return null;", generated, StringComparison.Ordinal);
        var transport = ReadResource("transport.dart");
        Assert.Contains("'result': result == null ? args : AspireMarshal.encode(result),", transport, StringComparison.Ordinal);

        // A field the decoder cannot convert becomes null instead of failing the whole object.
        Assert.Contains("static ResourceUrlAnnotation? fromWire(Object? wire) => wire is Map", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_CallbackPropertiesAreWrapped()
    {
        var atsContext = CreateContextFromBothAssemblies();
        var generated = GenerateSource(atsContext);

        var options = Assert.Single(atsContext.DtoTypes, dto => dto.Name == "ProcessCommandExportOptions");
        var createProcessSpec = Assert.Single(options.Properties, property => property.Name == "CreateProcessSpec");
        Assert.True(createProcessSpec.IsCallback);

        var module = ExtractClass(generated, "ProcessCommandExportOptions");

        // A callback property is typed by the same typedef as a callback argument.
        Assert.Contains("final ExecuteCommandCallback2? createProcessSpec;", module, StringComparison.Ordinal);

        // fromJson no longer skips the property. The host sends a callback identifier that the guest
        // cannot invoke, so the property stays null, and a guest function survives a round trip.
        Assert.Contains(
            "createProcessSpec: AspireRuntime.asCallback<ExecuteCommandCallback2>(json['createProcessSpec']),",
            module,
            StringComparison.Ordinal);

        // toJson wraps the function, so the host sees the same typed arguments as a callback the
        // caller passes to a capability.
        Assert.Contains(
            "final ExecuteCommandCallback2 createProcessSpecCallback = createProcessSpec!;",
            module,
            StringComparison.Ordinal);
        Assert.Contains(
            "final ExecuteCommandContext p0 = ExecuteCommandContext(AspireRuntime.requireHandle(a0, "
                + "'ProcessCommandExportOptions.createProcessSpec'), AspireTransport.defaultInstance);",
            module,
            StringComparison.Ordinal);

        // AspireTransport.request walks the arguments and registers every function it finds, so the
        // wrapped closure travels as a callback identifier.
        var transport = ReadResource("transport.dart");
        Assert.Contains("return registerCallback(value);", transport, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_NullabilityFollowsAtsTypeRef()
    {
        // AtsTypeRef.IsNullable drives the return type and the decode, not a blanket rule. A handle
        // normally decodes through requireHandle, which throws instead of returning null.
        var context = CreateContextWithExtraCapability(new AtsCapabilityInfo
        {
            CapabilityId = "Aspire.Tests/getOptionalBuilder",
            MethodName = "getOptionalBuilder",
            Parameters =
            [
                new AtsParameterInfo { Name = "builder", Type = BuilderTypeRef() },
                new AtsParameterInfo { Name = "fallback", Type = BuilderTypeRef(nullable: true), IsNullable = true }
            ],
            ReturnType = BuilderTypeRef(nullable: true),
            TargetTypeId = AtsConstants.BuilderTypeId,
            TargetType = BuilderTypeRef(),
            TargetParameterName = "builder"
        });

        var generated = GenerateSource(context);

        Assert.Contains(
            "Future<DistributedApplicationBuilder?> getOptionalBuilder(DistributedApplicationBuilder? fallback) async {",
            generated,
            StringComparison.Ordinal);
        // The builder type is a .NET interface, so the decoder builds the private implementation
        // class of the abstract Dart class.
        Assert.Contains(
            "return (result == null ? null : _DistributedApplicationBuilderImpl("
                + "AspireRuntime.requireHandle(result, 'Aspire.Tests/getOptionalBuilder'), transport));",
            generated,
            StringComparison.Ordinal);

        // A handle that the ATS type does not mark nullable keeps the non-null type.
        Assert.Contains(
            "Future<TestRedisResource> addTestRedis(String name, {num? port}) async {",
            generated,
            StringComparison.Ordinal);
    }

    // ── D2.4: documentation ──────────────────────────────────────────────────

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_EmitsXmlDocumentationAsDartdoc()
    {
        var generated = GenerateSource(CreateContextFromTestAssembly());

        // The ats-* overrides win over the plain XML documentation.
        Assert.Contains("/// Adds a test Redis resource from ATS documentation.", generated, StringComparison.Ordinal);
        Assert.Contains("/// * [name] — The ATS resource name.", generated, StringComparison.Ordinal);
        Assert.Contains("/// The ATS test Redis resource builder.", generated, StringComparison.Ordinal);
        Assert.Contains("/// ## Parameters", generated, StringComparison.Ordinal);
        Assert.Contains("/// ## Returns", generated, StringComparison.Ordinal);

        // Remarks reach the dartdoc under the summary.
        Assert.Contains(
            "/// This method tests the factory method codegen pattern where a method on builder type A",
            generated,
            StringComparison.Ordinal);

        // An empty ats-param and an empty ats-remarks suppress the plain documentation.
        Assert.DoesNotContain("The optional Redis port.", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Uses XML documentation instead of the attribute description when both are present.",
            generated,
            StringComparison.Ordinal);

        // Type, property and enum member documentation all reach the dartdoc.
        Assert.Contains("/// * [name] — The name of the test config.", generated, StringComparison.Ordinal);
        Assert.Contains("/// * [pending] — The resource is pending.", generated, StringComparison.Ordinal);

        // An obsolete capability keeps the Dart deprecation annotation. The test assembly declares
        // none, so the check reads the hosting assembly.
        Assert.Contains("@Deprecated(", GenerateSource(CreateContextFromBothAssemblies()), StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDistributedApplication_WithAtsReference_RendersDocLink()
    {
        var generated = GenerateSource(CreateContextFromBothAssemblies());

        // The scanner writes <ats-see cref="!:type:IHost"/> as {@ats-ref type:IHost}. Dartdoc reads
        // `{@...}` as a directive, so the marker never survives into the generated code.
        Assert.Contains(
            "Represents a distributed application that implements the `IHost` and `IAsyncDisposable` interfaces.",
            generated,
            StringComparison.Ordinal);
        Assert.DoesNotContain("{@ats-ref", generated, StringComparison.Ordinal);

        // A marker without a label becomes a backtick link. A marker with a label keeps the label
        // and names the target after it, because dartdoc has no labelled link syntax.
        Assert.Equal(
            "See `Foo.bar` and the builder (`DistributedApplicationBuilder`).",
            AtsDartCodeGenerator.ConvertAtsReferences(
                "See {@ats-ref method:Foo.bar} and {@ats-ref type:DistributedApplicationBuilder|the builder}."));
    }

    [Fact]
    public void GeneratedCode_EveryPublicMethodHasReturnType()
    {
        // Dart infers `dynamic` for a declaration that names no type, which would drop every
        // generated type from the caller. Every generated member has to name its type.
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());

        var sources = files
            .Where(file => file.Key.StartsWith("aspire_generated", StringComparison.Ordinal)
                || string.Equals(file.Key, "aspire.dart", StringComparison.Ordinal))
            .OrderBy(file => file.Key, StringComparer.Ordinal)
            .Select(file => file.Value);

        var missing = new List<string>();
        var checkedMembers = 0;

        foreach (var source in sources)
        {
            foreach (var line in source.Split('\n'))
            {
                var trimmed = line.Trim();

                // A method declaration ends with ") async {" and holds no assignment. A wrapper
                // closure such as `args['callback'] = (Object? a0) async {` holds one.
                if (trimmed.EndsWith(") async {", StringComparison.Ordinal)
                    && trimmed.Contains('(', StringComparison.Ordinal)
                    && !trimmed.Contains('=', StringComparison.Ordinal))
                {
                    checkedMembers++;
                    if (!trimmed.StartsWith("Future<", StringComparison.Ordinal))
                    {
                        missing.Add(trimmed);
                    }
                    continue;
                }

                if (trimmed.StartsWith("static ", StringComparison.Ordinal)
                    && trimmed.Contains(" get ", StringComparison.Ordinal))
                {
                    checkedMembers++;
                    // "static <type> get <name> => ..." has four words before the arrow.
                    if (trimmed.Split(' ').Length < 4)
                    {
                        missing.Add(trimmed);
                    }
                }
            }
        }

        Assert.Empty(missing);
        Assert.True(checkedMembers > 100, $"Expected many generated members, found {checkedMembers}.");
    }

    // ── D2.6: watch ──────────────────────────────────────────────────────────

    [Fact]
    public void GeneratedCode_HasWatchScript()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromTestAssembly());

        var watch = files["watch.dart"];
        Assert.Contains("Future<void> main(List<String> arguments) async {", watch, StringComparison.Ordinal);
        Assert.Contains("[aspire-watch] restarting:", watch, StringComparison.Ordinal);
        Assert.Contains("ProcessStartMode.inheritStdio", watch, StringComparison.Ordinal);
        Assert.Contains("ProcessSignal.sigterm", watch, StringComparison.Ordinal);
        Assert.Contains("ProcessSignal.sigkill", watch, StringComparison.Ordinal);

        // The watcher is copied without a change, so the Dart tests cover exactly what ships.
        Assert.Equal(ReadResource("watch.dart"), watch);

        // The entry point must not import the watcher: watch.dart starts a child process and would
        // run on every AppHost launch.
        Assert.DoesNotContain("watch.dart", files["aspire.dart"], StringComparison.Ordinal);
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

    /// <summary>
    /// Returns the declaration line of one generated class, with the <c>abstract</c> keyword and
    /// the <c>implements</c> clause.
    /// </summary>
    private static string ClassDeclaration(string source, string className)
    {
        var marker = $"class {className} extends ";
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Generated code does not define {className}.");

        var start = source.LastIndexOf('\n', index) + 1;
        var end = source.IndexOf('\n', index);
        return end < 0 ? source[start..] : source[start..end];
    }

    private static string ExtractClass(string source, string className)
    {
        var start = source.IndexOf($"class {className} ", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Generated code does not define {className}.");

        // Walk back over the documentation comment so the class body starts at its first line.
        var end = source.IndexOf("\n}\n", start, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..(end + 3)];
    }

    /// <summary>
    /// Returns the body of one generated method, from its signature to its closing brace.
    /// </summary>
    private static string ExtractMethod(string source, string methodName)
    {
        var start = source.IndexOf($"> {methodName}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Generated code does not define {methodName}.");

        var end = source.IndexOf("\n  }\n", start, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..(end + 5)];
    }

    private static string ExtractEnum(string source, string enumName)
    {
        var start = source.IndexOf($"enum {enumName} ", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Generated code does not define {enumName}.");

        var end = source.IndexOf("\n}\n", start, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..(end + 3)];
    }

    private static AtsContext CreateContextWithExtraCapability(AtsCapabilityInfo capability)
    {
        var context = CreateContextFromTestAssembly();

        return new AtsContext
        {
            Capabilities = [.. context.Capabilities, capability],
            HandleTypes = context.HandleTypes,
            DtoTypes = context.DtoTypes,
            EnumTypes = context.EnumTypes,
            ExportedValues = context.ExportedValues,
            Diagnostics = context.Diagnostics
        };
    }

    private static AtsTypeRef BuilderTypeRef(bool nullable = false) => new()
    {
        TypeId = AtsConstants.BuilderTypeId,
        Category = AtsTypeCategory.Handle,
        IsInterface = true,
        IsNullable = nullable ? true : null
    };

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

    private static AtsContext CreateContextFromHostingAssembly()
    {
        var hostingAssembly = typeof(DistributedApplication).Assembly;
        return AtsCapabilityScanner.ScanAssembly(hostingAssembly).ToAtsContext();
    }

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
