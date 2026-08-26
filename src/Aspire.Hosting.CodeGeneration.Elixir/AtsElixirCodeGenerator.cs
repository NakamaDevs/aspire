// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Shared.CodeGeneration;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Elixir;

/// <summary>
/// Generates an Elixir SDK from the ATS (Aspire Type System) capability model.
/// </summary>
/// <remarks>
/// <para>
/// Every ATS handle type becomes one Elixir module that holds a struct with a <c>:handle</c> field.
/// Every capability that targets the type becomes a function in that module. The function returns
/// <c>{:ok, value}</c> or <c>{:error, %Aspire.Error{}}</c>, and a bang variant raises instead.
/// </para>
/// <para>
/// The emitted modules stay small because the shared logic lives in <c>aspire_runtime.ex</c>.
/// </para>
/// </remarks>
internal sealed class AtsElixirCodeGenerator : ICodeGenerator
{
    /// <summary>
    /// The maximum number of lines a generated module file holds before the generator starts a new one.
    /// Modules are never split across two files.
    /// </summary>
    private const int MaxGeneratedFileLines = 2000;

    private const string GeneratedFileBaseName = "aspire_generated";
    private const string EntryFileName = "aspire.ex";
    private const string RuntimeModule = "Aspire.Runtime";

    /// <summary>
    /// Names that a generated function must not use. Elixir keywords and special forms cannot be
    /// function names at all, and a function that shadows a <c>Kernel</c> name makes the compiler
    /// warn about the conflict. The generator appends an underscore to any of them.
    /// </summary>
    private static readonly HashSet<string> s_reservedFunctionNames = new(StringComparer.Ordinal)
    {
        // Keywords and special forms.
        "after", "and", "alias", "case", "catch", "cond", "do", "else", "end", "false", "fn", "for",
        "if", "import", "in", "nil", "not", "or", "quote", "receive", "require", "rescue", "super",
        "true", "try", "unless", "unquote", "unquote_splicing", "when", "with",
        // Definition macros.
        "def", "defdelegate", "defexception", "defguard", "defguardp", "defimpl", "defmacro",
        "defmacrop", "defmodule", "defoverridable", "defp", "defprotocol", "defstruct", "use",
        // Kernel functions and macros that a generated name would shadow.
        "abs", "alias!", "apply", "binary_part", "binary_slice", "binding", "bit_size", "byte_size",
        "ceil", "dbg", "destructure", "div", "elem", "exit", "floor", "function_exported?",
        "get_and_update_in", "get_in", "hd", "inspect", "is_atom", "is_binary", "is_bitstring",
        "is_boolean", "is_exception", "is_float", "is_function", "is_integer", "is_list", "is_map",
        "is_map_key", "is_nil", "is_non_struct_map", "is_number", "is_pid", "is_port",
        "is_reference", "is_struct", "is_tuple", "length", "macro_exported?", "make_ref", "map_size",
        "match?", "max", "min", "node", "pop_in", "put_elem", "put_in", "raise", "rem", "reraise",
        "round", "self", "send", "spawn", "spawn_link", "spawn_monitor", "struct", "struct!", "tap",
        "then", "throw", "tl", "to_char_list", "to_charlist", "to_string", "to_timeout", "trunc",
        "tuple_size", "update_in", "var!",
        // Names the generated module bodies already define or rely on.
        "handle", "transport"
    };

    /// <summary>
    /// Local variable names that a generated function body uses. A parameter that maps to one of
    /// them gets a trailing underscore so it cannot shadow the body.
    /// </summary>
    private static readonly HashSet<string> s_reservedLocalNames = new(StringComparer.Ordinal)
    {
        "target", "opts", "args", "transport"
    };

    /// <summary>
    /// Module names that <c>base.ex</c>, <c>transport.ex</c> and <c>aspire_runtime.ex</c> already
    /// define. A generated module that took one of them would redefine a runtime module.
    /// </summary>
    private static readonly HashSet<string> s_reservedModuleNames = new(StringComparer.Ordinal)
    {
        "Aspire", "Aspire.CancellationToken", "Aspire.Dict", "Aspire.Enums", "Aspire.Error",
        "Aspire.Handle", "Aspire.List", "Aspire.Marshal", "Aspire.ReferenceExpression",
        "Aspire.Runtime", "Aspire.Transport", "Aspire.Values", "Aspire.Watch"
    };

    /// <summary>
    /// The Elixir module of a mutable .NET list. <c>aspire_runtime.ex</c> defines it.
    /// </summary>
    private const string ListModule = "Aspire.List";

    /// <summary>
    /// The Elixir module of a mutable .NET dictionary. <c>aspire_runtime.ex</c> defines it.
    /// </summary>
    private const string DictModule = "Aspire.Dict";

    /// <summary>
    /// The Elixir module of a reference expression. <c>base.ex</c> defines it, so the generator
    /// never emits a module for the ATS type. The hand written module holds the format, the value
    /// providers, and <c>get_value_async/2</c>.
    /// </summary>
    private const string ReferenceExpressionModule = "Aspire.ReferenceExpression";

    private const string ErrorType = "Aspire.Error.t()";

    private static string ResolveReservedModuleName(string moduleName) =>
        s_reservedModuleNames.Contains(moduleName) ? moduleName + "Type" : moduleName;

    /// <inheritdoc />
    public string Language => "Elixir";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(AtsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var model = ElixirModel.Build(context);

        var modules = new List<ElixirModuleSource>();
        modules.AddRange(GenerateEnumModules(model));
        modules.AddRange(GenerateDtoModules(model));
        modules.AddRange(GenerateExportedValueModules(model));
        modules.AddRange(GenerateHandleModules(model));

        var generatedFiles = ChunkModules(modules);

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["base.ex"] = GetEmbeddedResource("base.ex"),
            ["transport.ex"] = GetEmbeddedResource("transport.ex"),
            ["aspire_runtime.ex"] = GetEmbeddedResource("aspire_runtime.ex"),
            // The watcher is a standalone script. aspire.ex never loads it: the CLI runs it as the
            // parent process of the AppHost when watch mode is on.
            ["watch.exs"] = GetEmbeddedResource("watch.exs")
        };

        foreach (var generatedFile in generatedFiles)
        {
            files[generatedFile.FileName] = generatedFile.Source;
        }

        files[EntryFileName] = GenerateEntryFile(model, generatedFiles.Select(file => file.FileName).ToList());

        return files;
    }

    private static string GetEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Aspire.Hosting.CodeGeneration.Elixir.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ── Files ────────────────────────────────────────────────────────────────

    private static List<GeneratedFile> ChunkModules(List<ElixirModuleSource> modules)
    {
        var files = new List<GeneratedFile>();
        var writer = new ElixirWriter();
        var lines = 0;
        var index = 1;

        void Flush()
        {
            if (lines == 0)
            {
                return;
            }

            files.Add(new GeneratedFile(FileNameFor(index), Header(index) + writer.ToSource()));
            writer = new ElixirWriter();
            lines = 0;
            index++;
        }

        foreach (var module in modules)
        {
            if (lines > 0 && lines + module.LineCount > MaxGeneratedFileLines)
            {
                Flush();
            }

            writer.WriteRaw(module.Source);
            lines += module.LineCount;
        }

        Flush();

        if (files.Count == 0)
        {
            // An empty context still needs the file the entry point loads.
            files.Add(new GeneratedFile(FileNameFor(1), Header(1)));
        }

        return files;
    }

    private static string FileNameFor(int index) =>
        index == 1
            ? $"{GeneratedFileBaseName}.ex"
            : string.Create(CultureInfo.InvariantCulture, $"{GeneratedFileBaseName}_{index}.ex");

    private static string Header(int index)
    {
        var writer = new ElixirWriter();
        writer.WriteLine($"# {FileNameFor(index)} - Generated Aspire modules");
        writer.WriteLine("# GENERATED CODE - DO NOT EDIT");
        writer.WriteLine();
        return writer.ToSource();
    }

    private static string GenerateEntryFile(ElixirModel model, IReadOnlyList<string> generatedFileNames)
    {
        var writer = new ElixirWriter();

        writer.WriteLine("# aspire.ex - Aspire Elixir SDK entry point");
        writer.WriteLine("# GENERATED CODE - DO NOT EDIT");
        writer.WriteLine("#");
        writer.WriteLine("# `apphost.exs` requires this file. It loads the runtime files and the generated");
        writer.WriteLine("# modules in the order they need, then it defines the `Aspire` facade.");
        writer.WriteLine();
        writer.WriteLine("for aspire_module_file <- [");
        writer.WriteLine("      \"base.ex\",");
        writer.WriteLine("      \"transport.ex\",");
        writer.WriteLine("      \"aspire_runtime.ex\",");

        for (var i = 0; i < generatedFileNames.Count; i++)
        {
            var separator = i == generatedFileNames.Count - 1 ? "" : ",";
            writer.WriteLine($"      \"{generatedFileNames[i]}\"{separator}");
        }

        writer.WriteLine("    ] do");
        writer.WriteLine("  Code.require_file(aspire_module_file, __DIR__)");
        writer.WriteLine("end");
        writer.WriteLine();

        var builderModule = model.HandleModules.TryGetValue(AtsConstants.BuilderTypeId, out var name)
            ? name
            : "Aspire.DistributedApplicationBuilder";

        writer.WriteLine("defmodule Aspire do");
        writer.Indent();
        writer.WriteDoc("moduledoc", $"""
            The entry point of the Aspire Elixir SDK.

            `create_builder!/1` connects to the AppHost and returns a
            `{builderModule}` struct. Every capability is a function in the
            module of the type it targets.

                builder = Aspire.create_builder!()

                builder
                |> Aspire.build!()
                |> Aspire.run!()

            `ref/1` builds a reference expression from strings and handles.
            """);
        writer.WriteLine();
        writer.WriteLine($"@builder_module {builderModule}");
        writer.WriteLine();

        writer.WriteDoc("doc", """
            Creates a distributed application builder.

            The function connects to the AppHost socket in
            `REMOTE_APP_HOST_SOCKET_PATH` and authenticates with
            `ASPIRE_REMOTE_APPHOST_TOKEN`. The Aspire CLI sets both variables.

            ## Options

              * `:transport` — an already started transport.
              * `:args` — the command line arguments. The default is `System.argv/0`.
              * `:project_directory` — the AppHost project directory.
              * `:app_host_file_path` — the path of the AppHost file.
              * `:dashboard_application_name` — the name the dashboard shows.
            """);
        writer.WriteLine("@spec create_builder(keyword()) :: {:ok, struct()} | {:error, Aspire.Error.t()}");
        writer.WriteLine("def create_builder(opts \\\\ []) do");
        writer.Indent();
        writer.WriteLine($"case {RuntimeModule}.ensure_transport(opts) do");
        writer.Indent();
        writer.WriteLine("{:ok, transport} ->");
        writer.Indent();
        writer.WriteLine($"args = %{{\"argsOrOptions\" => {RuntimeModule}.create_builder_args(opts)}}");
        writer.WriteLine();
        writer.WriteLine("transport");
        writer.WriteLine($"|> {RuntimeModule}.invoke(\"{AtsConstants.CreateBuilderCapability}\", args)");
        writer.WriteLine($"|> {RuntimeModule}.result({{:handle, @builder_module}}, transport)");
        writer.Outdent();
        writer.WriteLine();
        writer.WriteLine("{:error, error} ->");
        writer.Indent();
        writer.WriteLine("{:error, error}");
        writer.Outdent();
        writer.Outdent();
        writer.WriteLine("end");
        writer.Outdent();
        writer.WriteLine("end");
        writer.WriteLine();

        writer.WriteDoc("doc", """
            Creates a distributed application builder. Raises `Aspire.Error` on a failure.

            See `create_builder/1` for the options.
            """);
        writer.WriteLine("@spec create_builder!(keyword()) :: struct()");
        writer.WriteLine($"def create_builder!(opts \\\\ []), do: {RuntimeModule}.ok!(create_builder(opts))");
        writer.WriteLine();

        writer.WriteDoc("doc", "Builds the distributed application from the builder.");
        writer.WriteLine($"@spec build(struct()) :: {{:ok, term()}} | {{:error, {ErrorType}}}");
        writer.WriteLine("def build(%module{} = builder), do: module.build(builder)");
        writer.WriteLine();
        writer.WriteDoc("doc", "Builds the distributed application. Raises `Aspire.Error` on a failure.");
        writer.WriteLine("@spec build!(struct()) :: term()");
        writer.WriteLine("def build!(%module{} = builder), do: module.build!(builder)");
        writer.WriteLine();
        writer.WriteDoc("doc", "Runs the distributed application. It returns when the application stops.");
        writer.WriteLine($"@spec run(struct()) :: {{:ok, term()}} | {{:error, {ErrorType}}}");
        writer.WriteLine("def run(%module{} = app), do: module.run(app)");
        writer.WriteLine();
        writer.WriteDoc("doc", "Runs the distributed application. Raises `Aspire.Error` on a failure.");
        writer.WriteLine("@spec run!(struct()) :: term()");
        writer.WriteLine("def run!(%module{} = app), do: module.run!(app)");
        writer.WriteLine();

        writer.WriteDoc("doc", """
            Builds a reference expression from a list of parts.

            A binary part is literal text. Every other part becomes a value provider, and the
            format string gets a `{n}` placeholder for it. The AppHost resolves the expression
            when it needs the value.

                Aspire.ref(["http://", endpoint, "/health"])

            ## Parameters

              * `parts` — the strings and handles of the expression, in order.

            ## Returns

            An `Aspire.ReferenceExpression` struct.
            """);
        writer.WriteLine("@spec ref([term()]) :: Aspire.ReferenceExpression.t()");
        writer.WriteLine("def ref(parts) when is_list(parts), do: Aspire.ReferenceExpression.from_parts(parts)");
        writer.Outdent();
        writer.WriteLine("end");

        return writer.ToSource();
    }

    // ── Enums ────────────────────────────────────────────────────────────────

    private static List<ElixirModuleSource> GenerateEnumModules(ElixirModel model)
    {
        var modules = new List<ElixirModuleSource>();

        foreach (var enumType in model.Context.EnumTypes.OrderBy(e => e.TypeId, StringComparer.Ordinal))
        {
            if (!model.EnumModules.TryGetValue(enumType.TypeId, out var moduleName))
            {
                continue;
            }

            var writer = new ElixirWriter();
            writer.WriteLine($"defmodule {moduleName} do");
            writer.Indent();

            var members = enumType.Values.ToList();
            var atoms = AssignUniqueNames(members, ToElixirAtomName);
            var allowed = string.Join(", ", members.Select(member => ":" + atoms[member]));

            var valueDocs = members
                .Select(member => (
                    Name: $"`:{atoms[member]}`",
                    Description: enumType.ValueInfos
                        .FirstOrDefault(info => string.Equals(info.Name, member, StringComparison.Ordinal))
                        ?.Documentation?.Summary))
                .ToList();

            writer.WriteDoc("moduledoc", BuildDoc(
                enumType.Documentation,
                descriptionFallback: null,
                defaultSummary: $"The `{enumType.Name}` enum. A value is an atom and the wire form is the .NET member name.",
                sections: [new DocSection("## Values", valueDocs)]));

            writer.WriteLine();
            writer.WriteLine($"@values [{allowed}]");
            writer.WriteLine($"@to_wire %{{{string.Join(", ", members.Select(member => $"{atoms[member]}: \"{member}\""))}}}");
            writer.WriteLine($"@from_wire %{{{string.Join(", ", members.Select(member => $"\"{member}\" => :{atoms[member]}"))}}}");
            writer.WriteLine();
            writer.WriteLine("@typedoc \"A value of the enum. The wire form is the .NET member name.\"");
            writer.WriteLine("@type t :: atom()");
            writer.WriteLine();
            writer.WriteDoc("doc", "Returns every value of the enum.");
            writer.WriteLine("@spec values() :: [t()]");
            writer.WriteLine("def values, do: @values");
            writer.WriteLine();
            writer.WriteDoc("doc", $"""
                Returns the wire form of a value. Raises `ArgumentError` on an unknown value.

                The error message names every value the enum accepts.
                """);
            writer.WriteLine("@spec to_wire!(t() | String.t()) :: String.t()");
            writer.WriteLine("def to_wire!(value) when is_binary(value), do: value");
            writer.WriteLine();
            writer.WriteLine("def to_wire!(value) when is_atom(value) do");
            writer.Indent();
            writer.WriteLine("case Map.fetch(@to_wire, value) do");
            writer.Indent();
            writer.WriteLine("{:ok, name} ->");
            writer.Indent();
            writer.WriteLine("name");
            writer.Outdent();
            writer.WriteLine();
            writer.WriteLine(":error ->");
            writer.Indent();
            writer.WriteLine("raise ArgumentError,");
            writer.Indent();
            writer.WriteLine($"\"{moduleName} does not accept #{{inspect(value)}}. It accepts {EscapeInterpolation(allowed)}.\"");
            writer.Outdent();
            writer.Outdent();
            writer.Outdent();
            writer.WriteLine("end");
            writer.Outdent();
            writer.WriteLine("end");
            writer.WriteLine();
            writer.WriteDoc("doc", "Returns `{:ok, wire_form}`, or `:error` when the enum has no such value.");
            writer.WriteLine("@spec to_wire(t() | String.t()) :: {:ok, String.t()} | :error");
            writer.WriteLine("def to_wire(value) when is_binary(value), do: {:ok, value}");
            writer.WriteLine("def to_wire(value) when is_atom(value), do: Map.fetch(@to_wire, value)");
            writer.WriteLine();
            writer.WriteDoc("doc", "Returns the value of a wire form. An unknown form returns without a change.");
            writer.WriteLine("@spec from_wire(term()) :: t() | term()");
            writer.WriteLine("def from_wire(name) when is_binary(name), do: Map.get(@from_wire, name, name)");
            writer.WriteLine("def from_wire(value), do: value");
            writer.Outdent();
            writer.WriteLine("end");
            writer.WriteLine();

            modules.Add(new ElixirModuleSource(moduleName, writer.ToSource()));
        }

        return modules;
    }

    /// <summary>
    /// Escapes the text that goes inside a generated interpolated string literal.
    /// </summary>
    private static string EscapeInterpolation(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("#{", "\\#{", StringComparison.Ordinal);

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private static List<ElixirModuleSource> GenerateDtoModules(ElixirModel model)
    {
        var modules = new List<ElixirModuleSource>();

        foreach (var dto in model.Context.DtoTypes.OrderBy(d => d.TypeId, StringComparer.Ordinal))
        {
            if (!model.DtoModules.TryGetValue(dto.TypeId, out var moduleName))
            {
                continue;
            }

            var properties = dto.Properties.ToList();
            var fieldNames = AssignUniqueNames(properties.Select(p => p.Name).ToList(), ToSnakeCase);

            var writer = new ElixirWriter();
            writer.WriteLine($"defmodule {moduleName} do");
            writer.Indent();
            writer.WriteDoc("moduledoc", BuildDoc(
                dto.Documentation,
                dto.Description,
                $"The `{dto.Name}` data object.",
                sections: [
                    new DocSection(
                        "## Properties",
                        properties
                            .Select(property => (
                                Name: $"`:{fieldNames[property.Name]}`",
                                Description: property.Documentation?.Summary ?? property.Description))
                            .ToList())
                ]));
            writer.WriteLine();
            writer.WriteLine($"defstruct [{string.Join(", ", properties.Select(p => ":" + fieldNames[p.Name]))}]");
            writer.WriteLine();

            if (properties.Count == 0)
            {
                writer.WriteLine("@type t :: %__MODULE__{}");
            }
            else
            {
                writer.WriteLine("@type t :: %__MODULE__{");
                writer.Indent();
                for (var i = 0; i < properties.Count; i++)
                {
                    var property = properties[i];
                    var separator = i == properties.Count - 1 ? "" : ",";
                    var spec = TypeSpecOf(model, property.Type, property.IsCallback, byValue: true);
                    writer.WriteLine($"{fieldNames[property.Name]}: {spec} | nil{separator}");
                }
                writer.Outdent();
                writer.WriteLine("}");
            }

            writer.WriteLine();
            writer.WriteDoc("doc", """
                Builds the struct from a keyword list.

                A capability that flattens its `options` argument calls this function.
                """);
            writer.WriteLine("@spec new(keyword() | t()) :: t()");
            writer.WriteLine("def new(%__MODULE__{} = value), do: value");
            writer.WriteLine("def new(opts) when is_list(opts), do: struct!(__MODULE__, opts)");
            writer.WriteLine();
            writer.WriteDoc("doc", "Returns the wire form of the struct. The nil properties are removed.");
            writer.WriteLine("@spec to_wire(t()) :: map()");

            if (properties.Count == 0)
            {
                writer.WriteLine("def to_wire(%__MODULE__{} = _value), do: %{}");
            }
            else
            {
                writer.WriteLine("def to_wire(%__MODULE__{} = value) do");
                writer.Indent();
                writer.WriteLine($"{RuntimeModule}.build_wire([");
                writer.Indent();
                for (var i = 0; i < properties.Count; i++)
                {
                    var property = properties[i];
                    var separator = i == properties.Count - 1 ? "" : ",";
                    var field = $"value.{fieldNames[property.Name]}";

                    if (property.IsCallback && property.CallbackParameters is { } callbackParameters)
                    {
                        // A callback property carries an Elixir function. The wrapper decodes the
                        // arguments of the host before the function runs, so a nested callback sees
                        // the same typed values as a callback the caller passes to a capability.
                        writer.WriteLine($"{{\"{property.Name}\", {RuntimeModule}.wrap_callback({field}, fn callback ->");
                        writer.Indent();
                        WriteCallbackFunction(
                            model,
                            writer,
                            "callback",
                            callbackParameters,
                            property.CallbackReturnType,
                            transportExpression: "nil");
                        writer.Outdent();
                        writer.WriteLine($"end)}}{separator}");
                    }
                    else
                    {
                        writer.WriteLine($"{{\"{property.Name}\", {field}}}{separator}");
                    }
                }
                writer.Outdent();
                writer.WriteLine("])");
                writer.Outdent();
                writer.WriteLine("end");
            }

            writer.WriteLine();
            writer.WriteDoc("doc", """
                Builds the struct from a wire map.

                A property the map does not hold becomes nil. A property the decoder cannot
                convert keeps its wire value, so one unknown shape never fails the whole struct.
                """);
            writer.WriteLine("@spec from_wire(term()) :: t() | term()");

            if (properties.Count == 0)
            {
                writer.WriteLine("def from_wire(%{} = _wire), do: %__MODULE__{}");
            }
            else
            {
                writer.WriteLine("def from_wire(%{} = wire) do");
                writer.Indent();
                writer.WriteLine("%__MODULE__{");
                writer.Indent();
                for (var i = 0; i < properties.Count; i++)
                {
                    var property = properties[i];
                    var separator = i == properties.Count - 1 ? "" : ",";
                    var decoder = BuildDecoder(model, property.Type, property.IsCallback, byValue: true);
                    var call = decoder is null
                        ? $"{RuntimeModule}.wire_get(wire, \"{property.Name}\")"
                        : $"{RuntimeModule}.wire_get(wire, \"{property.Name}\", {decoder})";
                    writer.WriteLine($"{fieldNames[property.Name]}: {call}{separator}");
                }
                writer.Outdent();
                writer.WriteLine("}");
                writer.Outdent();
                writer.WriteLine("end");
            }

            writer.WriteLine();
            writer.WriteLine("def from_wire(value), do: value");
            writer.Outdent();
            writer.WriteLine("end");
            writer.WriteLine();

            modules.Add(new ElixirModuleSource(moduleName, writer.ToSource()));
        }

        return modules;
    }

    // ── Exported values ──────────────────────────────────────────────────────

    private static List<ElixirModuleSource> GenerateExportedValueModules(ElixirModel model)
    {
        var modules = new List<ElixirModuleSource>();

        var groups = model.Context.ExportedValues
            .Where(value => value.PathSegments.Count > 0)
            .GroupBy(
                value => string.Join(
                    ".",
                    new[] { "Aspire", "Values" }.Concat(
                        value.PathSegments.Take(value.PathSegments.Count - 1).Select(SanitizeModuleSegment))),
                StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var writer = new ElixirWriter();
            writer.WriteLine($"defmodule {group.Key} do");
            writer.Indent();
            writer.WriteDoc("moduledoc", """
                Exported Aspire values.

                The values are snapped when the SDK is generated. A value that has a DTO type
                arrives as the struct of that DTO.
                """);

            var values = group.OrderBy(value => value.PathSegments[^1], StringComparer.Ordinal).ToList();
            var functionNames = AssignUniqueNames(
                values.Select(value => value.PathSegments[^1]).ToList(),
                ToElixirFunctionName);

            foreach (var value in values)
            {
                var functionName = functionNames[value.PathSegments[^1]];
                writer.WriteLine();
                writer.WriteDoc("doc", BuildDoc(
                    value.Documentation,
                    value.Description,
                    $"The exported `{string.Join(".", value.PathSegments)}` value."));

                writer.WriteLine($"@spec {functionName}() :: {TypeSpecOf(model, value.Type)}");
                writer.WriteLine($"def {functionName} do");
                writer.Indent();

                // A snapped value arrives in its wire form. The decoder turns a DTO into its struct
                // so a catalog entry looks the same as a value the host returned.
                var decoder = BuildDecoder(model, value.Type, isCallback: false);
                var literal = FormatElixirValue(value.Value);
                writer.WriteLine(decoder is null
                    ? literal
                    : $"{RuntimeModule}.decode({literal}, {decoder}, nil)");

                writer.Outdent();
                writer.WriteLine("end");
            }

            writer.Outdent();
            writer.WriteLine("end");
            writer.WriteLine();

            modules.Add(new ElixirModuleSource(group.Key, writer.ToSource()));
        }

        return modules;
    }

    // ── Handle types ─────────────────────────────────────────────────────────

    private static List<ElixirModuleSource> GenerateHandleModules(ElixirModel model)
    {
        var modules = new List<ElixirModuleSource>();

        foreach (var typeId in model.HandleModules.Keys.OrderBy(id => model.HandleModules[id], StringComparer.Ordinal))
        {
            if (model.RuntimeProvidedTypeIds.Contains(typeId))
            {
                // base.ex already defines the module. See ReferenceExpressionModule.
                continue;
            }

            var moduleName = model.HandleModules[typeId];
            var writer = new ElixirWriter();

            writer.WriteLine($"defmodule {moduleName} do");
            writer.Indent();
            writer.WriteDoc(
                "moduledoc",
                BuildTypeDoc(
                    model.HandleTypeInfos.TryGetValue(typeId, out var typeInfo) ? typeInfo.Documentation : null,
                    fallback: null,
                    $"A handle to `{typeId}` in the AppHost."));
            writer.WriteLine();
            writer.WriteLine("@enforce_keys [:handle]");
            writer.WriteLine("defstruct [:handle, :transport]");
            writer.WriteLine();
            writer.WriteLine("@type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}");

            if (model.CapabilitiesByTarget.TryGetValue(typeId, out var capabilities))
            {
                var functionNames = AssignUniqueNames(
                    capabilities.Select(capability => capability.CapabilityId).ToList(),
                    _ => "",
                    capabilities.ToDictionary(
                        capability => capability.CapabilityId,
                        capability => ToElixirFunctionName(capability.MethodName),
                        StringComparer.Ordinal));

                foreach (var capability in capabilities)
                {
                    GenerateCapability(model, writer, moduleName, typeId, capability, functionNames[capability.CapabilityId]);
                }
            }

            writer.Outdent();
            writer.WriteLine("end");
            writer.WriteLine();

            modules.Add(new ElixirModuleSource(moduleName, writer.ToSource()));
        }

        return modules;
    }

    private static void GenerateCapability(
        ElixirModel model,
        ElixirWriter writer,
        string moduleName,
        string moduleTypeId,
        AtsCapabilityInfo capability,
        string functionName)
    {
        var targetParameterName = capability.TargetParameterName ?? "builder";
        var parameters = capability.Parameters
            .Where(parameter => !string.Equals(parameter.Name, targetParameterName, StringComparison.Ordinal))
            .ToList();

        // A cancellation token of a method is always optional, even when the .NET parameter has no
        // default. The caller passes it as the `:cancellation_token` option, so
        // `Aspire.CancellationToken.cancel/1` can stop the call. A property setter keeps its
        // `value` parameter required, because that value is the property itself.
        bool AlwaysOptional(AtsParameterInfo parameter) =>
            IsCancellationTokenParameter(parameter)
            && capability.CapabilityKind != AtsCapabilityKind.PropertySetter;

        var required = parameters
            .Where(parameter => !parameter.IsOptional && !AlwaysOptional(parameter))
            .ToList();
        var optional = parameters
            .Where(parameter => parameter.IsOptional || AlwaysOptional(parameter))
            .ToList();

        // Go rule: one optional DTO named `options` and nothing else flattens into the keyword
        // list, so the caller writes the properties of the DTO directly. A cancellation token
        // beside it blocks the flattening, because both would share the same keyword list.
        var flattened = AtsOptionsFlattening.TryGetDirectOptionsParameter(
            optional,
            IsCancellationTokenParameter,
            cancellationTokenIsSeparateParameter: false,
            out var directOptions)
            && model.DtoModules.ContainsKey(directOptions.Type!.TypeId)
            && model.DtoPropertyKeys.ContainsKey(directOptions.Type.TypeId);

        var localNames = AssignUniqueNames(
            parameters.Select(parameter => parameter.Name).ToList(),
            ToElixirLocalName);

        var optionKeys = AssignUniqueNames(
            optional.Select(parameter => parameter.Name).ToList(),
            ToElixirAtomName);

        List<(string Key, string WireName, string? Description)> flattenedKeys = [];
        if (flattened)
        {
            flattenedKeys = model.DtoPropertyKeys[directOptions!.Type!.TypeId];
        }

        var hasOptions = flattened ? flattenedKeys.Count > 0 : optional.Count > 0;

        var signature = new StringBuilder();
        signature.Append("%__MODULE__{} = target");
        foreach (var parameter in required)
        {
            signature.Append(", ").Append(localNames[parameter.Name]);
        }

        if (hasOptions)
        {
            signature.Append(@", opts \\ []");
        }

        var arity = 1 + required.Count + (hasOptions ? 1 : 0);

        // A fluent capability returns its receiver. The declared .NET return type is the generic
        // constraint, which is usually an interface such as IResourceWithEnvironment, so decoding
        // into that type would end a pipe chain on a struct that carries no functions. The other
        // generators keep the receiver the same way: AtsTypeScriptCodeGenerator, AtsPythonCodeGenerator
        // and AtsJavaCodeGenerator all treat a return type equal to the target as self-returning. A
        // factory method, such as AddDatabase, returns a different builder and keeps its own type.
        var returnsReceiver = capability.ReturnsBuilder
            && capability.ReturnType?.TypeId is { Length: > 0 } capabilityReturnTypeId
            && (string.Equals(capabilityReturnTypeId, capability.TargetTypeId, StringComparison.Ordinal)
                || string.Equals(capabilityReturnTypeId, moduleTypeId, StringComparison.Ordinal));

        var decoder = returnsReceiver
            ? "{:handle, __MODULE__}"
            : BuildDecoder(model, capability.ReturnType, isCallback: false);
        var returnSpec = returnsReceiver
            ? "t()"
            : ReturnTypeSpecOf(model, capability.ReturnType);

        var specTypes = new List<string> { "t()" };
        specTypes.AddRange(required.Select(parameter => TypeSpecOf(model, parameter.Type, parameter.IsCallback)));
        if (hasOptions)
        {
            specTypes.Add("keyword()");
        }

        var specArguments = string.Join(", ", specTypes);

        writer.WriteLine();
        WriteCapabilityDoc(writer, capability, required, optional, localNames, optionKeys, flattened, flattenedKeys);
        writer.WriteLine($"@spec {functionName}({specArguments}) :: {{:ok, {returnSpec}}} | {{:error, {ErrorType}}}");
        writer.WriteLine($"def {functionName}({signature}) do");
        writer.Indent();

        if (hasOptions)
        {
            var allowed = flattened
                ? string.Join(", ", flattenedKeys.Select(entry => ":" + entry.Key))
                : string.Join(", ", optional.Select(parameter => ":" + optionKeys[parameter.Name]));
            writer.WriteLine($"{RuntimeModule}.validate_opts!(opts, [{allowed}], \"{moduleName}.{functionName}/{arity}\")");
        }

        writer.WriteLine($"transport = {RuntimeModule}.transport_of(target)");

        // A callback needs a wrapper that decodes the arguments of the host before the function of
        // the caller runs. The wrapper closes over `transport`, so it is built here.
        var wrapperNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in parameters)
        {
            if (!parameter.IsCallback || parameter.CallbackParameters is not { } callbackParameters)
            {
                continue;
            }

            if (flattened && ReferenceEquals(parameter, directOptions))
            {
                continue;
            }

            var wrapperName = localNames[parameter.Name] + "_wrapper";
            wrapperNames[parameter.Name] = wrapperName;

            writer.WriteLine();
            writer.WriteLine($"{wrapperName} = fn callback ->");
            writer.Indent();
            WriteCallbackFunction(model, writer, "callback", callbackParameters, parameter.CallbackReturnType, "transport");
            writer.Outdent();
            writer.WriteLine("end");
        }

        writer.WriteLine();

        if (required.Count == 0 && !hasOptions)
        {
            writer.WriteLine($"args = %{{\"{targetParameterName}\" => {RuntimeModule}.handle_of(target)}}");
        }
        else
        {
            writer.WriteLine("args =");
            writer.Indent();
            writer.WriteLine($"%{{\"{targetParameterName}\" => {RuntimeModule}.handle_of(target)}}");

            foreach (var parameter in required)
            {
                var local = localNames[parameter.Name];
                var value = wrapperNames.TryGetValue(parameter.Name, out var wrapper)
                    ? $"{wrapper}.({local})"
                    : EncodeExpression(model, parameter, local, $"{moduleName}.{functionName}/{arity}");
                writer.WriteLine($"|> Map.put(\"{parameter.Name}\", {value})");
            }

            if (flattened)
            {
                var dtoModule = model.DtoModules[directOptions!.Type!.TypeId];
                writer.WriteLine($"|> {RuntimeModule}.put_opts_dto(\"{directOptions.Name}\", opts, {dtoModule})");
            }
            else
            {
                foreach (var parameter in optional)
                {
                    writer.WriteLine($"|> {PutOptionExpression(model, parameter, optionKeys[parameter.Name], wrapperNames)}");
                }
            }

            writer.Outdent();
        }

        writer.WriteLine();
        writer.WriteLine("transport");
        writer.WriteLine($"|> {RuntimeModule}.invoke(\"{capability.CapabilityId}\", args)");
        writer.WriteLine($"|> {RuntimeModule}.result({decoder ?? "nil"}, transport)");
        writer.Outdent();
        writer.WriteLine("end");

        // Bang variant.
        var bangArguments = new StringBuilder("target");
        foreach (var parameter in required)
        {
            bangArguments.Append(", ").Append(localNames[parameter.Name]);
        }

        if (hasOptions)
        {
            bangArguments.Append(", opts");
        }

        writer.WriteLine();
        writer.WriteDoc("doc", $"The same as `{functionName}/{arity}`. Raises `Aspire.Error` on a failure.");
        writer.WriteLine($"@spec {functionName}!({specArguments}) :: {returnSpec}");
        writer.WriteLine($"def {functionName}!({signature}) do");
        writer.Indent();
        writer.WriteLine($"{RuntimeModule}.ok!({functionName}({bangArguments}))");
        writer.Outdent();
        writer.WriteLine("end");
    }

    /// <summary>
    /// Writes the anonymous function that the transport registers for a callback argument.
    /// </summary>
    /// <remarks>
    /// The host sends the arguments as <c>p0</c>, <c>p1</c>, … The function decodes each one with
    /// the decoder of its ATS type, so a context handle becomes its wrapper struct and a DTO
    /// becomes its struct. An Elixir struct never changes in place, so a callback that receives a
    /// DTO returns the changed struct and <c>callback_writeback/2</c> sends it back to the host.
    /// </remarks>
    private static void WriteCallbackFunction(
        ElixirModel model,
        ElixirWriter writer,
        string callbackName,
        IReadOnlyList<AtsCallbackParameterInfo> callbackParameters,
        AtsTypeRef? callbackReturnType,
        string transportExpression)
    {
        var arguments = new List<string>();
        for (var i = 0; i < callbackParameters.Count; i++)
        {
            arguments.Add(string.Create(CultureInfo.InvariantCulture, $"a{i}"));
        }

        writer.WriteLine(arguments.Count == 0
            ? "fn ->"
            : $"fn {string.Join(", ", arguments)} ->");
        writer.Indent();

        var dtoArguments = new List<string>();
        for (var i = 0; i < callbackParameters.Count; i++)
        {
            var callbackParameter = callbackParameters[i];
            var decoder = BuildDecoder(model, callbackParameter.Type, isCallback: false);

            if (decoder is not null)
            {
                writer.WriteLine($"{arguments[i]} = {RuntimeModule}.decode({arguments[i]}, {decoder}, {transportExpression})");
            }

            if (callbackParameter.Type.Category == AtsTypeCategory.Dto
                && model.DtoModules.TryGetValue(callbackParameter.Type.TypeId, out var dtoModule))
            {
                dtoArguments.Add(string.Create(CultureInfo.InvariantCulture, $"{{{i}, {dtoModule}, {arguments[i]}}}"));
            }
        }

        var call = $"{callbackName}.({string.Join(", ", arguments)})";
        var returnsValue = callbackReturnType is not null
            && !string.Equals(callbackReturnType.TypeId, AtsConstants.Void, StringComparison.Ordinal);

        writer.WriteLine(returnsValue
            ? $"{RuntimeModule}.encode({call})"
            : $"{RuntimeModule}.callback_writeback({call}, [{string.Join(", ", dtoArguments)}])");

        writer.Outdent();
        writer.WriteLine("end");
    }

    private static void WriteCapabilityDoc(
        ElixirWriter writer,
        AtsCapabilityInfo capability,
        IReadOnlyList<AtsParameterInfo> required,
        IReadOnlyList<AtsParameterInfo> optional,
        IReadOnlyDictionary<string, string> localNames,
        IReadOnlyDictionary<string, string> optionKeys,
        bool flattened,
        IReadOnlyList<(string Key, string WireName, string? Description)> flattenedKeys)
    {
        var parameterDocs = required
            .Select(parameter => (
                Name: $"`{localNames[parameter.Name]}`",
                Description: parameter.Documentation?.Summary
                    ?? DocumentationFor(capability.Documentation, parameter.Name)))
            .ToList();

        var optionDocs = flattened
            ? flattenedKeys.Select(entry => (Name: $"`:{entry.Key}`", entry.Description)).ToList()
            : optional
                .Select(parameter => (
                    Name: $"`:{optionKeys[parameter.Name]}`",
                    Description: parameter.Documentation?.Summary
                        ?? DocumentationFor(capability.Documentation, parameter.Name)))
                .ToList();

        var isVoid = capability.ReturnType is null
            || string.Equals(capability.ReturnType.TypeId, AtsConstants.Void, StringComparison.Ordinal);

        var sections = new List<DocSection>();
        if (parameterDocs.Count > 0)
        {
            sections.Add(new DocSection("## Parameters", parameterDocs));
        }

        if (optionDocs.Count > 0)
        {
            sections.Add(new DocSection("## Options", optionDocs));
        }

        writer.WriteDoc("doc", BuildDoc(
            capability.Documentation,
            capability.Description,
            $"Invokes the `{capability.CapabilityId}` capability.",
            sections,
            suppressReturns: isVoid,
            isObsolete: capability.IsObsolete,
            obsoleteMessage: capability.ObsoleteMessage));
    }

    private static string EncodeExpression(
        ElixirModel model,
        AtsParameterInfo parameter,
        string localName,
        string functionName)
    {
        if (parameter.IsCallback)
        {
            // The transport registers a function and sends the callback identifier.
            return localName;
        }

        if (parameter.Type is { Category: AtsTypeCategory.Union } unionType)
        {
            var specs = BuildUnionSpecs(model, unionType);
            if (specs.Count > 0)
            {
                return $"{RuntimeModule}.encode_union!({localName}, [{string.Join(", ", specs)}], \"{functionName}\", \"{parameter.Name}\")";
            }
        }

        if (parameter.Type is { Category: AtsTypeCategory.Enum } enumType
            && model.EnumModules.TryGetValue(enumType.TypeId, out var enumModule))
        {
            return $"{RuntimeModule}.encode_enum!({localName}, {enumModule})";
        }

        return $"{RuntimeModule}.encode({localName})";
    }

    private static string PutOptionExpression(
        ElixirModel model,
        AtsParameterInfo parameter,
        string optionKey,
        IReadOnlyDictionary<string, string> wrapperNames)
    {
        if (parameter.IsCallback)
        {
            return wrapperNames.TryGetValue(parameter.Name, out var wrapper)
                ? $"{RuntimeModule}.put_opt_callback(\"{parameter.Name}\", opts, :{optionKey}, {wrapper})"
                : $"{RuntimeModule}.put_opt_raw(\"{parameter.Name}\", opts, :{optionKey})";
        }

        if (parameter.Type is { Category: AtsTypeCategory.Enum } enumType
            && model.EnumModules.TryGetValue(enumType.TypeId, out var enumModule))
        {
            return $"{RuntimeModule}.put_opt_enum!(\"{parameter.Name}\", opts, :{optionKey}, {enumModule})";
        }

        return $"{RuntimeModule}.put_opt(\"{parameter.Name}\", opts, :{optionKey})";
    }

    /// <summary>
    /// Builds the accepted forms of an <c>[AspireUnion]</c> parameter.
    /// </summary>
    /// <remarks>
    /// A handle member expands to the module of the member and to the module of every handle type
    /// that implements or extends it, because a caller passes the concrete wrapper struct. Two
    /// members can expand to the same module, so the list is deduplicated and sorted.
    /// </remarks>
    private static List<string> BuildUnionSpecs(ElixirModel model, AtsTypeRef unionType)
    {
        if (unionType.UnionTypes is not { Count: > 0 } members)
        {
            return [];
        }

        var primitives = new SortedSet<string>(StringComparer.Ordinal);
        var modules = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var member in members)
        {
            switch (member.Category)
            {
                case AtsTypeCategory.Handle:
                    foreach (var moduleName in model.ExpandUnionMember(member.TypeId))
                    {
                        modules.Add(moduleName);
                    }

                    break;

                case AtsTypeCategory.Dto:
                    if (model.DtoModules.TryGetValue(member.TypeId, out var dtoModule))
                    {
                        modules.Add(dtoModule);
                    }

                    break;

                case AtsTypeCategory.Enum:
                    primitives.Add(":string");
                    break;

                case AtsTypeCategory.Array:
                case AtsTypeCategory.List:
                case AtsTypeCategory.Dict:
                case AtsTypeCategory.Callback:
                case AtsTypeCategory.Union:
                    primitives.Add(":any");
                    break;

                default:
                    primitives.Add(member.TypeId switch
                    {
                        AtsConstants.Number => ":number",
                        AtsConstants.Boolean => ":boolean",
                        AtsConstants.Any => ":any",
                        _ => ":string"
                    });
                    break;
            }
        }

        var specs = new List<string>(primitives);
        specs.AddRange(modules.Select(moduleName => $"{{:module, {moduleName}}}"));
        return specs;
    }

    private static bool IsCancellationTokenParameter(AtsParameterInfo parameter) =>
        IsCancellationTokenTypeId(parameter.Type?.TypeId);

    private static string? BuildDecoder(ElixirModel model, AtsTypeRef? typeRef, bool isCallback, bool byValue = false)
    {
        if (typeRef is null || isCallback || string.Equals(typeRef.TypeId, AtsConstants.Void, StringComparison.Ordinal))
        {
            return null;
        }

        if (IsCancellationTokenTypeId(typeRef.TypeId))
        {
            return null;
        }

        switch (typeRef.Category)
        {
            case AtsTypeCategory.Handle:
                if (model.DtoModules.TryGetValue(typeRef.TypeId, out var dtoAsHandle))
                {
                    return $"{{:dto, {dtoAsHandle}}}";
                }

                return model.HandleModules.TryGetValue(typeRef.TypeId, out var handleModule)
                    ? $"{{:handle, {handleModule}}}"
                    : null;

            case AtsTypeCategory.Dto:
                return model.DtoModules.TryGetValue(typeRef.TypeId, out var dtoModule)
                    ? $"{{:dto, {dtoModule}}}"
                    : null;

            case AtsTypeCategory.Enum:
                return model.EnumModules.TryGetValue(typeRef.TypeId, out var enumModule)
                    ? $"{{:enum, {enumModule}}}"
                    : null;

            case AtsTypeCategory.List when !byValue:
                // A mutable list stays in the AppHost. The host returns a handle and
                // `Aspire.List` sends every operation back. A read-only collection is category
                // Array instead, so it arrives as a plain Elixir list.
                return $"{{:handle, {ListModule}}}";

            case AtsTypeCategory.Dict when !typeRef.IsReadOnly && !byValue:
                return $"{{:handle, {DictModule}}}";

            case AtsTypeCategory.Array:
            case AtsTypeCategory.List:
                var inner = BuildDecoder(model, typeRef.ElementType, isCallback: false, byValue: byValue);
                return inner is null ? null : $"{{:list, {inner}}}";

            default:
                return null;
        }
    }

    // ── Documentation ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the text of a <c>@doc</c> or <c>@moduledoc</c> attribute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The summary comes from <see cref="AtsDocumentationInfo.Summary"/>. An empty
    /// <c>&lt;ats-summary&gt;</c> element suppresses the summary: the scanner then returns a
    /// documentation object whose <c>Summary</c> is <see langword="null"/>. The generator keeps
    /// that decision and does not fall back to the <c>[AspireExport(Description = ...)]</c> text.
    /// The fallback applies only when the whole documentation object is missing.
    /// </para>
    /// </remarks>
    private static string BuildDoc(
        AtsDocumentationInfo? documentation,
        string? descriptionFallback,
        string? defaultSummary,
        IReadOnlyList<DocSection>? sections = null,
        bool suppressReturns = false,
        bool isObsolete = false,
        string? obsoleteMessage = null)
    {
        var summary = documentation is null
            ? Coalesce(descriptionFallback, defaultSummary)
            : Coalesce(documentation.Summary, defaultSummary);

        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(summary))
        {
            lines.Add(ConvertAtsReferences(summary.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(documentation?.Remarks))
        {
            AddBlank(lines);
            lines.Add(ConvertAtsReferences(documentation.Remarks.Trim()));
        }

        if (isObsolete)
        {
            AddBlank(lines);
            lines.Add("> #### Obsolete {: .warning}");
            lines.Add(">");
            lines.Add("> " + ConvertAtsReferences(Flatten(obsoleteMessage ?? "This capability is obsolete.")));
        }

        foreach (var section in sections ?? [])
        {
            AddSection(lines, section.Heading, section.Entries);
        }

        if (!suppressReturns && !string.IsNullOrWhiteSpace(documentation?.Returns))
        {
            AddBlank(lines);
            lines.Add("## Returns");
            AddBlank(lines);
            lines.Add(ConvertAtsReferences(documentation.Returns.Trim()));
        }

        return string.Join("\n", lines);

        static string? Coalesce(string? first, string? second) =>
            string.IsNullOrWhiteSpace(first) ? second : first;

        static void AddBlank(List<string> lines)
        {
            if (lines.Count > 0)
            {
                lines.Add("");
            }
        }

        static void AddSection(
            List<string> lines,
            string heading,
            IReadOnlyList<(string Name, string? Description)>? entries)
        {
            if (entries is null || entries.Count == 0)
            {
                return;
            }

            AddBlank(lines);
            lines.Add(heading);
            AddBlank(lines);

            foreach (var (name, description) in entries)
            {
                lines.Add(string.IsNullOrWhiteSpace(description)
                    ? $"  * {name}"
                    : $"  * {name} — {ConvertAtsReferences(Flatten(description))}");
            }
        }
    }

    /// <summary>
    /// Turns the language neutral reference markers of the scanner into ExDoc links.
    /// </summary>
    /// <remarks>
    /// The scanner writes <c>&lt;ats-see cref="!:type:Foo"/&gt;</c> as <c>{@ats-ref type:Foo}</c>,
    /// and adds <c>|label</c> when the element holds text. A marker without a label becomes a
    /// backticked name, which ExDoc turns into a link. A marker with a label becomes an ExDoc
    /// link with that text.
    /// </remarks>
    internal static string ConvertAtsReferences(string text)
    {
        const string Marker = "{@ats-ref ";

        if (!text.Contains(Marker, StringComparison.Ordinal))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var index = 0;

        while (index < text.Length)
        {
            var start = text.IndexOf(Marker, index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(text, index, text.Length - index);
                break;
            }

            var end = text.IndexOf('}', start);
            if (end < 0)
            {
                builder.Append(text, index, text.Length - index);
                break;
            }

            builder.Append(text, index, start - index);

            var reference = text[(start + Marker.Length)..end];
            var separator = reference.IndexOf(':', StringComparison.Ordinal);

            if (separator < 0)
            {
                // The marker carries no kind. Keep it as it is so nothing is lost.
                builder.Append(text, start, end - start + 1);
            }
            else
            {
                var remainder = reference[(separator + 1)..];
                var labelIndex = remainder.IndexOf('|', StringComparison.Ordinal);
                var target = labelIndex < 0 ? remainder : remainder[..labelIndex];
                var label = labelIndex < 0 ? null : remainder[(labelIndex + 1)..];

                builder.Append(string.IsNullOrWhiteSpace(label)
                    ? $"`{target}`"
                    : $"[{label}](`{target}`)");
            }

            index = end + 1;
        }

        return builder.ToString();
    }

    private static string BuildTypeDoc(AtsDocumentationInfo? documentation, string? fallback, string defaultSummary) =>
        BuildDoc(documentation, fallback, defaultSummary);

    /// <summary>
    /// A named list that a generated documentation block holds, such as the parameters of a
    /// function or the properties of a data object.
    /// </summary>
    private sealed record DocSection(string Heading, IReadOnlyList<(string Name, string? Description)> Entries);

    private static string Flatten(string value) =>
        string.Join(" ", value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? DocumentationFor(AtsDocumentationInfo? documentation, string parameterName) =>
        documentation?.Parameters.FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.Ordinal))?.Description;

    // ── Typespecs ────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps an ATS type reference to an Elixir typespec.
    /// </summary>
    /// <remarks>
    /// Every module the spec names has to define <c>t/0</c>, because
    /// <c>elixirc --warnings-as-errors</c> refuses an unknown remote type. The generated modules,
    /// <c>base.ex</c> and <c>aspire_runtime.ex</c> all define it.
    /// </remarks>
    private static string TypeSpecOf(ElixirModel model, AtsTypeRef? typeRef, bool isCallback = false, bool byValue = false)
    {
        if (isCallback)
        {
            return "function()";
        }

        if (typeRef is null)
        {
            return "term()";
        }

        if (IsCancellationTokenTypeId(typeRef.TypeId))
        {
            return "Aspire.CancellationToken.t()";
        }

        switch (typeRef.Category)
        {
            case AtsTypeCategory.Handle:
                if (model.DtoModules.TryGetValue(typeRef.TypeId, out var dtoAsHandle))
                {
                    return $"{dtoAsHandle}.t()";
                }

                return model.HandleModules.TryGetValue(typeRef.TypeId, out var handleModule)
                    ? $"{handleModule}.t()"
                    : "term()";

            case AtsTypeCategory.Dto:
                return model.DtoModules.TryGetValue(typeRef.TypeId, out var dtoModule)
                    ? $"{dtoModule}.t()"
                    : "map()";

            case AtsTypeCategory.Enum:
                return model.EnumModules.TryGetValue(typeRef.TypeId, out var enumModule)
                    ? $"{enumModule}.t()"
                    : "atom()";

            case AtsTypeCategory.Union:
                return "term()";

            case AtsTypeCategory.Array:
                return $"[{TypeSpecOf(model, typeRef.ElementType, byValue: byValue)}]";

            case AtsTypeCategory.List:
                // A DTO carries its collections by value. A capability instead returns a handle
                // to a mutable collection, so the caller gets an Aspire.List.
                return byValue
                    ? $"[{TypeSpecOf(model, typeRef.ElementType, byValue: true)}]"
                    : $"{ListModule}.t()";

            case AtsTypeCategory.Dict:
                return typeRef.IsReadOnly || byValue
                    ? "map()"
                    : $"{DictModule}.t()";

            case AtsTypeCategory.Callback:
                return "function()";

            default:
                return PrimitiveTypeSpec(typeRef.TypeId);
        }
    }

    private static string PrimitiveTypeSpec(string typeId) => typeId switch
    {
        AtsConstants.String or AtsConstants.Char or AtsConstants.Guid or AtsConstants.Uri
            or AtsConstants.DateTime or AtsConstants.DateTimeOffset or AtsConstants.DateOnly
            or AtsConstants.TimeOnly or AtsConstants.TimeSpan => "String.t()",
        AtsConstants.Number => "number()",
        AtsConstants.Boolean => "boolean()",
        AtsConstants.Void => "nil",
        _ => "term()"
    };

    /// <summary>
    /// Returns the typespec of a value that a capability returns, after the decoder ran.
    /// </summary>
    private static string ReturnTypeSpecOf(ElixirModel model, AtsTypeRef? typeRef)
    {
        if (typeRef is null || string.Equals(typeRef.TypeId, AtsConstants.Void, StringComparison.Ordinal))
        {
            return "nil";
        }

        return typeRef.Category switch
        {
            AtsTypeCategory.List => $"{ListModule}.t()",
            AtsTypeCategory.Dict when !typeRef.IsReadOnly => $"{DictModule}.t()",
            _ => TypeSpecOf(model, typeRef)
        };
    }

    // ── Literals ─────────────────────────────────────────────────────────────

    private static string FormatElixirValue(JsonNode? node)
    {
        if (node is null)
        {
            return "nil";
        }

        using var document = JsonDocument.Parse(node.ToJsonString());
        return FormatElixirElement(document.RootElement);
    }

    private static string FormatElixirElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                return $"[{string.Join(", ", element.EnumerateArray().Select(FormatElixirElement))}]";

            case JsonValueKind.Object:
                var entries = element.EnumerateObject()
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => $"{EscapeElixirString(property.Name)} => {FormatElixirElement(property.Value)}");
                return $"%{{{string.Join(", ", entries)}}}";

            case JsonValueKind.String:
                return EscapeElixirString(element.GetString() ?? string.Empty);

            case JsonValueKind.Number:
                return element.GetRawText();

            case JsonValueKind.True:
                return "true";

            case JsonValueKind.False:
                return "false";

            default:
                return "nil";
        }
    }

    private static string EscapeElixirString(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '#':
                    builder.Append("\\#");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    // ── Names ────────────────────────────────────────────────────────────────

    private static Dictionary<string, string> AssignUniqueNames(
        IReadOnlyList<string> keys,
        Func<string, string> convert,
        Dictionary<string, string>? preConverted = null)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            if (result.ContainsKey(key))
            {
                continue;
            }

            var candidate = preConverted is not null ? preConverted[key] : convert(key);
            var name = candidate;
            var counter = 1;
            while (!used.Add(name))
            {
                counter++;
                name = string.Create(CultureInfo.InvariantCulture, $"{candidate}_{counter}");
            }

            result[key] = name;
        }

        return result;
    }

    private static string ToElixirFunctionName(string name)
    {
        var snake = ToSnakeCase(name);
        return s_reservedFunctionNames.Contains(snake) ? snake + "_" : snake;
    }

    private static string ToElixirLocalName(string name)
    {
        var snake = ToSnakeCase(name);
        if (s_reservedFunctionNames.Contains(snake) || s_reservedLocalNames.Contains(snake))
        {
            return snake + "_";
        }

        return snake;
    }

    private static string ToElixirAtomName(string name)
    {
        var snake = ToSnakeCase(name);
        return s_reservedFunctionNames.Contains(snake) ? snake + "_" : snake;
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "value";
        }

        var converted = JsonNamingPolicy.SnakeCaseLower.ConvertName(name);
        var builder = new StringBuilder(converted.Length);
        foreach (var ch in converted)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? char.ToLowerInvariant(ch) : '_');
        }

        var sanitized = builder.ToString();
        if (sanitized.Length == 0 || (!char.IsLetter(sanitized[0]) && sanitized[0] != '_'))
        {
            sanitized = "v_" + sanitized;
        }

        return sanitized;
    }

    internal static string SanitizeModuleSegment(string segment)
    {
        var builder = new StringBuilder(segment.Length);
        foreach (var ch in segment)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                builder.Append(ch);
            }
        }

        if (builder.Length == 0)
        {
            return "Value";
        }

        if (!char.IsUpper(builder[0]))
        {
            if (char.IsLetter(builder[0]))
            {
                builder[0] = char.ToUpperInvariant(builder[0]);
            }
            else
            {
                builder.Insert(0, 'V');
            }
        }

        return builder.ToString();
    }

    private static bool IsCancellationTokenTypeId(string? typeId) =>
        string.Equals(typeId, AtsConstants.CancellationToken, StringComparison.Ordinal)
        || (typeId?.EndsWith("/System.Threading.CancellationToken", StringComparison.Ordinal) ?? false);

    // ── Model ────────────────────────────────────────────────────────────────

    private sealed record GeneratedFile(string FileName, string Source);

    private sealed class ElixirModuleSource
    {
        public ElixirModuleSource(string moduleName, string source)
        {
            ModuleName = moduleName;
            Source = source;
            LineCount = source.Count(ch => ch == '\n');
        }

        public string ModuleName { get; }

        public string Source { get; }

        public int LineCount { get; }
    }

    private sealed class ElixirModel
    {
        public required AtsContext Context { get; init; }

        public required Dictionary<string, string> HandleModules { get; init; }

        public required Dictionary<string, string> DtoModules { get; init; }

        public required Dictionary<string, string> EnumModules { get; init; }

        public required Dictionary<string, AtsTypeInfo> HandleTypeInfos { get; init; }

        public required Dictionary<string, List<AtsCapabilityInfo>> CapabilitiesByTarget { get; init; }

        /// <summary>
        /// The ATS type ids whose Elixir module is hand written in <c>base.ex</c> or
        /// <c>aspire_runtime.ex</c>. The generator maps them but emits no module.
        /// </summary>
        public required HashSet<string> RuntimeProvidedTypeIds { get; init; }

        /// <summary>
        /// The keyword keys of every DTO, in property order. A capability that flattens its
        /// <c>options</c> argument uses them as its option names.
        /// </summary>
        public required Dictionary<string, List<(string Key, string WireName, string? Description)>> DtoPropertyKeys { get; init; }

        /// <summary>
        /// The Elixir modules that can be assigned to a handle type. It holds the module of the
        /// type and the module of every handle type that implements or extends it.
        /// </summary>
        public required Dictionary<string, List<string>> UnionExpansions { get; init; }

        public IReadOnlyList<string> ExpandUnionMember(string typeId) =>
            UnionExpansions.TryGetValue(typeId, out var modules)
                ? modules
                : HandleModules.TryGetValue(typeId, out var moduleName) ? [moduleName] : [];

        public static ElixirModel Build(AtsContext context)
        {
            var dtoTypeIds = new HashSet<string>(context.DtoTypes.Select(dto => dto.TypeId), StringComparer.Ordinal);
            var handleTypeIds = CollectHandleTypeIds(context, dtoTypeIds);

            var candidates = new List<ModuleNameCandidate>();
            foreach (var typeId in handleTypeIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                candidates.Add(ModuleNameCandidate.ForType(typeId, IsInterfaceType(context, typeId)));
            }

            foreach (var dto in context.DtoTypes.OrderBy(dto => dto.TypeId, StringComparer.Ordinal))
            {
                candidates.Add(ModuleNameCandidate.ForType(dto.TypeId, isInterface: false));
            }

            var assigned = AssignModuleNames(candidates);

            var enumCandidates = context.EnumTypes
                .OrderBy(enumType => enumType.TypeId, StringComparer.Ordinal)
                .Select(ModuleNameCandidate.ForEnum)
                .ToList();
            var assignedEnums = AssignModuleNames(enumCandidates);

            var handleModules = new Dictionary<string, string>(StringComparer.Ordinal);
            var dtoModules = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var typeId in handleTypeIds)
            {
                handleModules[typeId] = assigned[typeId];
            }

            foreach (var dto in context.DtoTypes)
            {
                dtoModules[dto.TypeId] = assigned[dto.TypeId];
            }

            var handleTypeInfos = new Dictionary<string, AtsTypeInfo>(StringComparer.Ordinal);
            foreach (var typeInfo in context.HandleTypes)
            {
                handleTypeInfos.TryAdd(typeInfo.AtsTypeId, typeInfo);
            }

            var runtimeProvided = new HashSet<string>(StringComparer.Ordinal);
            if (handleModules.ContainsKey(AtsConstants.ReferenceExpressionTypeId))
            {
                // base.ex holds the reference expression, because a guest builds one locally with
                // Aspire.ref/1 and the host also returns one as a handle.
                handleModules[AtsConstants.ReferenceExpressionTypeId] = ReferenceExpressionModule;
                runtimeProvided.Add(AtsConstants.ReferenceExpressionTypeId);
            }

            var dtoPropertyKeys = new Dictionary<string, List<(string, string, string?)>>(StringComparer.Ordinal);
            foreach (var dto in context.DtoTypes)
            {
                var properties = dto.Properties.ToList();
                var keys = AssignUniqueNames(properties.Select(property => property.Name).ToList(), ToSnakeCase);
                dtoPropertyKeys[dto.TypeId] = properties
                    .Select(property => (
                        keys[property.Name],
                        property.Name,
                        property.Documentation?.Summary ?? property.Description))
                    .ToList();
            }

            return new ElixirModel
            {
                Context = context,
                HandleModules = handleModules,
                DtoModules = dtoModules,
                EnumModules = assignedEnums,
                HandleTypeInfos = handleTypeInfos,
                CapabilitiesByTarget = GroupCapabilitiesByTarget(context.Capabilities, handleTypeIds),
                RuntimeProvidedTypeIds = runtimeProvided,
                DtoPropertyKeys = dtoPropertyKeys,
                UnionExpansions = BuildUnionExpansions(context, handleModules)
            };
        }

        /// <summary>
        /// Maps every handle type id to the modules a caller can pass for it.
        /// </summary>
        /// <remarks>
        /// An interface member of a union accepts every concrete wrapper struct that implements it.
        /// Two members of the same union often expand to the same module, so the caller of this
        /// map deduplicates the result.
        /// </remarks>
        private static Dictionary<string, List<string>> BuildUnionExpansions(
            AtsContext context,
            Dictionary<string, string> handleModules)
        {
            var expansions = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

            void Add(string typeId, string moduleName)
            {
                if (!expansions.TryGetValue(typeId, out var modules))
                {
                    modules = new SortedSet<string>(StringComparer.Ordinal);
                    expansions[typeId] = modules;
                }

                modules.Add(moduleName);
            }

            foreach (var (typeId, moduleName) in handleModules)
            {
                Add(typeId, moduleName);
            }

            foreach (var typeInfo in context.HandleTypes)
            {
                if (typeInfo.IsInterface || !handleModules.TryGetValue(typeInfo.AtsTypeId, out var concreteModule))
                {
                    continue;
                }

                foreach (var implemented in typeInfo.ImplementedInterfaces)
                {
                    if (handleModules.ContainsKey(implemented.TypeId))
                    {
                        Add(implemented.TypeId, concreteModule);
                    }
                }

                foreach (var baseType in typeInfo.BaseTypeHierarchy)
                {
                    if (handleModules.ContainsKey(baseType.TypeId))
                    {
                        Add(baseType.TypeId, concreteModule);
                    }
                }
            }

            return expansions.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToList(),
                StringComparer.Ordinal);
        }

        private static bool IsInterfaceType(AtsContext context, string typeId)
        {
            foreach (var typeInfo in context.HandleTypes)
            {
                if (string.Equals(typeInfo.AtsTypeId, typeId, StringComparison.Ordinal))
                {
                    return typeInfo.IsInterface;
                }
            }

            foreach (var capability in context.Capabilities)
            {
                if (capability.TargetType is { } target
                    && string.Equals(target.TypeId, typeId, StringComparison.Ordinal))
                {
                    return target.IsInterface;
                }
            }

            return false;
        }

        private static HashSet<string> CollectHandleTypeIds(AtsContext context, HashSet<string> dtoTypeIds)
        {
            var handleTypeIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var typeInfo in context.HandleTypes)
            {
                Add(typeInfo.AtsTypeId, isHandle: true);
            }

            foreach (var capability in context.Capabilities)
            {
                AddTypeRef(capability.TargetType);
                AddTypeRef(capability.ReturnType);

                foreach (var parameter in capability.Parameters)
                {
                    AddTypeRef(parameter.Type);
                    AddTypeRef(parameter.CallbackReturnType);

                    if (parameter.CallbackParameters is { } callbackParameters)
                    {
                        foreach (var callbackParameter in callbackParameters)
                        {
                            AddTypeRef(callbackParameter.Type);
                        }
                    }
                }

                foreach (var expanded in capability.ExpandedTargetTypes)
                {
                    AddTypeRef(expanded);
                }
            }

            return handleTypeIds;

            void AddTypeRef(AtsTypeRef? typeRef)
            {
                if (typeRef is null)
                {
                    return;
                }

                AddTypeRef(typeRef.ElementType);
                AddTypeRef(typeRef.KeyType);
                AddTypeRef(typeRef.ValueType);

                if (typeRef.UnionTypes is { } unionTypes)
                {
                    foreach (var unionType in unionTypes)
                    {
                        AddTypeRef(unionType);
                    }
                }

                if (typeRef.Category == AtsTypeCategory.Handle)
                {
                    Add(typeRef.TypeId, isHandle: true);
                }
            }

            void Add(string typeId, bool isHandle)
            {
                if (!isHandle || dtoTypeIds.Contains(typeId) || IsCancellationTokenTypeId(typeId))
                {
                    return;
                }

                if (!typeId.Contains('/', StringComparison.Ordinal))
                {
                    return;
                }

                handleTypeIds.Add(typeId);
            }
        }

        private static Dictionary<string, List<AtsCapabilityInfo>> GroupCapabilitiesByTarget(
            IReadOnlyList<AtsCapabilityInfo> capabilities,
            HashSet<string> handleTypeIds)
        {
            var result = new Dictionary<string, List<AtsCapabilityInfo>>(StringComparer.Ordinal);
            var seen = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var capability in capabilities)
            {
                if (string.IsNullOrEmpty(capability.TargetTypeId))
                {
                    continue;
                }

                var targetTypes = capability.ExpandedTargetTypes.Count > 0
                    ? capability.ExpandedTargetTypes
                    : capability.TargetType is not null
                        ? [capability.TargetType]
                        : (IReadOnlyList<AtsTypeRef>)[];

                foreach (var targetType in targetTypes)
                {
                    if (targetType.TypeId is null || !handleTypeIds.Contains(targetType.TypeId))
                    {
                        continue;
                    }

                    if (!seen.TryGetValue(targetType.TypeId, out var capabilityIds))
                    {
                        capabilityIds = new HashSet<string>(StringComparer.Ordinal);
                        seen[targetType.TypeId] = capabilityIds;
                        result[targetType.TypeId] = new List<AtsCapabilityInfo>();
                    }

                    if (capabilityIds.Add(capability.CapabilityId))
                    {
                        result[targetType.TypeId].Add(capability);
                    }
                }
            }

            foreach (var list in result.Values)
            {
                list.Sort((left, right) => string.CompareOrdinal(left.CapabilityId, right.CapabilityId));
            }

            return result;
        }

        private static Dictionary<string, string> AssignModuleNames(IReadOnlyList<ModuleNameCandidate> candidates)
        {
            var assigned = new Dictionary<string, string>(StringComparer.Ordinal);

            var groups = candidates
                .GroupBy(candidate => candidate.Prefix + "." + candidate.PreferredName, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                var members = group.ToList();
                foreach (var member in members)
                {
                    var name = members.Count == 1 ? member.PreferredName : member.FallbackName;
                    assigned[member.TypeId] = ResolveReservedModuleName(member.Prefix + "." + name);
                }
            }

            var collisions = assigned
                .GroupBy(pair => pair.Value, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();

            if (collisions.Count > 0)
            {
                var details = string.Join(
                    "; ",
                    collisions.Select(group => $"{group.Key} <- [{string.Join(", ", group.Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal))}]"));

                throw new InvalidOperationException(
                    "Elixir code generation cannot continue because two or more ATS types map to the same Elixir module name. " +
                    "Rename one of the types, or move it to another assembly. Collisions: " + details);
            }

            return assigned;
        }
    }

    private sealed record ModuleNameCandidate(string TypeId, string Prefix, string PreferredName, string FallbackName)
    {
        public static ModuleNameCandidate ForType(string typeId, bool isInterface)
        {
            var simpleName = SanitizeModuleSegment(ExtractSimpleTypeName(typeId));
            var preferred = isInterface ? StripLeadingInterfacePrefix(simpleName) : simpleName;
            return new ModuleNameCandidate(typeId, BuildPrefix(typeId), preferred, simpleName);
        }

        public static ModuleNameCandidate ForEnum(AtsEnumTypeInfo enumType)
        {
            // An enum type id has the form "enum:{FullTypeName}". It carries no assembly, so the
            // fallback name flattens the namespace instead of adding an assembly segment.
            var fullName = enumType.TypeId.StartsWith(AtsConstants.EnumPrefix, StringComparison.Ordinal)
                ? enumType.TypeId[AtsConstants.EnumPrefix.Length..]
                : enumType.TypeId;

            var preferred = SanitizeModuleSegment(ExtractSimpleName(fullName));
            var fallback = string.Concat(fullName.Split('.', '+').Select(SanitizeModuleSegment));

            return new ModuleNameCandidate(enumType.TypeId, "Aspire.Enums", preferred, fallback);
        }

        private static string StripLeadingInterfacePrefix(string name) =>
            name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]) ? name[1..] : name;

        private static string BuildPrefix(string typeId)
        {
            var slashIndex = typeId.IndexOf('/', StringComparison.Ordinal);
            var assembly = slashIndex >= 0 ? typeId[..slashIndex] : AtsConstants.AspireHostingAssembly;

            string remainder;
            if (string.Equals(assembly, AtsConstants.AspireHostingAssembly, StringComparison.Ordinal))
            {
                remainder = string.Empty;
            }
            else if (assembly.StartsWith("Aspire.Hosting.", StringComparison.Ordinal))
            {
                remainder = assembly["Aspire.Hosting.".Length..];
            }
            else if (assembly.StartsWith("Aspire.", StringComparison.Ordinal))
            {
                remainder = assembly["Aspire.".Length..];
            }
            else
            {
                remainder = assembly;
            }

            if (remainder.Length == 0)
            {
                return "Aspire";
            }

            return "Aspire." + string.Join('.', remainder.Split('.').Select(SanitizeModuleSegment));
        }

        private static string ExtractSimpleTypeName(string typeId)
        {
            var slashIndex = typeId.IndexOf('/', StringComparison.Ordinal);
            return ExtractSimpleName(slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId);
        }

        private static string ExtractSimpleName(string typeName)
        {
            var lastDot = typeName.LastIndexOf('.');
            var plusIndex = typeName.LastIndexOf('+');
            var delimiterIndex = Math.Max(lastDot, plusIndex);
            return delimiterIndex >= 0 ? typeName[(delimiterIndex + 1)..] : typeName;
        }
    }

    // ── Writer ───────────────────────────────────────────────────────────────

    private sealed class ElixirWriter
    {
        private readonly StringBuilder _builder = new();

        public int IndentLevel { get; private set; }

        public void Indent() => IndentLevel++;

        public void Outdent() => IndentLevel = Math.Max(0, IndentLevel - 1);

        public void WriteLine(string value = "")
        {
            if (value.Length == 0)
            {
                _builder.Append('\n');
                return;
            }

            _builder.Append(' ', IndentLevel * 2).Append(value).Append('\n');
        }

        public void WriteRaw(string value) => _builder.Append(value);

        /// <summary>
        /// Writes a <c>@doc</c> or <c>@moduledoc</c> heredoc. The text is escaped so that no content
        /// closes the heredoc or starts an interpolation.
        /// </summary>
        public void WriteDoc(string attribute, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            WriteLine($"@{attribute} \"\"\"");
            foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                var escaped = line
                    .Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("\"\"\"", "\\\"\\\"\\\"", StringComparison.Ordinal)
                    .Replace("#{", "\\#{", StringComparison.Ordinal)
                    .TrimEnd();

                WriteLine(escaped);
            }

            WriteLine("\"\"\"");
        }

        public string ToSource() => _builder.ToString();
    }
}
