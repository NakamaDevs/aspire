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

namespace Aspire.Hosting.CodeGeneration.Elixir.Tests;

public class AtsElixirCodeGeneratorTests(ITestOutputHelper outputHelper)
{
    private readonly AtsElixirCodeGenerator _generator = new();

    // The test types are compiled into this assembly via Compile Include
    private const string TestTypesAssemblyName = "Aspire.Hosting.CodeGeneration.Elixir.Tests";

    [Fact]
    public void Language_ReturnsElixir()
    {
        Assert.Equal("Elixir", _generator.Language);
    }

    [Fact]
    public void GeneratedCode_HasAspireExFile()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());

        Assert.Contains("aspire.ex", files.Keys);
        Assert.Contains("aspire_generated.ex", files.Keys);
        Assert.Contains("Code.require_file(aspire_module_file, __DIR__)", files["aspire.ex"], StringComparison.Ordinal);
        Assert.Contains("\"base.ex\",", files["aspire.ex"], StringComparison.Ordinal);
        Assert.Contains("\"transport.ex\",", files["aspire.ex"], StringComparison.Ordinal);
        Assert.Contains("\"aspire_runtime.ex\",", files["aspire.ex"], StringComparison.Ordinal);
        Assert.Contains("\"aspire_generated.ex\"", files["aspire.ex"], StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_HasCreateBuilderFunction()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        var aspireEx = files["aspire.ex"];

        Assert.Contains("def create_builder(opts \\\\ []) do", aspireEx, StringComparison.Ordinal);
        Assert.Contains("def create_builder!(opts \\\\ [])", aspireEx, StringComparison.Ordinal);
        Assert.Contains("Aspire.Hosting/createBuilder", aspireEx, StringComparison.Ordinal);
        Assert.Contains("@builder_module Aspire.DistributedApplicationBuilder", aspireEx, StringComparison.Ordinal);

        // Aspire.build!/1 and Aspire.run!/1 dispatch on the struct module, so both functions have to
        // exist in the modules that createBuilder and build return.
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());
        var builderModule = ExtractModule(generated, "Aspire.DistributedApplicationBuilder");
        Assert.Contains("def build(%__MODULE__{} = target", builderModule, StringComparison.Ordinal);
        Assert.Contains("def build!(%__MODULE__{} = target", builderModule, StringComparison.Ordinal);

        var applicationModule = ExtractModule(generated, "Aspire.DistributedApplication");
        Assert.Contains("def run(%__MODULE__{} = target", applicationModule, StringComparison.Ordinal);
        Assert.Contains("def run!(%__MODULE__{} = target", applicationModule, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_EmitsTransportAndBaseVerbatim()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromTestAssembly());

        // The runtime files are copied without a change so the Elixir tests under
        // Aspire.Hosting.CodeGeneration.Elixir.ExTests cover exactly what ships.
        Assert.Equal(ReadResource("base.ex"), files["base.ex"]);
        Assert.Equal(ReadResource("transport.ex"), files["transport.ex"]);
        Assert.Equal(ReadResource("aspire_runtime.ex"), files["aspire_runtime.ex"]);
    }

    [Fact]
    public void GeneratedCode_UsesSnakeCaseFunctionNames()
    {
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        Assert.Contains("def add_container(", generated, StringComparison.Ordinal);
        Assert.Contains("def with_environment(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("def addContainer(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("def withEnvironment(", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_SanitizesElixirKeywordParameters()
    {
        // A reserved Elixir word cannot be a variable or a function name. The generator appends a
        // trailing underscore, so no generated signature holds a bare keyword.
        HashSet<string> reserved = new(StringComparer.Ordinal)
        {
            "do", "end", "fn", "when", "in", "and", "or", "not", "nil", "true", "false",
            "after", "else", "catch", "rescue", "receive", "try", "quote", "unquote", "defmodule",
            "case", "cond", "for", "if", "import", "alias", "require", "with", "def", "defp"
        };

        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());
        var checkedSignatures = 0;

        foreach (var line in generated.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("def ", StringComparison.Ordinal))
            {
                continue;
            }

            var open = trimmed.IndexOf('(', StringComparison.Ordinal);
            var close = trimmed.IndexOf(')', StringComparison.Ordinal);
            if (open < 0 || close < open)
            {
                continue;
            }

            var functionName = trimmed[4..open].TrimEnd('!');
            Assert.False(reserved.Contains(functionName), $"Generated function name '{functionName}' is a reserved Elixir word.");

            foreach (var parameter in trimmed[(open + 1)..close].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                // Strip a default value so `opts \\ []` reduces to `opts`.
                var name = parameter.Split(" \\\\ ", StringSplitOptions.TrimEntries)[0];
                Assert.False(reserved.Contains(name), $"Generated parameter '{name}' is a reserved Elixir word in: {trimmed}");
            }

            checkedSignatures++;
        }

        Assert.True(checkedSignatures > 100, $"Expected many generated signatures, found {checkedSignatures}.");
    }

    [Fact]
    public void GeneratedCode_EmitsBangVariants()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());

        Assert.Contains("def add_test_redis(", generated, StringComparison.Ordinal);
        Assert.Contains("def add_test_redis!(", generated, StringComparison.Ordinal);
        Assert.Contains("Aspire.Runtime.ok!(add_test_redis(", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_OptionalParametersAreKeywordList()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());

        // addTestRedis(builder, name, port = null) -> add_test_redis(target, name, opts \\ [])
        Assert.Contains("def add_test_redis(%__MODULE__{} = target, name, opts \\\\ []) do", generated, StringComparison.Ordinal);
        Assert.Contains("Aspire.Runtime.put_opt(\"port\", opts, :port)", generated, StringComparison.Ordinal);
        Assert.Contains("Aspire.Runtime.validate_opts!(opts, [:port]", generated, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput()
    {
        var atsContext = CreateContextFromTestAssembly();

        var files = _generator.GenerateDistributedApplication(atsContext);

        Assert.Contains("aspire.ex", files.Keys);
        Assert.Contains("aspire_generated.ex", files.Keys);
        Assert.Contains("transport.ex", files.Keys);
        Assert.Contains("base.ex", files.Keys);
        Assert.Contains("aspire_runtime.ex", files.Keys);

        await Verify(GenerateModuleSource(atsContext), extension: "ex")
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
        var atsContext = CreateContextFromBothAssemblies();

        var generated = GenerateModuleSource(atsContext);

        var testRedisModule = ExtractModule(generated, $"Aspire.CodeGeneration.Elixir.Tests.TestRedisResource");

        Assert.Contains("def with_environment(", testRedisModule, StringComparison.Ordinal);
        Assert.Contains("def with_environment!(", testRedisModule, StringComparison.Ordinal);
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
    [RequiresTools(["elixir", "elixirc"])]
    public async Task GeneratedCode_CompilesWithElixirc()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        foreach (var (name, content) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(workspace.Path, name), content);
        }

        // Compile every module file with --warnings-as-errors. The loader entry point is excluded
        // because its top-level Code.require_file/2 would define the same modules a second time.
        var moduleFiles = files.Keys
            .Where(name => !string.Equals(name, "aspire.ex", StringComparison.Ordinal))
            .OrderBy(LoadOrder)
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToList();

        var compileArguments = new List<string> { "--warnings-as-errors", "-o", "ebin" };
        compileArguments.AddRange(moduleFiles);

        var compile = await RunAsync("elixirc", workspace.Path, compileArguments);
        outputHelper.WriteLine(compile.Output);
        Assert.Equal(0, compile.ExitCode);

        // Load the SDK the way apphost.exs does, so the generated loader order is covered too.
        var load = await RunAsync("elixir", workspace.Path, ["-e", "Code.require_file(\"aspire.ex\", File.cwd!())"]);
        outputHelper.WriteLine(load.Output);
        Assert.Equal(0, load.ExitCode);
        Assert.DoesNotContain("warning:", load.Output, StringComparison.OrdinalIgnoreCase);
    }

    private static int LoadOrder(string fileName) => fileName switch
    {
        "base.ex" => 0,
        "transport.ex" => 1,
        "aspire_runtime.ex" => 2,
        _ => 3
    };

    private static async Task<(int ExitCode, string Output)> RunAsync(
        string fileName,
        string workingDirectory,
        IReadOnlyList<string> arguments)
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
    /// Joins every generated module file so a test can assert over the whole generated surface.
    /// </summary>
    private string GenerateModuleSource(AtsContext context)
    {
        var files = _generator.GenerateDistributedApplication(context);

        return string.Join(
            "\n",
            files
                .Where(file => file.Key.StartsWith("aspire_generated", StringComparison.Ordinal))
                .OrderBy(file => file.Key, StringComparer.Ordinal)
                .Select(file => file.Value));
    }

    private static string ExtractModule(string source, string moduleName)
    {
        var start = source.IndexOf($"defmodule {moduleName} do", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Generated code does not define {moduleName}.");

        var end = source.IndexOf("\ndefmodule ", start + 1, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..end];
    }

    private static string ReadResource(string name)
    {
        var assembly = typeof(AtsElixirCodeGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Aspire.Hosting.CodeGeneration.Elixir.Resources.{name}")!;
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
