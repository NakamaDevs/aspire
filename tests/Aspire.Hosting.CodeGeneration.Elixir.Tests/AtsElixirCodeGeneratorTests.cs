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
        Assert.Equal(ReadResource("watch.exs"), files["watch.exs"]);
    }

    [Fact]
    public void GeneratedCode_HasWatchScript()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromTestAssembly());

        var watch = files["watch.exs"];
        Assert.Contains("defmodule Aspire.Watch do", watch, StringComparison.Ordinal);
        Assert.Contains("Aspire.Watch.main(System.argv())", watch, StringComparison.Ordinal);
        Assert.Contains("[aspire-watch] restarting:", watch, StringComparison.Ordinal);

        // The loader entry point must not require the watcher: watch.exs starts a child process
        // and would run on every AppHost launch.
        Assert.DoesNotContain("watch.exs", files["aspire.ex"], StringComparison.Ordinal);
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
            .Where(name => !string.Equals(name, "aspire.ex", StringComparison.Ordinal)
                && !string.Equals(name, "watch.exs", StringComparison.Ordinal))
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

    // ── M2.4: DTOs, enums, values, unions, cancellation ──────────────────────

    [Fact]
    public void Generate_AspireDtoType_GeneratesStruct()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var dto = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestConfigDto");

        Assert.Contains("defstruct [:name, :port, :enabled, :optional_field]", dto, StringComparison.Ordinal);
        Assert.Contains("@type t :: %__MODULE__{", dto, StringComparison.Ordinal);
        Assert.Contains("name: String.t() | nil,", dto, StringComparison.Ordinal);
        Assert.Contains("port: number() | nil,", dto, StringComparison.Ordinal);
        Assert.Contains("enabled: boolean() | nil,", dto, StringComparison.Ordinal);

        // The wire form keeps the .NET property names and drops the nil properties.
        Assert.Contains("@spec to_wire(t()) :: map()", dto, StringComparison.Ordinal);
        Assert.Contains("{\"Name\", value.name},", dto, StringComparison.Ordinal);
        Assert.Contains("@spec from_wire(term()) :: t() | term()", dto, StringComparison.Ordinal);
        Assert.Contains("name: Aspire.Runtime.wire_get(wire, \"Name\")", dto, StringComparison.Ordinal);

        // new/1 lets a flattened options keyword list build the struct.
        Assert.Contains("def new(opts) when is_list(opts), do: struct!(__MODULE__, opts)", dto, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_NestedDtoType_GeneratesCorrectTypes()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var dto = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestNestedDto");

        // A nested DTO decodes into its own struct.
        Assert.Contains(
            "config: Aspire.Runtime.wire_get(wire, \"Config\", {:dto, Aspire.CodeGeneration.Elixir.Tests.TestConfigDto})",
            dto,
            StringComparison.Ordinal);
        Assert.Contains("config: Aspire.CodeGeneration.Elixir.Tests.TestConfigDto.t() | nil,", dto, StringComparison.Ordinal);

        // A DTO carries its collections by value, so no collection handle appears here.
        Assert.Contains("tags: [String.t()] | nil,", dto, StringComparison.Ordinal);
        Assert.Contains("counts: map() | nil", dto, StringComparison.Ordinal);
        Assert.DoesNotContain("{:handle, Aspire.List}", dto, StringComparison.Ordinal);

        var deeplyNested = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestDeeplyNestedDto");
        Assert.Contains("nested_data: map() | nil,", deeplyNested, StringComparison.Ordinal);
        Assert.Contains("metadata_array: [map()] | nil", deeplyNested, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_EnumType_GeneratesAtoms()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var enumModule = ExtractModule(generated, "Aspire.Enums.TestResourceStatus");

        Assert.Contains("@values [:pending, :running, :stopped, :failed]", enumModule, StringComparison.Ordinal);
        Assert.Contains("@type t :: atom()", enumModule, StringComparison.Ordinal);
        Assert.Contains("@spec values() :: [t()]", enumModule, StringComparison.Ordinal);
        Assert.Contains("def values, do: @values", enumModule, StringComparison.Ordinal);

        // The wire form is the .NET member name in both directions.
        Assert.Contains("@to_wire %{pending: \"Pending\"", enumModule, StringComparison.Ordinal);
        Assert.Contains("@from_wire %{\"Pending\" => :pending", enumModule, StringComparison.Ordinal);
        Assert.Contains("def from_wire(name) when is_binary(name), do: Map.get(@from_wire, name, name)", enumModule, StringComparison.Ordinal);

        // The moduledoc lists every value with the documentation of its member.
        Assert.Contains("## Values", enumModule, StringComparison.Ordinal);
        Assert.Contains("* `:pending` — The resource is pending.", enumModule, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_EnumType_ToWireRejectsUnknownAtom()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var enumModule = ExtractModule(generated, "Aspire.Enums.TestResourceStatus");

        // to_wire!/1 raises and names every value the enum accepts.
        Assert.Contains("@spec to_wire!(t() | String.t()) :: String.t()", enumModule, StringComparison.Ordinal);
        Assert.Contains("raise ArgumentError,", enumModule, StringComparison.Ordinal);
        Assert.Contains(
            "\"Aspire.Enums.TestResourceStatus does not accept #{inspect(value)}. It accepts :pending, :running, :stopped, :failed.\"",
            enumModule,
            StringComparison.Ordinal);

        // to_wire/1 stays a non-raising lookup.
        Assert.Contains("@spec to_wire(t() | String.t()) :: {:ok, String.t()} | :error", enumModule, StringComparison.Ordinal);
        Assert.Contains("def to_wire(value) when is_atom(value), do: Map.fetch(@to_wire, value)", enumModule, StringComparison.Ordinal);

        // An enum argument goes through the raising variant.
        Assert.Contains("Aspire.Runtime.encode_enum!(status, Aspire.Enums.TestResourceStatus)", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_IncludesExportedValues()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());

        // The catalog nests one module for each path segment above the value name.
        var catalog = ExtractModule(generated, "Aspire.Values.TestConfigs");
        Assert.Contains("@spec default() :: Aspire.CodeGeneration.Elixir.Tests.TestConfigDto.t()", catalog, StringComparison.Ordinal);
        Assert.Contains("def default do", catalog, StringComparison.Ordinal);
        Assert.Contains("The default test configuration.", catalog, StringComparison.Ordinal);

        // A DTO value decodes into its struct, so a catalog entry is the same shape as a value
        // the host returns.
        Assert.Contains(
            "{:dto, Aspire.CodeGeneration.Elixir.Tests.TestConfigDto}",
            catalog,
            StringComparison.Ordinal);

        Assert.Contains("@spec unicode_greeting() :: String.t()", catalog, StringComparison.Ordinal);
        Assert.Contains("\"你好こんにちは\"", catalog, StringComparison.Ordinal);

        var profiles = ExtractModule(generated, "Aspire.Values.TestConfigs.Profiles");
        Assert.Contains("def development do", profiles, StringComparison.Ordinal);
    }

    [Fact]
    public void AspireUnion_InterfaceHandleInput_GeneratesUnionGuard()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var module = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource");

        // withUnionDependency accepts a string or any builder of IResourceWithConnectionString.
        // The guard names the accepted modules, and the interface expands to the concrete wrappers.
        Assert.Contains("Aspire.Runtime.encode_union!(dependency, [:string, ", module, StringComparison.Ordinal);
        Assert.Contains("{:module, Aspire.ResourceWithConnectionString}", module, StringComparison.Ordinal);
        Assert.Contains("{:module, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}", module, StringComparison.Ordinal);
        Assert.Contains(
            "\"Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_union_dependency/2\", \"dependency\")",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_ReturnsBuilderCapabilitiesReturnReceiverType()
    {
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());
        var module = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource");

        // A fluent capability returns the receiver, so a pipe chain stays on the same module. The
        // declared .NET return type is often an interface or a base type, and decoding into that
        // type would end the chain on a struct that has no functions.
        Assert.Contains(
            "@spec with_persistence(t(), keyword()) :: {:ok, t()} | {:error, Aspire.Error.t()}",
            module,
            StringComparison.Ordinal);
        Assert.Contains("@spec with_persistence!(t(), keyword()) :: t()", module, StringComparison.Ordinal);

        // withEnvironment declares IResourceWithEnvironment, and withReference declares the same
        // interface. Both keep the concrete receiver.
        Assert.Contains(
            "@spec with_environment(t(), String.t(), term()) :: {:ok, t()} | {:error, Aspire.Error.t()}",
            module,
            StringComparison.Ordinal);
        Assert.DoesNotContain("{:handle, Aspire.ResourceWithEnvironment}, transport)", module, StringComparison.Ordinal);

        // The decoder wraps the handle into the module that holds the function.
        Assert.Contains(
            "|> Aspire.Runtime.invoke(\"Aspire.Hosting.CodeGeneration.Elixir.Tests/withPersistence\", args)\n" +
            "    |> Aspire.Runtime.result({:handle, __MODULE__}, transport)",
            module,
            StringComparison.Ordinal);

        // A factory method returns a different builder, so it keeps its declared return type.
        Assert.Contains(
            "@spec add_test_child_database(t(), String.t(), keyword()) :: "
                + "{:ok, Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource.t()} | {:error, Aspire.Error.t()}",
            module,
            StringComparison.Ordinal);
        Assert.Contains(
            "{:handle, Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource}, transport)",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void TwoPassScanning_DeduplicatesExpandedUnionTypes()
    {
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        foreach (var line in generated.Split('\n'))
        {
            var marker = line.IndexOf("encode_union!(", StringComparison.Ordinal);
            if (marker < 0)
            {
                continue;
            }

            var open = line.IndexOf('[', marker);
            var close = line.IndexOf(']', open);
            Assert.True(open > 0 && close > open, $"Cannot read the union list in: {line}");

            var specs = line[(open + 1)..close]
                .Split("}, ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            Assert.Equal(specs.Count, specs.Distinct(StringComparer.Ordinal).Count());
        }

        // withEnvironment names seven union members, and several expand to the same wrapper.
        Assert.Contains("encode_union!(value, [:string, ", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "{:module, Aspire.ResourceWithConnectionString}, {:module, Aspire.ResourceWithConnectionString}",
            generated,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_MethodWithCancellationToken_GeneratesCancellationTokenOption()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var module = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource");

        // A CancellationToken parameter is always an option, never a positional argument.
        Assert.Contains("def get_status_async(%__MODULE__{} = target, opts \\\\ []) do", module, StringComparison.Ordinal);
        Assert.Contains(
            "Aspire.Runtime.validate_opts!(opts, [:cancellation_token], \"Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.get_status_async/2\")",
            module,
            StringComparison.Ordinal);
        Assert.Contains("Aspire.Runtime.put_opt(\"cancellationToken\", opts, :cancellation_token)", module, StringComparison.Ordinal);

        // A token beside a required parameter keeps the required parameter positional.
        Assert.Contains("def wait_for_ready_async(%__MODULE__{} = target, timeout, opts \\\\ []) do", module, StringComparison.Ordinal);

        // Aspire.CancellationToken.new/0 and cancel/1 come from base.ex without a change.
        var baseEx = ReadResource("base.ex");
        Assert.Contains("def new do", baseEx, StringComparison.Ordinal);
        Assert.Contains("def cancel(%__MODULE__{id: id}, transport \\\\ nil) do", baseEx, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_FlattensSingleOptionalDtoOptionsParameter()
    {
        // withHttpCommand has one optional "options" DTO and no cancellation token, so the DTO
        // flattens into the keyword list and the caller writes its properties directly.
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        Assert.Contains(
            "def with_http_command(%__MODULE__{} = target, path, display_name, opts \\\\ []) do",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "|> Aspire.Runtime.put_opts_dto(\"options\", opts, Aspire.HttpCommandExportOptions)",
            generated,
            StringComparison.Ordinal);

        // The accepted options are the properties of the DTO, not a single :options key.
        Assert.Contains("validate_opts!(opts, [:command_options, ", generated, StringComparison.Ordinal);
        Assert.Contains(":command_name,", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_DoesNotFlattenWhenOptionsCoexistsWithCancellationToken()
    {
        // promptInput has an "options" DTO and a cancellation token. Both would share one keyword
        // list, so the DTO stays a single :options value.
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());
        var module = ExtractModule(generated, "Aspire.InteractionService");
        var promptInput = ExtractFunction(module, "prompt_input");

        Assert.Contains(
            "Aspire.Runtime.validate_opts!(opts, [:options, :cancellation_token], \"Aspire.InteractionService.prompt_input/5\")",
            promptInput,
            StringComparison.Ordinal);
        Assert.Contains("|> Aspire.Runtime.put_opt(\"options\", opts, :options)", promptInput, StringComparison.Ordinal);
        Assert.Contains("|> Aspire.Runtime.put_opt(\"cancellationToken\", opts, :cancellation_token)", promptInput, StringComparison.Ordinal);
        Assert.DoesNotContain("put_opts_dto(", promptInput, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ListProperty_GeneratesGetterOnlyFunctions()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var module = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestCollectionContext");

        // A get-only collection property has a getter and no setter.
        Assert.Contains("@spec items(t()) :: {:ok, Aspire.List.t()} | {:error, Aspire.Error.t()}", module, StringComparison.Ordinal);
        Assert.Contains("|> Aspire.Runtime.result({:handle, Aspire.List}, transport)", module, StringComparison.Ordinal);
        Assert.Contains("@spec metadata(t()) :: {:ok, Aspire.Dict.t()} | {:error, Aspire.Error.t()}", module, StringComparison.Ordinal);
        Assert.Contains("|> Aspire.Runtime.result({:handle, Aspire.Dict}, transport)", module, StringComparison.Ordinal);

        Assert.DoesNotContain("def set_items(", module, StringComparison.Ordinal);
        Assert.DoesNotContain("def set_metadata(", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_MutableCollectionProperties_UsePropertyAccessors()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var module = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestMutableCollectionContext");

        // A property that has a getter and a setter produces both accessor functions. The getter
        // returns the collection handle, so the caller mutates through Aspire.List or Aspire.Dict.
        Assert.Contains("@spec tags(t()) :: {:ok, Aspire.List.t()} | {:error, Aspire.Error.t()}", module, StringComparison.Ordinal);
        Assert.Contains("def set_tags(%__MODULE__{} = target, value) do", module, StringComparison.Ordinal);
        Assert.Contains("@spec counts(t()) :: {:ok, Aspire.Dict.t()} | {:error, Aspire.Error.t()}", module, StringComparison.Ordinal);
        Assert.Contains("def set_counts(%__MODULE__{} = target, value) do", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_CollectionIntrinsics_GenerateListAndDictModules()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        var runtime = files["aspire_runtime.ex"];

        // The wrappers are hand written in the runtime file, so a generated module never takes
        // the Aspire.List or Aspire.Dict name.
        Assert.Contains("defmodule Aspire.List do", runtime, StringComparison.Ordinal);
        Assert.Contains("defmodule Aspire.Dict do", runtime, StringComparison.Ordinal);

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
            Assert.Contains($"\"{capabilityId}\"", runtime, StringComparison.Ordinal);
        }

        // Every capability the wrappers call has to exist in the hosting assembly.
        var capabilityIds = ScanCapabilitiesFromHostingAssembly().Select(c => c.CapabilityId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Aspire.Hosting/List.length", capabilityIds);
        Assert.Contains("Aspire.Hosting/Dict.count", capabilityIds);

        // The wrappers name the target argument the way the scanner registered it.
        Assert.Contains("Map.put(arguments, \"list\", handle)", runtime, StringComparison.Ordinal);
        Assert.Contains("Map.put(arguments, \"dict\", handle)", runtime, StringComparison.Ordinal);
    }

    // ── M2.5: callbacks, context types, reference expressions ────────────────

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

        // The getter becomes get_<property>/1 style accessor and the setter set_<property>/2.
        var module = ExtractModule(
            GenerateModuleSource(CreateContextFromTestAssembly()),
            "Aspire.CodeGeneration.Elixir.Tests.TestCallbackContext");

        Assert.Contains("@spec name(t()) :: {:ok, String.t()} | {:error, Aspire.Error.t()}", module, StringComparison.Ordinal);
        Assert.Contains("def name(%__MODULE__{} = target) do", module, StringComparison.Ordinal);
        Assert.Contains("def set_name(%__MODULE__{} = target, value) do", module, StringComparison.Ordinal);

        // A setter returns the context, so a caller can chain the write back.
        Assert.Contains(
            "|> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestCallbackContext}, transport)",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_TypedCallbackDecodesContextHandle()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());
        var module = ExtractModule(generated, "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource");

        // The wrapper decodes p0 into the context wrapper struct before the caller function runs.
        Assert.Contains("callback_wrapper = fn callback ->", module, StringComparison.Ordinal);
        Assert.Contains(
            "a0 = Aspire.Runtime.decode(a0, {:handle, Aspire.CodeGeneration.Elixir.Tests.TestEnvironmentContext}, transport)",
            module,
            StringComparison.Ordinal);
        Assert.Contains("|> Map.put(\"callback\", callback_wrapper.(callback))", module, StringComparison.Ordinal);

        // A callback that returns a value sends the value back instead of a write-back map.
        Assert.Contains("Aspire.Runtime.encode(callback.(a0))", module, StringComparison.Ordinal);

        // An optional callback goes through put_opt_callback so a missing option sends nothing.
        Assert.Contains(
            "|> Aspire.Runtime.put_opt_callback(\"callback\", opts, :callback, callback_wrapper)",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_DtoCallbacksReturnMutatedArguments()
    {
        // An Elixir struct never changes in place, so a callback that receives a DTO returns the
        // changed struct and the wrapper puts it in the positional write-back map.
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        Assert.Contains("a0 = Aspire.Runtime.decode(a0, {:dto, Aspire.ResourceUrlAnnotation}, transport)", generated, StringComparison.Ordinal);
        Assert.Contains(
            "Aspire.Runtime.callback_writeback(callback.(a0), [{0, Aspire.ResourceUrlAnnotation, a0}])",
            generated,
            StringComparison.Ordinal);

        // A callback with no DTO argument passes an empty list, and the runtime then echoes the
        // arguments the host sent.
        Assert.Contains("Aspire.Runtime.callback_writeback(callback.(a0), [])", generated, StringComparison.Ordinal);

        var runtime = ReadResource("aspire_runtime.ex");
        Assert.Contains("def callback_writeback(_result, []), do: nil", runtime, StringComparison.Ordinal);
        Assert.Contains("{\"p#{index}\", encode(Map.get(replacements, index, decoded))}", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_CallbackArgsSkipUndecodableStructFields()
    {
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());
        var dto = ExtractModule(generated, "Aspire.ResourceUrlAnnotation");

        // from_wire/1 reads each property on its own, so an unknown or undecodable field never
        // fails the whole struct: the property keeps its wire value and the rest still decodes.
        Assert.Contains("def from_wire(%{} = wire) do", dto, StringComparison.Ordinal);
        Assert.Contains("Aspire.Runtime.wire_get(wire, ", dto, StringComparison.Ordinal);
        Assert.Contains("def from_wire(value), do: value", dto, StringComparison.Ordinal);

        var runtime = ReadResource("aspire_runtime.ex");
        Assert.Contains("def wire_get(map, name, decoder \\\\ nil) do", runtime, StringComparison.Ordinal);

        // A decoder that does not fit the wire shape returns the value without a change.
        Assert.Contains("def decode(value, {:dto, _module}, _transport), do: value", runtime, StringComparison.Ordinal);
        Assert.Contains("def wrap(_module, value, _transport), do: value", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDistributedApplication_WithDtoCallbackOptions_MarshalsNestedCallbackProperties()
    {
        var atsContext = CreateContextFromBothAssemblies();
        var generated = GenerateModuleSource(atsContext);

        var options = Assert.Single(atsContext.DtoTypes, dto => dto.Name == "ProcessCommandExportOptions");
        var createProcessSpec = Assert.Single(options.Properties, property => property.Name == "CreateProcessSpec");
        Assert.True(createProcessSpec.IsCallback);

        var module = ExtractModule(generated, "Aspire.ProcessCommandExportOptions");

        // A callback property of a DTO gets the same typed wrapper as a callback argument.
        Assert.Contains(
            "{\"CreateProcessSpec\", Aspire.Runtime.wrap_callback(value.create_process_spec, fn callback ->",
            module,
            StringComparison.Ordinal);
        Assert.Contains("a0 = Aspire.Runtime.decode(a0, {:handle, Aspire.ExecuteCommandContext}, nil)", module, StringComparison.Ordinal);

        // A nested DTO property goes through Aspire.Runtime.encode/1 in build_wire, which calls
        // to_wire/1 of the nested module, so its own callbacks are marshalled too.
        Assert.Contains("{\"CommandOptions\", value.command_options},", module, StringComparison.Ordinal);

        var runtime = ReadResource("aspire_runtime.ex");
        Assert.Contains("def wrap_callback(value, wrapper) when is_function(value), do: wrapper.(value)", runtime, StringComparison.Ordinal);
        Assert.Contains("wire_module?(module) -> module.to_wire(value)", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDistributedApplication_WithHostingTypes_KeepsReferenceExpressionInRuntime()
    {
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        // base.ex owns the reference expression, because a guest builds one locally and the host
        // also returns one as a handle. The generator must not emit a second module.
        Assert.DoesNotContain("defmodule Aspire.ReferenceExpression do", generated, StringComparison.Ordinal);
        Assert.Contains("defmodule Aspire.ReferenceExpression do", files["base.ex"], StringComparison.Ordinal);

        // The generated modules still name it, so a parameter and a return value stay typed.
        Assert.Contains("Aspire.ReferenceExpression.t()", generated, StringComparison.Ordinal);

        var baseEx = files["base.ex"];
        Assert.Contains("%{\"$expr\" => expression}", baseEx, StringComparison.Ordinal);
        Assert.Contains("Map.put(expression, \"valueProviders\", Enum.map(list, &provider/1))", baseEx, StringComparison.Ordinal);
        Assert.Contains("@get_value_capability \"Aspire.Hosting.ApplicationModel/getValueAsync\"", baseEx, StringComparison.Ordinal);

        // Aspire.ref/1 is the entry point a guest calls.
        Assert.Contains(
            "def ref(parts) when is_list(parts), do: Aspire.ReferenceExpression.from_parts(parts)",
            files["aspire.ex"],
            StringComparison.Ordinal);
    }

    [Fact]
    public void Scanner_CancellationTokenInCallback_MapsCorrectly()
    {
        var capabilities = ScanCapabilitiesFromTestAssembly();

        var withCancellableOperation = capabilities
            .FirstOrDefault(c => c.CapabilityId == $"{TestTypesAssemblyName}/withCancellableOperation");
        Assert.NotNull(withCancellableOperation);

        var operation = Assert.Single(withCancellableOperation.Parameters, p => p.Name == "operation");
        Assert.True(operation.IsCallback);
        Assert.NotNull(operation.CallbackParameters);
        Assert.Equal(AtsConstants.CancellationToken, Assert.Single(operation.CallbackParameters).Type?.TypeId);

        // The transport turns the token identifier into a struct before the callback runs, so the
        // generated wrapper adds no decoder for it.
        var module = ExtractModule(
            GenerateModuleSource(CreateContextFromTestAssembly()),
            "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource");
        Assert.Contains("operation_wrapper = fn callback ->", module, StringComparison.Ordinal);

        var transport = ReadResource("transport.ex");
        Assert.Contains("defp decode_callback_arg(value, token) when is_binary(token) and value == token do", transport, StringComparison.Ordinal);
        Assert.Contains("CancellationToken.from_id(token)", transport, StringComparison.Ordinal);
    }

    [Fact]
    public void Scanner_ReferenceExpressionGetValueAsync_IsExported()
    {
        var capabilities = ScanCapabilitiesFromHostingAssembly();

        var getValueAsync = capabilities.FirstOrDefault(c =>
            c.CapabilityId == "Aspire.Hosting.ApplicationModel/getValueAsync" &&
            c.TargetTypeId == AtsConstants.ReferenceExpressionTypeId);

        Assert.NotNull(getValueAsync);
        Assert.Equal(AtsCapabilityKind.InstanceMethod, getValueAsync.CapabilityKind);

        // base.ex calls exactly that capability id.
        Assert.Contains(
            $"@get_value_capability \"{getValueAsync.CapabilityId}\"",
            ReadResource("base.ex"),
            StringComparison.Ordinal);
    }

    // ── M2.6: documentation and typespecs ────────────────────────────────────

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_EmitsXmlDocumentationAsDoc()
    {
        var generated = GenerateModuleSource(CreateContextFromTestAssembly());

        // The ats-* overrides win over the plain XML documentation.
        Assert.Contains("Adds a test Redis resource from ATS documentation.", generated, StringComparison.Ordinal);
        Assert.Contains("* `name` — The ATS resource name.", generated, StringComparison.Ordinal);
        Assert.Contains("The ATS test Redis resource builder.", generated, StringComparison.Ordinal);
        Assert.Contains("## Parameters", generated, StringComparison.Ordinal);
        Assert.Contains("## Returns", generated, StringComparison.Ordinal);

        // An empty ats-param and an empty ats-remarks suppress the plain documentation.
        Assert.DoesNotContain("The optional Redis port.", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Uses XML documentation instead of the attribute description when both are present.",
            generated,
            StringComparison.Ordinal);

        // Type and property documentation reaches the moduledoc.
        Assert.Contains("* `:name` — The name of the test config.", generated, StringComparison.Ordinal);
        Assert.Contains("The default test configuration.", generated, StringComparison.Ordinal);
        Assert.Contains("* `:pending` — The resource is pending.", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDistributedApplication_WithAtsReference_RendersDocLink()
    {
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        // The scanner writes <ats-see cref="!:type:IHost"/> as {@ats-ref type:IHost}. The
        // generator turns it into a backticked name, which ExDoc renders as a link.
        Assert.Contains(
            "Represents a distributed application that implements the `IHost` and `IAsyncDisposable` interfaces.",
            generated,
            StringComparison.Ordinal);
        Assert.DoesNotContain("{@ats-ref", generated, StringComparison.Ordinal);

        // A marker that carries a label becomes an ExDoc link with that text.
        Assert.Equal(
            "See `Foo.bar/1` and [the builder](`Aspire.DistributedApplicationBuilder`).",
            AtsElixirCodeGenerator.ConvertAtsReferences(
                "See {@ats-ref method:Foo.bar/1} and {@ats-ref type:Aspire.DistributedApplicationBuilder|the builder}."));
    }

    [Fact]
    public void GenerateDistributedApplication_WithSuppressedSummary_DoesNotUseDescriptionFallback()
    {
        // A documentation object whose Summary is null means the author suppressed it with an
        // empty <ats-summary/>. The attribute description must not come back as a fallback.
        var context = CreateContextWithExtraCapability(new AtsCapabilityInfo
        {
            CapabilityId = "Aspire.Tests/withSuppressedSummary",
            MethodName = "withSuppressedSummary",
            Description = "Description fallback should not be emitted.",
            Documentation = new AtsDocumentationInfo(),
            Parameters = [new AtsParameterInfo { Name = "builder", Type = BuilderTypeRef() }],
            ReturnType = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive },
            TargetTypeId = AtsConstants.BuilderTypeId,
            TargetType = BuilderTypeRef(),
            TargetParameterName = "builder"
        });

        var generated = GenerateModuleSource(context);

        Assert.Contains("def with_suppressed_summary(%__MODULE__{} = target) do", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Description fallback should not be emitted.", generated, StringComparison.Ordinal);

        // The fallback still applies when the whole documentation object is missing.
        Assert.Contains("Invokes the `Aspire.Tests/withSuppressedSummary` capability.", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDistributedApplication_WithVoidReturn_DoesNotEmitReturnsDocumentation()
    {
        var context = CreateContextWithExtraCapability(new AtsCapabilityInfo
        {
            CapabilityId = "Aspire.Tests/runVoid",
            MethodName = "runVoid",
            Documentation = new AtsDocumentationInfo
            {
                Summary = "Runs a void capability.",
                Returns = "Void return documentation should not be emitted."
            },
            Parameters = [new AtsParameterInfo { Name = "builder", Type = BuilderTypeRef() }],
            ReturnType = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive },
            TargetTypeId = AtsConstants.BuilderTypeId,
            TargetType = BuilderTypeRef(),
            TargetParameterName = "builder"
        });

        var generated = GenerateModuleSource(context);

        Assert.Contains("Runs a void capability.", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Void return documentation should not be emitted.", generated, StringComparison.Ordinal);
        Assert.Contains("@spec run_void(t()) :: {:ok, nil} | {:error, Aspire.Error.t()}", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_EmitsSpecsForEveryFunction()
    {
        var generated = GenerateModuleSource(CreateContextFromBothAssemblies());

        Assert.Contains(
            "@spec add_test_redis(t(), String.t(), keyword()) :: {:ok, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.t()} | {:error, Aspire.Error.t()}",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "@spec add_test_redis!(t(), String.t(), keyword()) :: Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.t()",
            generated,
            StringComparison.Ordinal);

        // Handles, DTOs and enums all have a t/0 type, so a spec can name them.
        Assert.Contains("@type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}", generated, StringComparison.Ordinal);
        Assert.Contains("@type t :: atom()", generated, StringComparison.Ordinal);
        Assert.Contains("@type t :: %__MODULE__{", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_HasSpecForEveryPublicFunction()
    {
        // A static check replaces a Dialyzer run: dialyxir is a Hex dependency and the generated
        // SDK ships without one, so the suite cannot install it.
        var files = _generator.GenerateDistributedApplication(CreateContextFromBothAssemblies());

        var sources = files
            .Where(file => file.Key.StartsWith("aspire_generated", StringComparison.Ordinal)
                || string.Equals(file.Key, "aspire.ex", StringComparison.Ordinal))
            .OrderBy(file => file.Key, StringComparer.Ordinal)
            .Select(file => file.Value);

        var missing = new List<string>();
        var checkedFunctions = 0;

        foreach (var source in sources)
        {
            var moduleName = "<file>";
            var specs = new HashSet<string>(StringComparer.Ordinal);
            var defined = new List<(string Module, string Name)>();

            foreach (var line in source.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("defmodule ", StringComparison.Ordinal))
                {
                    Flush(moduleName, specs, defined, missing);
                    moduleName = trimmed["defmodule ".Length..].Replace(" do", "", StringComparison.Ordinal);
                    continue;
                }

                if (trimmed.StartsWith("@spec ", StringComparison.Ordinal))
                {
                    specs.Add(FunctionName(trimmed[6..]));
                    continue;
                }

                if (trimmed.StartsWith("def ", StringComparison.Ordinal))
                {
                    defined.Add((moduleName, FunctionName(trimmed[4..])));
                    checkedFunctions++;
                }
            }

            Flush(moduleName, specs, defined, missing);
        }

        Assert.True(checkedFunctions > 500, $"Expected many generated functions, found {checkedFunctions}.");
        Assert.Empty(missing);

        static void Flush(string moduleName, HashSet<string> specs, List<(string Module, string Name)> defined, List<string> missing)
        {
            foreach (var (module, name) in defined)
            {
                if (!specs.Contains(name))
                {
                    missing.Add($"{module}.{name}");
                }
            }

            specs.Clear();
            defined.Clear();
        }

        static string FunctionName(string text)
        {
            var end = text.IndexOfAny(['(', ',', ' ']);
            return end < 0 ? text.Trim() : text[..end].Trim();
        }
    }

    private static AtsTypeRef BuilderTypeRef() => new()
    {
        TypeId = AtsConstants.BuilderTypeId,
        Category = AtsTypeCategory.Handle,
        IsInterface = true
    };

    /// <summary>
    /// Builds a context from the test assembly plus one synthetic capability on the builder.
    /// </summary>
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

    /// <summary>
    /// Returns the body of one generated function, from its signature to its `end`.
    /// </summary>
    private static string ExtractFunction(string moduleSource, string functionName)
    {
        var start = moduleSource.IndexOf($"  def {functionName}(%__MODULE__{{}} = target", StringComparison.Ordinal);
        Assert.True(start >= 0, $"The module does not define {functionName}.");

        var end = moduleSource.IndexOf("\n  end\n", start, StringComparison.Ordinal);
        return end < 0 ? moduleSource[start..] : moduleSource[start..(end + 7)];
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
