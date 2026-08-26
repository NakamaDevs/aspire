// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Shared.CodeGeneration;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Dart;

/// <summary>
/// Generates a Dart SDK from the ATS (Aspire Type System) capability model.
/// </summary>
/// <remarks>
/// <para>
/// Every ATS handle type becomes one Dart class that extends <c>AspireObject</c>, so it carries the
/// host handle and the transport that produced it. Every capability that targets the type becomes an
/// asynchronous method of that class. A required parameter stays positional and an optional
/// parameter becomes a named parameter.
/// </para>
/// <para>
/// Dart has no module namespace, so every generated name shares one scope. <see cref="DartModel"/>
/// assigns the names and fails the generation when two ATS types cannot get a distinct one.
/// </para>
/// <para>
/// The generated classes stay small because the shared logic lives in <c>aspire_runtime.dart</c>,
/// <c>base.dart</c> and <c>transport.dart</c>. Those three files are copied without a change.
/// </para>
/// </remarks>
internal sealed class AtsDartCodeGenerator : ICodeGenerator
{
    /// <summary>
    /// The maximum number of lines a generated part file holds before the generator starts a new
    /// one. A class is never split across two files.
    /// </summary>
    private const int MaxGeneratedFileLines = 2000;

    private const string GeneratedFileBaseName = "aspire_generated";
    private const string EntryFileName = "aspire.dart";
    private const string RuntimeClass = "AspireRuntime";

    /// <summary>
    /// The class of a mutable .NET list. <c>aspire_runtime.dart</c> defines it.
    /// </summary>
    private const string ListClass = "AspireList";

    /// <summary>
    /// The class of a mutable .NET dictionary. <c>aspire_runtime.dart</c> defines it.
    /// </summary>
    private const string DictClass = "AspireDict";

    /// <summary>
    /// The Dart class of a reference expression. <c>aspire_runtime.dart</c> defines it, so the
    /// generator never emits a class for the ATS type. The hand written class holds the format, the
    /// value providers and <c>getValueAsync</c>, because a guest builds an expression with
    /// <c>ref</c> and the host also returns one as a handle.
    /// </summary>
    private const string ReferenceExpressionClass = "ReferenceExpression";

    /// <summary>
    /// Reserved words and built-in identifiers of Dart. A generated identifier that matches one of
    /// them gets a trailing underscore.
    /// </summary>
    private static readonly HashSet<string> s_dartKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "assert", "async", "augment", "await", "base", "break", "case", "catch",
        "class", "const", "continue", "covariant", "default", "deferred", "do", "dynamic", "else",
        "enum", "export", "extends", "extension", "external", "factory", "false", "final",
        "finally", "for", "function", "get", "hide", "if", "implements", "import", "in",
        "interface", "is", "late", "library", "mixin", "new", "null", "of", "on", "operator",
        "part", "required", "rethrow", "return", "sealed", "set", "show", "static", "super",
        "switch", "sync", "this", "throw", "true", "try", "type", "typedef", "var", "void", "when",
        "while", "with", "yield"
    };

    /// <summary>
    /// Names that a generated method must not take. <c>AspireObject</c> and <c>Object</c> already
    /// define them, so a method with one of these names would not compile.
    /// </summary>
    private static readonly HashSet<string> s_reservedMemberNames = new(StringComparer.Ordinal)
    {
        "handle", "transport", "hashCode", "runtimeType", "toString", "noSuchMethod"
    };

    /// <summary>
    /// Local names that a generated method body uses. A parameter that maps to one of them gets a
    /// trailing underscore, so it cannot shadow the body.
    /// </summary>
    private static readonly HashSet<string> s_reservedLocalNames = new(StringComparer.Ordinal)
    {
        "args", "result", "handle", "transport", "options"
    };

    /// <summary>
    /// Class names that <c>base.dart</c>, <c>transport.dart</c> and <c>aspire_runtime.dart</c>
    /// already define, plus the <c>dart:core</c> names a generated class would shadow. A generated
    /// class that took one of them gets the <c>Type</c> suffix.
    /// </summary>
    private static readonly HashSet<string> s_reservedClassNames = new(StringComparer.Ordinal)
    {
        // base.dart, transport.dart and aspire_runtime.dart.
        "AspireDict", "AspireError", "AspireErrorCodes", "AspireHandle", "AspireList",
        "AspireMarshal", "AspireObject", "AspireRuntime", "AspireTransport", "AspireWireValue",
        "CancellationToken", ReferenceExpressionClass,
        // dart:core names that a generated declaration would shadow inside the library.
        "Comparable", "DateTime", "Duration", "Enum", "Error", "Exception", "Function", "Future",
        "Iterable", "List", "Map", "Null", "Object", "Pattern", "Record", "RegExp", "Set",
        "Stopwatch", "Stream", "String", "StringBuffer", "Symbol", "Type", "Uri"
    };

    /// <inheritdoc />
    public string Language => "Dart";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(AtsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var model = DartModel.Build(context);

        var declarations = new List<DartDeclaration>();
        declarations.AddRange(GenerateCallbackTypedefs(model));
        declarations.AddRange(GenerateEnumClasses(model));
        declarations.AddRange(GenerateDtoClasses(model));
        declarations.AddRange(GenerateExportedValueClasses(model));
        declarations.AddRange(GenerateHandleClasses(model));

        var generatedFiles = ChunkDeclarations(declarations);

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["base.dart"] = GetEmbeddedResource("base.dart"),
            ["transport.dart"] = GetEmbeddedResource("transport.dart"),
            ["aspire_runtime.dart"] = GetEmbeddedResource("aspire_runtime.dart"),
            // The watcher is a standalone script. aspire.dart never imports it: the CLI runs it as
            // the parent process of the AppHost when watch mode is on.
            ["watch.dart"] = GetEmbeddedResource("watch.dart")
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
        var resourceName = $"Aspire.Hosting.CodeGeneration.Dart.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ── Files ────────────────────────────────────────────────────────────────

    private static List<GeneratedFile> ChunkDeclarations(List<DartDeclaration> declarations)
    {
        var files = new List<GeneratedFile>();
        var builder = new StringBuilder();
        var lines = 0;
        var index = 1;

        void Flush()
        {
            if (lines == 0)
            {
                return;
            }

            files.Add(new GeneratedFile(FileNameFor(index), PartHeader(index) + builder));
            builder = new StringBuilder();
            lines = 0;
            index++;
        }

        foreach (var declaration in declarations)
        {
            if (lines > 0 && lines + declaration.LineCount > MaxGeneratedFileLines)
            {
                Flush();
            }

            builder.Append(declaration.Source);
            lines += declaration.LineCount;
        }

        Flush();

        if (files.Count == 0)
        {
            // An empty context still needs the part file that the entry point names.
            files.Add(new GeneratedFile(FileNameFor(1), PartHeader(1)));
        }

        return files;
    }

    private static string FileNameFor(int index) =>
        index == 1
            ? $"{GeneratedFileBaseName}.dart"
            : string.Create(CultureInfo.InvariantCulture, $"{GeneratedFileBaseName}_{index}.dart");

    private static string PartHeader(int index) =>
        $"""
        // {FileNameFor(index)} - Generated Aspire declarations
        // GENERATED CODE - DO NOT EDIT

        part of '{EntryFileName}';

        """;

    private static string GenerateEntryFile(DartModel model, IReadOnlyList<string> generatedFileNames)
    {
        var writer = new DartWriter();

        writer.WriteLine("// aspire.dart - Aspire Dart SDK entry point");
        writer.WriteLine("// GENERATED CODE - DO NOT EDIT");
        writer.WriteLine("//");
        writer.WriteLine("// `apphost.dart` imports this file. It owns the generated parts and re-exports the");
        writer.WriteLine("// runtime files, so one import brings the whole SDK into scope.");
        writer.WriteLine();
        if (model.CallbackShapes.Count > 0)
        {
            // A generated typedef names FutureOr, so a callback can be synchronous or
            // asynchronous. The import is left out when the model holds no callback, because the
            // analyzer reports an unused import.
            writer.WriteLine("import 'dart:async';");
        }

        writer.WriteLine("import 'dart:io';");
        writer.WriteLine();
        writer.WriteLine("import 'base.dart';");
        writer.WriteLine("import 'transport.dart';");
        writer.WriteLine("import 'aspire_runtime.dart';");
        writer.WriteLine();
        writer.WriteLine("export 'base.dart';");
        writer.WriteLine("export 'transport.dart';");
        writer.WriteLine("export 'aspire_runtime.dart';");
        writer.WriteLine();

        foreach (var fileName in generatedFileNames)
        {
            writer.WriteLine($"part '{fileName}';");
        }

        writer.WriteLine();

        var hasBuilderClass = model.HandleClasses.TryGetValue(AtsConstants.BuilderTypeId, out var builderClass);
        var returnType = hasBuilderClass ? builderClass! : "AspireHandle";

        WriteDoc(writer, $"""
            Creates a distributed application builder.

            The function connects to the AppHost socket in `REMOTE_APP_HOST_SOCKET_PATH` and
            authenticates with `ASPIRE_REMOTE_APPHOST_TOKEN`. The Aspire CLI sets both variables.

            Pass [transport] to reuse a connection that [AspireTransport.connect] already made.
            """);
        writer.WriteLine($"Future<{returnType}> createBuilder(");
        writer.Indent();
        writer.WriteLine("List<String> args, {");
        writer.WriteLine("AspireTransport? transport,");
        writer.Outdent();
        writer.WriteLine("}) async {");
        writer.Indent();
        writer.WriteLine("final AspireTransport connection =");
        writer.Indent();
        writer.WriteLine("transport ?? await AspireTransport.connect();");
        writer.Outdent();
        writer.WriteLine("final Map<String, Object?> options = <String, Object?>{'Args': args};");
        writer.WriteLine();
        writer.WriteLine("// ASPIRE_PROJECT_DIRECTORY is set by the CLI so the host reports the project");
        writer.WriteLine("// directory, not the working directory, when it matches --apphost <directory>.");
        writer.WriteLine("final String projectDirectory =");
        writer.Indent();
        writer.WriteLine("Platform.environment['ASPIRE_PROJECT_DIRECTORY'] ?? '';");
        writer.Outdent();
        writer.WriteLine("options['ProjectDirectory'] =");
        writer.Indent();
        writer.WriteLine("projectDirectory.isEmpty ? Directory.current.path : projectDirectory;");
        writer.Outdent();
        writer.WriteLine();
        writer.WriteLine("// ASPIRE_APPHOST_FILEPATH is set by the CLI so the host reports apphost.dart");
        writer.WriteLine("// instead of the entry assembly name.");
        writer.WriteLine("final String appHostFilePath =");
        writer.Indent();
        writer.WriteLine("Platform.environment['ASPIRE_APPHOST_FILEPATH'] ?? '';");
        writer.Outdent();
        writer.WriteLine("if (appHostFilePath.isNotEmpty) {");
        writer.Indent();
        writer.WriteLine("options['AppHostFilePath'] = appHostFilePath;");
        writer.Outdent();
        writer.WriteLine("}");
        writer.WriteLine();
        writer.WriteLine("final Object? result = await connection.invokeCapability(");
        writer.Indent();
        writer.WriteLine($"'{AtsConstants.CreateBuilderCapability}',");
        writer.WriteLine("<String, Object?>{'argsOrOptions': options},");
        writer.Outdent();
        writer.WriteLine(");");
        writer.WriteLine("final AspireHandle handle =");
        writer.Indent();
        writer.WriteLine($"{RuntimeClass}.requireHandle(result, '{AtsConstants.CreateBuilderCapability}');");
        writer.Outdent();
        writer.WriteLine(hasBuilderClass
            ? $"return {builderClass}(handle, connection);"
            : "return handle;");
        writer.Outdent();
        writer.WriteLine("}");

        return writer.ToSource();
    }

    // ── Enums ────────────────────────────────────────────────────────────────

    private static List<DartDeclaration> GenerateEnumClasses(DartModel model)
    {
        var declarations = new List<DartDeclaration>();

        foreach (var enumType in model.Context.EnumTypes.OrderBy(type => type.TypeId, StringComparer.Ordinal))
        {
            if (!model.EnumClasses.TryGetValue(enumType.TypeId, out var className))
            {
                continue;
            }

            var members = enumType.Values.Count > 0
                ? enumType.Values
                : enumType.ValueInfos.Select(value => value.Name).ToList();

            if (members.Count == 0)
            {
                // Dart has no empty enum, so the type falls back to its wire form.
                continue;
            }

            var writer = new DartWriter();
            var memberNames = AssignUniqueNames(members, ToDartMemberName);

            var allowed = string.Join(", ", members.Select(member => memberNames[member]));

            writer.WriteLine();
            WriteDoc(writer, BuildDoc(
                enumType.Documentation,
                null,
                $"The `{enumType.Name}` enumeration.",
                sections: [new DocSection("## Values", members
                    .Select(member => (
                        Name: $"[{memberNames[member]}]",
                        Description: enumType.ValueInfos
                            .FirstOrDefault(value => string.Equals(value.Name, member, StringComparison.Ordinal))?
                            .Documentation?.Summary))
                    .ToList())]));
            writer.WriteLine($"enum {className} implements AspireWireValue {{");
            writer.Indent();

            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                var separator = i == members.Count - 1 ? ";" : ",";
                var documentation = enumType.ValueInfos
                    .FirstOrDefault(value => string.Equals(value.Name, member, StringComparison.Ordinal))?
                    .Documentation?.Summary;

                WriteDoc(writer, string.IsNullOrWhiteSpace(documentation)
                    ? $"The `{member}` value."
                    : ConvertAtsReferences(documentation.Trim()));
                writer.WriteLine($"{memberNames[member]}('{EscapeString(member)}'){separator}");
            }

            writer.WriteLine();
            writer.WriteLine($"const {className}(this.wireName);");
            writer.WriteLine();
            WriteDoc(writer, "The .NET member name that travels on the wire.");
            writer.WriteLine("final String wireName;");
            writer.WriteLine();
            WriteDoc(writer, "Returns the wire form of the value.");
            writer.WriteLine("@override");
            writer.WriteLine("String toWire() => wireName;");
            writer.WriteLine();
            WriteDoc(writer, $"""
                Returns the wire form of [value].

                [value] is a `{className}`, or a string that names one. Throws
                [ArgumentError] when nothing matches. The message lists every value.
                """);
            writer.WriteLine($"static String toWireOf(Object? value) {{");
            writer.Indent();
            writer.WriteLine($"if (value is {className}) {{");
            writer.Indent();
            writer.WriteLine("return value.wireName;");
            writer.Outdent();
            writer.WriteLine("}");
            writer.WriteLine($"final {className}? match = fromWire(value);");
            writer.WriteLine("if (match != null) {");
            writer.Indent();
            writer.WriteLine("return match.wireName;");
            writer.Outdent();
            writer.WriteLine("}");
            writer.WriteLine("throw ArgumentError.value(");
            writer.Indent();
            writer.WriteLine("value,");
            writer.WriteLine("'value',");
            writer.WriteLine($"'{className} does not accept it. It accepts: {EscapeString(allowed)}',");
            writer.Outdent();
            writer.WriteLine(");");
            writer.Outdent();
            writer.WriteLine("}");
            writer.WriteLine();
            WriteDoc(writer, "Returns the value that [wire] names, or null when no value matches.");
            writer.WriteLine($"static {className}? fromWire(Object? wire) {{");
            writer.Indent();
            writer.WriteLine("for (final value in values) {");
            writer.Indent();
            writer.WriteLine("if (value.wireName == wire) {");
            writer.Indent();
            writer.WriteLine("return value;");
            writer.Outdent();
            writer.WriteLine("}");
            writer.Outdent();
            writer.WriteLine("}");
            writer.WriteLine("return null;");
            writer.Outdent();
            writer.WriteLine("}");
            writer.Outdent();
            writer.WriteLine("}");

            declarations.Add(DartDeclaration.From(writer));
        }

        return declarations;
    }

    // ── Data objects ─────────────────────────────────────────────────────────

    private static List<DartDeclaration> GenerateDtoClasses(DartModel model)
    {
        var declarations = new List<DartDeclaration>();

        foreach (var dto in model.Context.DtoTypes.OrderBy(type => type.TypeId, StringComparer.Ordinal))
        {
            if (!model.DtoClasses.TryGetValue(dto.TypeId, out var className))
            {
                continue;
            }

            var properties = model.DtoProperties[dto.TypeId];
            var writer = new DartWriter();

            writer.WriteLine();
            WriteDoc(writer, BuildDoc(
                dto.Documentation,
                dto.Description,
                $"The `{dto.Name}` data object.",
                sections: [new DocSection("## Properties", properties
                    .Select(property => (
                        Name: $"[{property.DartName}]",
                        Description: property.Documentation?.Summary ?? property.Description))
                    .ToList())]));
            writer.WriteLine($"class {className} implements AspireWireValue {{");
            writer.Indent();

            WriteDoc(writer, $"Builds a `{className}`.");
            if (properties.Count == 0)
            {
                writer.WriteLine($"const {className}();");
            }
            else
            {
                writer.WriteLine($"const {className}({{");
                writer.Indent();
                foreach (var property in properties)
                {
                    writer.WriteLine($"this.{property.DartName},");
                }
                writer.Outdent();
                writer.WriteLine("});");
            }

            writer.WriteLine();
            WriteDoc(writer, "Builds the data object from its wire form.");
            writer.WriteLine($"factory {className}.fromJson(Map<String, Object?> json) => {className}(");
            writer.Indent();
            foreach (var property in properties)
            {
                var decoded = DecodeExpression(
                    model,
                    property.Type,
                    $"json['{EscapeString(property.WireName)}']",
                    property.IsCallback,
                    forDataObject: true,
                    capabilityId: className + "." + property.DartName,
                    callbackType: property.CallbackType);
                writer.WriteLine($"{property.DartName}: {decoded},");
            }
            writer.Outdent();
            writer.WriteLine(");");

            writer.WriteLine();
            WriteDoc(writer, "Returns the data object that [wire] holds, or null when [wire] is not an object.");
            writer.WriteLine($"static {className}? fromWire(Object? wire) => wire is Map");
            writer.Indent();
            writer.WriteLine($"? {className}.fromJson({RuntimeClass}.asObject(wire))");
            writer.WriteLine(": null;");
            writer.Outdent();

            foreach (var property in properties)
            {
                writer.WriteLine();
                WriteDoc(writer, BuildDoc(
                    property.Documentation,
                    property.Description,
                    $"The `{property.WireName}` property."));
                writer.WriteLine($"final {property.DartType} {property.DartName};");
            }

            writer.WriteLine();
            WriteDoc(writer, """
                Returns the wire form of the data object.

                A property that is null is left out, so the host keeps its own default.
                """);
            writer.WriteLine("Map<String, Object?> toJson() {");
            writer.Indent();
            writer.WriteLine("final Map<String, Object?> json = <String, Object?>{};");
            foreach (var property in properties)
            {
                writer.WriteLine($"if ({property.DartName} != null) {{");
                writer.Indent();

                if (property.Shape is { } shape)
                {
                    // A public final field is never promoted, so the wrapper closes over a local
                    // that the null check already proved to be non-null. A data object carries no
                    // transport, so a handle argument goes through the default transport.
                    var local = property.DartName + "Callback";
                    writer.WriteLine($"final {shape.Name} {local} = {property.DartName}!;");
                    WriteCallbackWrapper(
                        model,
                        writer,
                        shape,
                        $"json['{EscapeString(property.WireName)}']",
                        local,
                        className + "." + property.DartName,
                        "AspireTransport.defaultInstance");
                }
                else
                {
                    var encoded = EncodeExpression(
                        model,
                        property.Type,
                        property.DartName,
                        property.IsCallback,
                        forDataObject: true,
                        nullableSource: true);
                    writer.WriteLine($"json['{EscapeString(property.WireName)}'] = {encoded};");
                }

                writer.Outdent();
                writer.WriteLine("}");
            }
            writer.WriteLine("return json;");
            writer.Outdent();
            writer.WriteLine("}");

            writer.WriteLine();
            WriteDoc(writer, "Returns the wire form of the data object. See [toJson].");
            writer.WriteLine("@override");
            writer.WriteLine("Object? toWire() => toJson();");

            writer.Outdent();
            writer.WriteLine("}");

            declarations.Add(DartDeclaration.From(writer));
        }

        return declarations;
    }

    // ── Exported values ──────────────────────────────────────────────────────

    private static List<DartDeclaration> GenerateExportedValueClasses(DartModel model)
    {
        var declarations = new List<DartDeclaration>();

        var groups = model.Context.ExportedValues
            .Where(value => value.PathSegments.Count > 1)
            .GroupBy(
                value => string.Join(".", value.PathSegments.Take(value.PathSegments.Count - 1)),
                StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            if (!model.ValueClasses.TryGetValue(group.Key, out var className))
            {
                continue;
            }

            var values = group.OrderBy(value => value.PathSegments[^1], StringComparer.Ordinal).ToList();
            var memberNames = AssignUniqueNames(
                values.Select(value => value.PathSegments[^1]).ToList(),
                ToDartMemberName);

            var writer = new DartWriter();

            writer.WriteLine();
            WriteDoc(writer, $"""
                The exported `{group.Key}` values.

                The values are snapped when the SDK is generated.
                """);
            writer.WriteLine($"abstract final class {className} {{");
            writer.Indent();

            var first = true;
            foreach (var value in values)
            {
                var memberName = memberNames[value.PathSegments[^1]];
                var (dartType, expression) = ExportedValueLiteral(model, value);

                if (!first)
                {
                    writer.WriteLine();
                }

                first = false;
                WriteDoc(writer, BuildDoc(
                    value.Documentation,
                    value.Description,
                    $"The exported `{string.Join(".", value.PathSegments)}` value."));
                writer.WriteLine($"static {dartType} get {memberName} => {expression};");
            }

            writer.Outdent();
            writer.WriteLine("}");

            declarations.Add(DartDeclaration.From(writer));
        }

        return declarations;
    }

    /// <summary>
    /// Renders an exported value as a Dart expression.
    /// </summary>
    /// <remarks>
    /// The value is known when the SDK is generated, so the expression is a literal. The generator
    /// falls back to <c>Object?</c> when the literal shape does not fit the declared ATS type,
    /// because a mismatched literal would not compile.
    /// </remarks>
    private static (string DartType, string Expression) ExportedValueLiteral(DartModel model, AtsExportedValueInfo value)
    {
        var literal = DartLiteral(value.Value);
        var typeRef = value.Type;

        if (typeRef is not null)
        {
            switch (typeRef.Category)
            {
                case AtsTypeCategory.Primitive when value.Value is JsonValue primitive:
                    var primitiveType = MapPrimitiveType(typeRef.TypeId);
                    if ((primitiveType == "String" && primitive.TryGetValue<string>(out _))
                        || (primitiveType == "num" && primitive.TryGetValue<double>(out _))
                        || (primitiveType == "bool" && primitive.TryGetValue<bool>(out _)))
                    {
                        return (primitiveType, literal);
                    }
                    break;

                case AtsTypeCategory.Dto when value.Value is JsonObject
                    && model.DtoClasses.TryGetValue(typeRef.TypeId, out var dtoClass):
                    return ($"{dtoClass}?", $"{dtoClass}.fromWire({literal})");

                case AtsTypeCategory.Enum when value.Value is JsonValue enumValue
                    && enumValue.TryGetValue<string>(out _)
                    && model.EnumClasses.TryGetValue(typeRef.TypeId, out var enumClass):
                    return ($"{enumClass}?", $"{enumClass}.fromWire({literal})");

                case AtsTypeCategory.Array or AtsTypeCategory.List when value.Value is JsonArray:
                    var elementType = MapElementType(model, typeRef.ElementType, forDataObject: true);
                    var element = DecodeExpression(
                        model,
                        typeRef.ElementType,
                        "item",
                        isCallback: false,
                        forDataObject: true,
                        capabilityId: "exported value");
                    return (
                        $"List<{elementType}>",
                        $"{RuntimeClass}.asList<{elementType}>({literal}, (Object? item) => {element})");

                case AtsTypeCategory.Dict when value.Value is JsonObject:
                    var valueType = MapElementType(model, typeRef.ValueType, forDataObject: true);
                    var entry = DecodeExpression(
                        model,
                        typeRef.ValueType,
                        "item",
                        isCallback: false,
                        forDataObject: true,
                        capabilityId: "exported value");
                    return (
                        $"Map<String, {valueType}>",
                        $"{RuntimeClass}.asMap<{valueType}>({literal}, (Object? item) => {entry})");
            }
        }

        return ("Object?", literal);
    }

    private static string DartLiteral(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return "null";

            case JsonArray array:
                var items = array.Select(DartLiteral).ToList();
                return items.Count == 0
                    ? "const <Object?>[]"
                    : "const <Object?>[" + string.Join(", ", items) + "]";

            case JsonObject jsonObject:
                var entries = jsonObject
                    .Select(pair => $"'{EscapeString(pair.Key)}': {DartLiteral(pair.Value)}")
                    .ToList();
                return entries.Count == 0
                    ? "const <String, Object?>{}"
                    : "const <String, Object?>{" + string.Join(", ", entries) + "}";

            case JsonValue jsonValue:
                if (jsonValue.TryGetValue<bool>(out var boolean))
                {
                    return boolean ? "true" : "false";
                }
                if (jsonValue.TryGetValue<string>(out var text))
                {
                    return $"'{EscapeString(text)}'";
                }
                return jsonValue.ToJsonString();

            default:
                return "null";
        }
    }

    // ── Handle classes ───────────────────────────────────────────────────────

    private static List<DartDeclaration> GenerateHandleClasses(DartModel model)
    {
        var declarations = new List<DartDeclaration>();

        foreach (var (typeId, className) in model.HandleClasses.OrderBy(pair => pair.Value, StringComparer.Ordinal))
        {
            if (model.RuntimeProvidedTypeIds.Contains(typeId))
            {
                // aspire_runtime.dart already defines the class. See ReferenceExpressionClass.
                continue;
            }

            var writer = new DartWriter();
            var typeInfo = model.HandleTypeInfos.GetValueOrDefault(typeId);

            writer.WriteLine();
            WriteDoc(writer, BuildDoc(
                typeInfo?.Documentation,
                null,
                $"A handle to the `{typeId}` object in the AppHost."));
            writer.WriteLine($"class {className} extends AspireObject {{");
            writer.Indent();
            WriteDoc(writer, $"Wraps the handle of a `{typeId}` object.");
            writer.WriteLine($"const {className}(super.handle, super.transport);");

            var capabilities = model.CapabilitiesByTarget.GetValueOrDefault(typeId) ?? [];
            var methodNames = AssignUniqueNames(
                capabilities.Select(capability => capability.CapabilityId).ToList(),
                capabilityId => ToDartMethodName(
                    capabilities.First(capability =>
                        string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal)).MethodName));

            foreach (var capability in capabilities)
            {
                GenerateCapability(model, writer, className, typeId, capability, methodNames[capability.CapabilityId]);
            }

            writer.Outdent();
            writer.WriteLine("}");

            declarations.Add(DartDeclaration.From(writer));
        }

        return declarations;
    }

    private static void GenerateCapability(
        DartModel model,
        DartWriter writer,
        string className,
        string classTypeId,
        AtsCapabilityInfo capability,
        string methodName)
    {
        var targetParameterName = capability.TargetParameterName ?? "builder";
        var parameters = capability.Parameters
            .Where(parameter => !string.Equals(parameter.Name, targetParameterName, StringComparison.Ordinal))
            .ToList();

        // A cancellation token is always a named parameter, even when the .NET parameter has no
        // default, so the caller can stop the call with CancellationToken.cancel. A property setter
        // keeps its `value` parameter required, because that value is the property itself.
        bool AlwaysOptional(AtsParameterInfo parameter) =>
            IsCancellationTokenParameter(parameter)
            && capability.CapabilityKind != AtsCapabilityKind.PropertySetter;

        var required = parameters
            .Where(parameter => !parameter.IsOptional && !AlwaysOptional(parameter))
            .ToList();
        var optional = parameters
            .Where(parameter => parameter.IsOptional || AlwaysOptional(parameter))
            .ToList();

        // One optional DTO named `options` flattens into named parameters, so the caller writes the
        // properties of the DTO directly. Dart renders a cancellation token as its own named
        // parameter, so a token beside the DTO does not block the flattening.
        var flattened = AtsOptionsFlattening.TryGetDirectOptionsParameter(
            optional,
            IsCancellationTokenParameter,
            cancellationTokenIsSeparateParameter: true,
            out var directOptions)
            && model.DtoProperties.ContainsKey(directOptions.Type!.TypeId);

        var flattenedProperties = flattened
            ? model.DtoProperties[directOptions!.Type!.TypeId]
            : [];

        // Every parameter name, flattened or not, shares one Dart scope, so they are assigned
        // together.
        var nameKeys = new List<string>();
        nameKeys.AddRange(required.Select(parameter => "p:" + parameter.Name));
        foreach (var parameter in optional)
        {
            if (flattened && ReferenceEquals(parameter, directOptions))
            {
                continue;
            }
            nameKeys.Add("p:" + parameter.Name);
        }
        nameKeys.AddRange(flattenedProperties.Select(property => "o:" + property.WireName));

        var localNames = AssignUniqueNames(nameKeys, key => ToDartLocalName(key[2..]));

        var namedOptional = optional
            .Where(parameter => !(flattened && ReferenceEquals(parameter, directOptions)))
            .ToList();

        var returnsReceiver = ReturnsReceiver(capability, classTypeId);
        var returnType = returnsReceiver
            ? className
            : MapReturnType(model, capability.ReturnType);

        writer.WriteLine();
        WriteCapabilityDoc(writer, capability, required, namedOptional, flattenedProperties, localNames);

        if (capability.IsObsolete)
        {
            var message = Flatten(capability.ObsoleteMessage ?? "This capability is obsolete.");
            writer.WriteLine($"@Deprecated('{EscapeString(message)}')");
        }

        // Signature.
        var signature = new StringBuilder();
        signature.Append(CultureInfo.InvariantCulture, $"Future<{returnType}> {methodName}(");
        signature.Append(string.Join(
            ", ",
            required.Select(parameter =>
                $"{MapParameterType(model, parameter, isOptional: parameter.IsNullable)} {localNames["p:" + parameter.Name]}")));

        var hasNamed = namedOptional.Count > 0 || flattenedProperties.Count > 0;
        if (hasNamed)
        {
            if (required.Count > 0)
            {
                signature.Append(", ");
            }

            var named = new List<string>();
            named.AddRange(namedOptional.Select(parameter =>
                $"{MapParameterType(model, parameter, isOptional: true)} {localNames["p:" + parameter.Name]}"));
            named.AddRange(flattenedProperties.Select(property =>
                $"{property.DartType} {localNames["o:" + property.WireName]}"));

            signature.Append('{').Append(string.Join(", ", named)).Append('}');
        }

        signature.Append(") async {");
        writer.WriteLine(signature.ToString());
        writer.Indent();

        writer.WriteLine($"final Map<String, Object?> args = <String, Object?>{{'{EscapeString(targetParameterName)}': handle}};");

        foreach (var parameter in required)
        {
            var local = localNames["p:" + parameter.Name];
            var target = $"args['{EscapeString(parameter.Name)}']";

            if (model.ShapeOf(parameter) is { } shape && !parameter.IsNullable)
            {
                WriteCallbackWrapper(model, writer, shape, target, local, capability.CapabilityId, "transport");
                continue;
            }

            var encoded = EncodeExpression(
                model,
                parameter.Type,
                local,
                parameter.IsCallback,
                forDataObject: false,
                unionGuard: BuildUnionGuard(model, parameter, local, capability.CapabilityId));
            writer.WriteLine($"{target} = {encoded};");
        }

        foreach (var parameter in namedOptional)
        {
            var local = localNames["p:" + parameter.Name];
            var target = $"args['{EscapeString(parameter.Name)}']";
            writer.WriteLine($"if ({local} != null) {{");
            writer.Indent();

            if (model.ShapeOf(parameter) is { } shape)
            {
                // A closure never promotes a captured variable, so the wrapper closes over a local
                // that the null check already proved to be non-null.
                var callbackLocal = local + "Callback";
                writer.WriteLine($"final {shape.Name} {callbackLocal} = {local};");
                WriteCallbackWrapper(model, writer, shape, target, callbackLocal, capability.CapabilityId, "transport");
            }
            else
            {
                var encoded = EncodeExpression(
                    model,
                    parameter.Type,
                    local,
                    parameter.IsCallback,
                    forDataObject: false,
                    unionGuard: BuildUnionGuard(model, parameter, local, capability.CapabilityId));
                writer.WriteLine($"{target} = {encoded};");
            }

            writer.Outdent();
            writer.WriteLine("}");
        }

        if (flattenedProperties.Count > 0)
        {
            writer.WriteLine("final Map<String, Object?> options = <String, Object?>{};");
            foreach (var property in flattenedProperties)
            {
                var local = localNames["o:" + property.WireName];
                var target = $"options['{EscapeString(property.WireName)}']";
                writer.WriteLine($"if ({local} != null) {{");
                writer.Indent();

                if (property.Shape is { } shape)
                {
                    var callbackLocal = local + "Callback";
                    writer.WriteLine($"final {shape.Name} {callbackLocal} = {local};");
                    WriteCallbackWrapper(model, writer, shape, target, callbackLocal, capability.CapabilityId, "transport");
                }
                else
                {
                    var encoded = EncodeExpression(model, property.Type, local, property.IsCallback, forDataObject: true);
                    writer.WriteLine($"{target} = {encoded};");
                }

                writer.Outdent();
                writer.WriteLine("}");
            }
            writer.WriteLine("if (options.isNotEmpty) {");
            writer.Indent();
            writer.WriteLine($"args['{EscapeString(directOptions!.Name)}'] = options;");
            writer.Outdent();
            writer.WriteLine("}");
        }

        var isVoid = returnType == "void";
        var invocation = $"await transport.invokeCapability('{EscapeString(capability.CapabilityId)}', args)";

        if (isVoid)
        {
            writer.WriteLine($"{invocation};");
        }
        else
        {
            writer.WriteLine($"final Object? result = {invocation};");

            var decoded = returnsReceiver
                ? $"{className}({RuntimeClass}.requireHandle(result, '{EscapeString(capability.CapabilityId)}'), transport)"
                : DecodeExpression(
                    model,
                    capability.ReturnType,
                    "result",
                    isCallback: false,
                    forDataObject: false,
                    capabilityId: capability.CapabilityId);

            writer.WriteLine($"return {decoded};");
        }

        writer.Outdent();
        writer.WriteLine("}");
    }

    /// <summary>
    /// Returns true when a fluent capability keeps the receiver as its return type.
    /// </summary>
    /// <remarks>
    /// The declared .NET return type of a fluent capability is often an interface such as
    /// <c>IResourceWithEnvironment</c>. Decoding into that type would end a call chain on a class
    /// that carries no methods, so the receiver class wins. A factory method such as
    /// <c>AddDatabase</c> returns a different builder and keeps its own type.
    /// </remarks>
    private static bool ReturnsReceiver(AtsCapabilityInfo capability, string classTypeId) =>
        capability.ReturnsBuilder
        && capability.ReturnType?.TypeId is { Length: > 0 } returnTypeId
        && (string.Equals(returnTypeId, capability.TargetTypeId, StringComparison.Ordinal)
            || string.Equals(returnTypeId, classTypeId, StringComparison.Ordinal));

    // ── Callbacks ────────────────────────────────────────────────────────────

    /// <summary>
    /// Emits one <c>typedef</c> for every callback shape the model holds.
    /// </summary>
    /// <remarks>
    /// Two callbacks that take the same argument types and return the same type share one typedef,
    /// so the generated surface names a small set of function types instead of a bare
    /// <c>Function</c>.
    /// </remarks>
    private static List<DartDeclaration> GenerateCallbackTypedefs(DartModel model)
    {
        var declarations = new List<DartDeclaration>();

        foreach (var shape in model.CallbackShapes.Values.OrderBy(shape => shape.Name, StringComparer.Ordinal))
        {
            var writer = new DartWriter();
            var parameters = new List<string>();

            for (var i = 0; i < shape.Parameters.Count; i++)
            {
                parameters.Add($"{CallbackParameterType(model, shape.Parameters[i])} {shape.ParameterNames[i]}");
            }

            var entries = new List<(string Name, string? Description)>();
            for (var i = 0; i < shape.Parameters.Count; i++)
            {
                entries.Add((
                    $"[{shape.ParameterNames[i]}]",
                    shape.Parameters[i].Documentation?.Summary));
            }

            writer.WriteLine();
            WriteDoc(writer, BuildDoc(
                documentation: null,
                fallback: null,
                defaultSummary: shape.Parameters.Count == 0
                    ? "A callback that the AppHost invokes with no argument."
                    : "A callback that the AppHost invokes. Every argument arrives as its "
                        + "generated Dart type.",
                sections: [new DocSection("## Parameters", entries)],
                returns: shape.WriteBackDtoClass is { } writeBack
                    ? $"The changed `{writeBack}`, or null to keep the argument as it is."
                    : null));
            writer.WriteLine(
                $"typedef {shape.Name} = FutureOr<{shape.DartReturnType}> Function({string.Join(", ", parameters)});");

            declarations.Add(DartDeclaration.From(writer));
        }

        return declarations;
    }

    /// <summary>
    /// Returns the Dart type of one callback argument.
    /// </summary>
    private static string CallbackParameterType(DartModel model, AtsCallbackParameterInfo parameter) =>
        MapElementType(model, parameter.Type, forDataObject: false);

    /// <summary>
    /// Writes the closure that the transport registers for a callback argument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The host sends the arguments as <c>p0</c>, <c>p1</c>, … The closure decodes each one with
    /// the decoder of its ATS type, so a context handle becomes its wrapper class and a data object
    /// becomes its class. A field that the decoder cannot convert becomes null instead of failing
    /// the call.
    /// </para>
    /// <para>
    /// A generated data object never changes in place, so a callback that receives one returns the
    /// changed object. The closure then answers with the positional write-back map. A callback that
    /// returns null keeps the argument the host sent, which is the same rule the transport applies
    /// when a closure returns null.
    /// </para>
    /// </remarks>
    private static void WriteCallbackWrapper(
        DartModel model,
        DartWriter writer,
        DartCallbackShape shape,
        string assignTarget,
        string callbackExpression,
        string capabilityId,
        string transportExpression)
    {
        var arguments = Enumerable
            .Range(0, shape.Parameters.Count)
            .Select(index => string.Create(CultureInfo.InvariantCulture, $"a{index}"))
            .ToList();

        writer.WriteLine($"{assignTarget} = ({string.Join(", ", arguments.Select(name => $"Object? {name}"))}) async {{");
        writer.Indent();

        var locals = new List<string>();
        for (var i = 0; i < shape.Parameters.Count; i++)
        {
            var parameter = shape.Parameters[i];
            var local = string.Create(CultureInfo.InvariantCulture, $"p{i}");
            locals.Add(local);

            var decoded = DecodeExpression(
                model,
                parameter.Type,
                arguments[i],
                isCallback: false,
                forDataObject: false,
                capabilityId,
                transportExpression: transportExpression);

            writer.WriteLine($"final {CallbackParameterType(model, parameter)} {local} = {decoded};");
        }

        var call = $"{callbackExpression}({string.Join(", ", locals)})";

        if (shape.ReturnsValue)
        {
            writer.WriteLine($"return await {call};");
        }
        else if (shape.WriteBackIndexes.Count == 0)
        {
            writer.WriteLine($"await {call};");
            // The transport echoes the arguments the host sent when a closure returns null.
            writer.WriteLine("return null;");
        }
        else
        {
            writer.WriteLine($"final {shape.DartReturnType} changed = await {call};");
            writer.WriteLine("return <String, Object?>{");
            writer.Indent();
            foreach (var index in shape.WriteBackIndexes)
            {
                var dtoClass = model.DtoClasses[shape.Parameters[index].Type.TypeId];
                var replacement = shape.WriteBackIndexes.Count == 1
                    ? $"changed ?? {locals[index]}"
                    : $"changed is {dtoClass} ? changed : {locals[index]}";
                writer.WriteLine($"'p{index.ToString(CultureInfo.InvariantCulture)}': ({replacement})?.toJson(),");
            }
            writer.Outdent();
            writer.WriteLine("};");
        }

        writer.Outdent();
        writer.WriteLine("};");
    }

    /// <summary>
    /// Returns the guard expression of an <c>[AspireUnion]</c> argument, or null.
    /// </summary>
    /// <remarks>
    /// A union parameter is typed as <c>Object?</c>, so the guard is the only place that can reject
    /// a wrong argument before the host does. A union that accepts any type gets no guard.
    /// </remarks>
    private static string? BuildUnionGuard(
        DartModel model,
        AtsParameterInfo parameter,
        string local,
        string capabilityId)
    {
        if (parameter.Type is not { Category: AtsTypeCategory.Union } unionType
            || unionType.UnionTypes is not { Count: > 0 } members)
        {
            return null;
        }

        var accepted = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var member in members)
        {
            switch (member.Category)
            {
                case AtsTypeCategory.Handle:
                    foreach (var className in model.ExpandUnionMember(member.TypeId))
                    {
                        accepted.Add(className);
                    }
                    break;

                case AtsTypeCategory.Dto when model.DtoClasses.TryGetValue(member.TypeId, out var dtoClass):
                    accepted.Add(dtoClass);
                    break;

                case AtsTypeCategory.Enum when model.EnumClasses.TryGetValue(member.TypeId, out var enumClass):
                    accepted.Add(enumClass);
                    accepted.Add("String");
                    break;

                case AtsTypeCategory.Primitive:
                    var primitive = MapPrimitiveType(member.TypeId);
                    if (string.Equals(primitive, "Object?", StringComparison.Ordinal))
                    {
                        // The union accepts any value, so a guard would never reject one.
                        return null;
                    }
                    accepted.Add(primitive);
                    break;

                default:
                    return null;
            }
        }

        if (accepted.Count == 0)
        {
            return null;
        }

        var test = string.Join(" || ", accepted.Select(name => $"value is {name}"));

        return $"{RuntimeClass}.requireUnion({local}, (Object? value) => {test}, "
            + $"const <String>[{string.Join(", ", accepted.Select(name => $"'{EscapeString(name)}'"))}], "
            + $"'{EscapeString(capabilityId)}', '{EscapeString(parameter.Name)}')";
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private static string MapReturnType(DartModel model, AtsTypeRef? typeRef)
    {
        if (typeRef is null || string.Equals(typeRef.TypeId, AtsConstants.Void, StringComparison.Ordinal))
        {
            return "void";
        }

        return MapType(model, typeRef, forDataObject: false, isOptional: IsNullableValue(typeRef, forDataObject: false));
    }

    /// <summary>
    /// Returns false for a value that the generated decoder never leaves null.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A handle decodes through <c>AspireRuntime.requireHandle</c>, which throws when the host
    /// returned something else, so a wrapper class is never null. A handle whose
    /// <see cref="AtsTypeRef.IsNullable"/> is true is the exception: the host may omit it, so the
    /// generator emits a null tolerant decode and a nullable type.
    /// </para>
    /// <para>
    /// Every other value can be null, because the decoder returns null for a shape it cannot
    /// convert. A collection is the exception, because the decoder falls back to an empty one.
    /// </para>
    /// </remarks>
    private static bool IsNullableValue(AtsTypeRef? typeRef, bool forDataObject)
    {
        if (forDataObject || typeRef is null)
        {
            return true;
        }

        return typeRef.Category switch
        {
            // AspireRuntime.requireHandle throws instead of returning null.
            AtsTypeCategory.Handle => IsCancellationTokenTypeId(typeRef.TypeId) || typeRef.IsNullable == true,
            // AspireRuntime.asList and AspireRuntime.asMap fall back to an empty collection, and a
            // mutable collection decodes into an AspireList or an AspireDict.
            AtsTypeCategory.Array or AtsTypeCategory.List or AtsTypeCategory.Dict => false,
            _ => true
        };
    }

    private static string MapParameterType(DartModel model, AtsParameterInfo parameter, bool isOptional)
    {
        if (model.ShapeOf(parameter) is { } shape)
        {
            return isOptional ? shape.Name + "?" : shape.Name;
        }

        if (parameter.IsCallback)
        {
            return isOptional ? "Function?" : "Function";
        }

        return MapType(model, parameter.Type, forDataObject: false, isOptional: isOptional);
    }

    /// <summary>
    /// Maps an ATS type reference to a Dart type.
    /// </summary>
    /// <param name="model">The generation model.</param>
    /// <param name="typeRef">The type reference.</param>
    /// <param name="forDataObject">
    /// True when the type is a property of a data object. A handle inside a data object keeps its
    /// wire form, because the object carries no transport to build a wrapper with.
    /// </param>
    /// <param name="isOptional">True when the value can be null.</param>
    private static string MapType(DartModel model, AtsTypeRef? typeRef, bool forDataObject, bool isOptional)
    {
        if (typeRef is null)
        {
            return "Object?";
        }

        string baseType;

        switch (typeRef.Category)
        {
            case AtsTypeCategory.Primitive:
                baseType = MapPrimitiveType(typeRef.TypeId);
                if (string.Equals(baseType, "Object?", StringComparison.Ordinal))
                {
                    return "Object?";
                }
                break;

            case AtsTypeCategory.Enum:
                baseType = model.EnumClasses.TryGetValue(typeRef.TypeId, out var enumClass) ? enumClass : "String";
                break;

            case AtsTypeCategory.Handle:
                if (IsCancellationTokenTypeId(typeRef.TypeId))
                {
                    baseType = "CancellationToken";
                    break;
                }
                if (forDataObject)
                {
                    baseType = "AspireHandle";
                    break;
                }
                baseType = model.HandleClasses.TryGetValue(typeRef.TypeId, out var handleClass)
                    ? handleClass
                    : "AspireHandle";
                break;

            case AtsTypeCategory.Dto:
                baseType = model.DtoClasses.TryGetValue(typeRef.TypeId, out var dtoClass)
                    ? dtoClass
                    : "Map<String, Object?>";
                break;

            case AtsTypeCategory.Callback:
                baseType = "Function";
                break;

            case AtsTypeCategory.Array:
                baseType = $"List<{MapElementType(model, typeRef.ElementType, forDataObject)}>";
                break;

            case AtsTypeCategory.List:
                baseType = typeRef.IsReadOnly || forDataObject
                    ? $"List<{MapElementType(model, typeRef.ElementType, forDataObject)}>"
                    : $"{ListClass}<{MapElementType(model, typeRef.ElementType, forDataObject)}>";
                break;

            case AtsTypeCategory.Dict:
                baseType = typeRef.IsReadOnly || forDataObject
                    ? $"Map<String, {MapElementType(model, typeRef.ValueType, forDataObject)}>"
                    : $"{DictClass}<{MapElementType(model, typeRef.ValueType, forDataObject)}>";
                break;

            default:
                return "Object?";
        }

        return isOptional ? MakeNullable(baseType) : baseType;
    }

    /// <summary>
    /// Makes a Dart type nullable. A type that already accepts null does not change.
    /// </summary>
    private static string MakeNullable(string dartType) =>
        dartType.EndsWith('?') ? dartType : dartType + "?";

    private static string MapElementType(DartModel model, AtsTypeRef? typeRef, bool forDataObject) =>
        MapType(model, typeRef, forDataObject, isOptional: IsNullableValue(typeRef, forDataObject));

    private static string MapPrimitiveType(string typeId) => typeId switch
    {
        AtsConstants.String or AtsConstants.Char or AtsConstants.Guid or AtsConstants.Uri => "String",
        AtsConstants.Number or AtsConstants.TimeSpan => "num",
        AtsConstants.Boolean => "bool",
        AtsConstants.DateTime or AtsConstants.DateTimeOffset
            or AtsConstants.DateOnly or AtsConstants.TimeOnly => "DateTime",
        AtsConstants.CancellationToken => "CancellationToken",
        _ => "Object?"
    };

    // ── Encoding and decoding ────────────────────────────────────────────────

    /// <summary>
    /// Returns the Dart expression that turns <paramref name="source"/> into its wire form.
    /// </summary>
    /// <remarks>
    /// The expression runs where <paramref name="source"/> is known to be non-null, so a nullable
    /// value is already inside an <c>if</c>.
    /// </remarks>
    private static string EncodeExpression(
        DartModel model,
        AtsTypeRef? typeRef,
        string source,
        bool isCallback,
        bool forDataObject,
        bool nullableSource = false,
        string? unionGuard = null)
    {
        // A union parameter is typed as Object?, so the guard is the only place that can reject a
        // wrong argument before the host does.
        if (unionGuard is not null)
        {
            return unionGuard;
        }

        // AspireTransport.request walks the arguments and registers every function it finds, so a
        // callback travels as the function itself.
        if (isCallback || typeRef is null || typeRef.Category == AtsTypeCategory.Callback)
        {
            return source;
        }

        // A public final field is never promoted to a non-null type, so a data object reads its own
        // properties through `?.`. The call sits inside a null check, so the wire value is never
        // null at run time.
        // A public final field is never promoted to a non-null type, so a data object reads its
        // own properties through `!`. The read sits inside a null check.
        var access = nullableSource ? "!." : ".";
        var iterable = nullableSource ? source + "!" : source;

        switch (typeRef.Category)
        {
            case AtsTypeCategory.Enum when model.EnumClasses.TryGetValue(typeRef.TypeId, out var enumClass):
                // The static form validates, so a value that reached the call as Object? cannot
                // travel as an unknown wire name.
                return $"{enumClass}.toWireOf({source})";

            case AtsTypeCategory.Dto when model.DtoClasses.ContainsKey(typeRef.TypeId):
                return $"{source}{access}toJson()";

            // aspire_runtime.dart owns the reference expression, because a guest builds one with
            // ref() and the host also returns one as a handle. Only the host form carries a handle,
            // so the value travels through AspireMarshal.encode instead.
            case AtsTypeCategory.Handle when model.RuntimeProvidedTypeIds.Contains(typeRef.TypeId):
                return source;

            case AtsTypeCategory.Handle when !forDataObject
                && !IsCancellationTokenTypeId(typeRef.TypeId)
                && model.HandleClasses.ContainsKey(typeRef.TypeId):
                return $"{source}{access}handle";

            case AtsTypeCategory.List when !typeRef.IsReadOnly && !forDataObject:
            case AtsTypeCategory.Dict when !typeRef.IsReadOnly && !forDataObject:
                return $"{source}{access}handle";

            case AtsTypeCategory.Array:
            case AtsTypeCategory.List:
                var element = EncodeExpression(
                    model,
                    typeRef.ElementType,
                    "item",
                    isCallback: false,
                    forDataObject,
                    nullableSource: IsNullableValue(typeRef.ElementType, forDataObject));

                return string.Equals(element, "item", StringComparison.Ordinal)
                    ? source
                    : $"<Object?>[for (final item in {iterable}) {element}]";

            default:
                return source;
        }
    }

    /// <summary>
    /// Returns the Dart expression that turns <paramref name="source"/> into a Dart value.
    /// </summary>
    private static string DecodeExpression(
        DartModel model,
        AtsTypeRef? typeRef,
        string source,
        bool isCallback,
        bool forDataObject,
        string capabilityId,
        string? callbackType = null,
        string transportExpression = "transport")
    {
        if (isCallback || typeRef?.Category == AtsTypeCategory.Callback)
        {
            // The host sends a callback identifier, which the guest cannot invoke, so a property
            // that arrives from the host stays null. A guest function survives a round trip.
            return $"{RuntimeClass}.asCallback<{callbackType ?? "Function"}>({source})";
        }

        if (typeRef is null)
        {
            return source;
        }

        switch (typeRef.Category)
        {
            case AtsTypeCategory.Primitive:
                return MapPrimitiveType(typeRef.TypeId) switch
                {
                    "String" => $"{RuntimeClass}.asString({source})",
                    "num" => $"{RuntimeClass}.asNum({source})",
                    "bool" => $"{RuntimeClass}.asBool({source})",
                    "DateTime" => $"{RuntimeClass}.asDateTime({source})",
                    "CancellationToken" => $"{RuntimeClass}.asCancellationToken({source})",
                    _ => source
                };

            case AtsTypeCategory.Enum when model.EnumClasses.TryGetValue(typeRef.TypeId, out var enumClass):
                return $"{enumClass}.fromWire({source})";

            case AtsTypeCategory.Dto when model.DtoClasses.TryGetValue(typeRef.TypeId, out var dtoClass):
                return $"{dtoClass}.fromWire({source})";

            case AtsTypeCategory.Handle when IsCancellationTokenTypeId(typeRef.TypeId):
                return $"{RuntimeClass}.asCancellationToken({source})";

            case AtsTypeCategory.Handle when forDataObject:
                return $"{source} is AspireHandle ? {source} as AspireHandle : null";

            case AtsTypeCategory.Handle when model.HandleClasses.TryGetValue(typeRef.TypeId, out var handleClass):
                var wrapped = $"{handleClass}({RuntimeClass}.requireHandle({source}, '{EscapeString(capabilityId)}'), {transportExpression})";
                // requireHandle throws on anything else, so a type the host may omit needs the
                // null check first.
                return typeRef.IsNullable == true ? $"({source} == null ? null : {wrapped})" : wrapped;

            case AtsTypeCategory.Array:
            case AtsTypeCategory.List when typeRef.IsReadOnly || forDataObject:
                var elementType = MapElementType(model, typeRef.ElementType, forDataObject);
                var element = DecodeExpression(
                    model, typeRef.ElementType, "item", isCallback: false, forDataObject, capabilityId,
                    transportExpression: transportExpression);
                return $"{RuntimeClass}.asList<{elementType}>({source}, (Object? item) => {element})";

            case AtsTypeCategory.List:
                var listElementType = MapElementType(model, typeRef.ElementType, forDataObject);
                var listElement = DecodeExpression(
                    model, typeRef.ElementType, "item", isCallback: false, forDataObject, capabilityId,
                    transportExpression: transportExpression);
                return $"{ListClass}<{listElementType}>({RuntimeClass}.requireHandle({source}, '{EscapeString(capabilityId)}'), {transportExpression}, "
                    + $"(Object? item) => {listElement})";

            case AtsTypeCategory.Dict when typeRef.IsReadOnly || forDataObject:
                var valueType = MapElementType(model, typeRef.ValueType, forDataObject);
                var value = DecodeExpression(
                    model, typeRef.ValueType, "item", isCallback: false, forDataObject, capabilityId,
                    transportExpression: transportExpression);
                return $"{RuntimeClass}.asMap<{valueType}>({source}, (Object? item) => {value})";

            case AtsTypeCategory.Dict:
                var dictValueType = MapElementType(model, typeRef.ValueType, forDataObject);
                var dictValue = DecodeExpression(
                    model, typeRef.ValueType, "item", isCallback: false, forDataObject, capabilityId,
                    transportExpression: transportExpression);
                return $"{DictClass}<{dictValueType}>({RuntimeClass}.requireHandle({source}, '{EscapeString(capabilityId)}'), {transportExpression}, "
                    + $"(Object? item) => {dictValue})";

            default:
                return source;
        }
    }

    // ── Documentation ────────────────────────────────────────────────────────

    private static void WriteCapabilityDoc(
        DartWriter writer,
        AtsCapabilityInfo capability,
        IReadOnlyList<AtsParameterInfo> required,
        IReadOnlyList<AtsParameterInfo> optional,
        IReadOnlyList<DartProperty> flattenedProperties,
        IReadOnlyDictionary<string, string> localNames)
    {
        var entries = new List<(string Name, string? Description)>();

        foreach (var parameter in required.Concat(optional))
        {
            entries.Add((
                $"[{localNames["p:" + parameter.Name]}]",
                DocumentationFor(capability.Documentation, parameter.Name) ?? parameter.Documentation?.Summary));
        }

        foreach (var property in flattenedProperties)
        {
            entries.Add((
                $"[{localNames["o:" + property.WireName]}]",
                property.Documentation?.Summary ?? property.Description));
        }

        var isVoid = capability.ReturnType is null
            || string.Equals(capability.ReturnType.TypeId, AtsConstants.Void, StringComparison.Ordinal);

        WriteDoc(writer, BuildDoc(
            capability.Documentation,
            capability.Description,
            $"Invokes the `{capability.CapabilityId}` capability.",
            sections: [new DocSection("## Parameters", entries)],
            returns: isVoid ? null : capability.Documentation?.Returns));
    }

    /// <summary>
    /// A named list that a generated dartdoc block holds, such as the parameters of a method or the
    /// properties of a data object.
    /// </summary>
    private sealed record DocSection(string Heading, IReadOnlyList<(string Name, string? Description)> Entries);

    /// <summary>
    /// Builds the text of a dartdoc comment.
    /// </summary>
    /// <remarks>
    /// The summary comes from <see cref="AtsDocumentationInfo.Summary"/>. An empty
    /// <c>&lt;ats-summary&gt;</c> element suppresses the summary: the scanner then returns a
    /// documentation object whose <c>Summary</c> is <see langword="null"/>. The generator keeps that
    /// decision and does not fall back to the <c>[AspireExport(Description = ...)]</c> text. The
    /// fallback applies only when the whole documentation object is missing.
    /// </remarks>
    private static string BuildDoc(
        AtsDocumentationInfo? documentation,
        string? fallback,
        string? defaultSummary,
        IReadOnlyList<DocSection>? sections = null,
        string? returns = null)
    {
        var summary = documentation is null
            ? Coalesce(fallback, defaultSummary)
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

        foreach (var section in sections ?? [])
        {
            var entries = section.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Description))
                .ToList();

            if (entries.Count == 0)
            {
                continue;
            }

            AddBlank(lines);
            lines.Add(section.Heading);
            AddBlank(lines);

            foreach (var (name, description) in entries)
            {
                lines.Add($"* {name} — {ConvertAtsReferences(Flatten(description!))}");
            }
        }

        if (!string.IsNullOrWhiteSpace(returns))
        {
            AddBlank(lines);
            lines.Add("## Returns");
            AddBlank(lines);
            lines.Add(ConvertAtsReferences(returns.Trim()));
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
    }

    private static void WriteDoc(DartWriter writer, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmed = line.TrimEnd();
            writer.WriteLine(trimmed.Length == 0 ? "///" : "/// " + trimmed);
        }
    }

    /// <summary>
    /// Turns the language neutral reference markers of the scanner into plain dartdoc text.
    /// </summary>
    /// <remarks>
    /// The scanner writes <c>&lt;ats-see cref="!:type:Foo"/&gt;</c> as <c>{@ats-ref type:Foo}</c>,
    /// and adds <c>|label</c> when the element holds text. Dartdoc reads <c>{@...}</c> as a
    /// directive, so the marker never survives into the generated code. A marker without a label
    /// becomes a backtick link. Dartdoc has no labelled link form, so a marker with a label keeps
    /// the label and names the target after it.
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
                builder.Append('`').Append(reference).Append('`');
            }
            else
            {
                var remainder = reference[(separator + 1)..];
                var labelIndex = remainder.IndexOf('|', StringComparison.Ordinal);
                var target = labelIndex < 0 ? remainder : remainder[..labelIndex];
                var label = labelIndex < 0 ? null : remainder[(labelIndex + 1)..];

                builder.Append(string.IsNullOrWhiteSpace(label) ? $"`{target}`" : $"{label} (`{target}`)");
            }

            index = end + 1;
        }

        return builder.ToString();
    }

    private static string Flatten(string value) =>
        string.Join(" ", value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? DocumentationFor(AtsDocumentationInfo? documentation, string parameterName) =>
        documentation?.Parameters
            .FirstOrDefault(parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal))?
            .Description;

    // ── Identifiers ──────────────────────────────────────────────────────────

    private static bool IsCancellationTokenParameter(AtsParameterInfo parameter) =>
        IsCancellationTokenTypeId(parameter.Type?.TypeId);

    private static bool IsCancellationTokenTypeId(string? typeId) =>
        string.Equals(typeId, AtsConstants.CancellationToken, StringComparison.Ordinal)
        || (typeId?.EndsWith("/System.Threading.CancellationToken", StringComparison.Ordinal) ?? false);

    private static Dictionary<string, string> AssignUniqueNames(
        IReadOnlyList<string> keys,
        Func<string, string> convert)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            if (result.ContainsKey(key))
            {
                continue;
            }

            var candidate = convert(key);
            var name = candidate;
            var counter = 1;
            while (!used.Add(name))
            {
                counter++;
                name = string.Create(CultureInfo.InvariantCulture, $"{candidate}{counter}");
            }

            result[key] = name;
        }

        return result;
    }

    /// <summary>
    /// Converts a name to a Dart method name. The ATS method name is already camel case.
    /// </summary>
    private static string ToDartMethodName(string name)
    {
        var sanitized = ToCamelCase(name);
        return s_dartKeywords.Contains(sanitized) || s_reservedMemberNames.Contains(sanitized)
            ? sanitized + "_"
            : sanitized;
    }

    /// <summary>
    /// Converts a name to a Dart parameter or local name.
    /// </summary>
    private static string ToDartLocalName(string name)
    {
        var sanitized = ToCamelCase(name);
        return s_dartKeywords.Contains(sanitized) || s_reservedLocalNames.Contains(sanitized)
            ? sanitized + "_"
            : sanitized;
    }

    /// <summary>
    /// Converts a name to a Dart member name, such as an enum value or a data object property.
    /// </summary>
    private static string ToDartMemberName(string name)
    {
        var sanitized = ToCamelCase(name);
        return s_dartKeywords.Contains(sanitized) || s_reservedMemberNames.Contains(sanitized)
            ? sanitized + "_"
            : sanitized;
    }

    private static string ToCamelCase(string name)
    {
        var sanitized = SanitizeIdentifier(name);
        if (sanitized.Length == 0)
        {
            return "value";
        }

        if (!char.IsUpper(sanitized[0]))
        {
            return sanitized;
        }

        // An acronym prefix such as "URL" becomes "url", so "URLValue" becomes "urlValue".
        var upper = 0;
        while (upper < sanitized.Length && char.IsUpper(sanitized[upper]))
        {
            upper++;
        }

        if (upper > 1 && upper < sanitized.Length)
        {
            upper--;
        }

        return sanitized[..upper].ToLowerInvariant() + sanitized[upper..];
    }

    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        if (builder.Length > 0 && char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static string EscapeString(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal)
        .Replace("$", "\\$", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    // ── Model ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Holds every generated Dart name and the capability grouping the generator walks.
    /// </summary>
    private sealed class DartModel
    {
        public required AtsContext Context { get; init; }

        public required Dictionary<string, string> HandleClasses { get; init; }

        public required Dictionary<string, string> DtoClasses { get; init; }

        public required Dictionary<string, string> EnumClasses { get; init; }

        /// <summary>
        /// The Dart class of every exported value path, keyed by the dotted path above the value.
        /// </summary>
        public required Dictionary<string, string> ValueClasses { get; init; }

        public required Dictionary<string, AtsTypeInfo> HandleTypeInfos { get; init; }

        public required Dictionary<string, List<AtsCapabilityInfo>> CapabilitiesByTarget { get; init; }

        /// <summary>
        /// The Dart properties of every data object, in declaration order.
        /// </summary>
        public required Dictionary<string, List<DartProperty>> DtoProperties { get; init; }

        /// <summary>
        /// The ATS type ids whose Dart class is hand written in <c>aspire_runtime.dart</c>. The
        /// generator maps them but emits no class.
        /// </summary>
        public required HashSet<string> RuntimeProvidedTypeIds { get; init; }

        /// <summary>
        /// Every callback shape the model holds, keyed by its Dart signature. Two callbacks that
        /// take the same argument types and return the same type share one typedef.
        /// </summary>
        public required Dictionary<string, DartCallbackShape> CallbackShapes { get; init; }

        /// <summary>
        /// The Dart classes that can be assigned to a handle type. It holds the class of the type
        /// and the class of every handle type that implements or extends it.
        /// </summary>
        public required Dictionary<string, List<string>> UnionExpansions { get; init; }

        public IReadOnlyList<string> ExpandUnionMember(string typeId) =>
            UnionExpansions.TryGetValue(typeId, out var classes)
                ? classes
                : HandleClasses.TryGetValue(typeId, out var className) ? [className] : [];

        /// <summary>
        /// Returns the callback shape of a capability parameter, or null when the parameter is not
        /// a callback or the scanner captured no signature for it.
        /// </summary>
        public DartCallbackShape? ShapeOf(AtsParameterInfo parameter) =>
            parameter.IsCallback && parameter.CallbackParameters is { } parameters
                ? CallbackShapes.GetValueOrDefault(ShapeKey(this, parameters, parameter.CallbackReturnType))
                : null;

        public static DartModel Build(AtsContext context)
        {
            var dtoTypeIds = new HashSet<string>(context.DtoTypes.Select(dto => dto.TypeId), StringComparer.Ordinal);
            var handleTypeIds = CollectHandleTypeIds(context, dtoTypeIds);

            // Dart has one global scope, so handle classes, data objects and enums share one name
            // pool.
            var candidates = new List<ClassNameCandidate>();
            foreach (var typeId in handleTypeIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                candidates.Add(ClassNameCandidate.ForType(typeId, IsInterfaceType(context, typeId)));
            }

            foreach (var dto in context.DtoTypes.OrderBy(dto => dto.TypeId, StringComparer.Ordinal))
            {
                candidates.Add(ClassNameCandidate.ForType(dto.TypeId, isInterface: false));
            }

            foreach (var enumType in context.EnumTypes.OrderBy(type => type.TypeId, StringComparer.Ordinal))
            {
                candidates.Add(ClassNameCandidate.ForEnum(enumType));
            }

            var assigned = AssignClassNames(candidates);

            var handleClasses = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var typeId in handleTypeIds)
            {
                handleClasses[typeId] = assigned[typeId];
            }

            var dtoClasses = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var dto in context.DtoTypes)
            {
                dtoClasses[dto.TypeId] = assigned[dto.TypeId];
            }

            var enumClasses = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var enumType in context.EnumTypes)
            {
                enumClasses[enumType.TypeId] = assigned[enumType.TypeId];
            }

            var handleTypeInfos = new Dictionary<string, AtsTypeInfo>(StringComparer.Ordinal);
            foreach (var typeInfo in context.HandleTypes)
            {
                handleTypeInfos.TryAdd(typeInfo.AtsTypeId, typeInfo);
            }

            var runtimeProvided = new HashSet<string>(StringComparer.Ordinal);
            if (handleClasses.ContainsKey(AtsConstants.ReferenceExpressionTypeId))
            {
                // aspire_runtime.dart holds the reference expression, because a guest builds one
                // with ref() and the host also returns one as a handle.
                handleClasses[AtsConstants.ReferenceExpressionTypeId] = ReferenceExpressionClass;
                runtimeProvided.Add(AtsConstants.ReferenceExpressionTypeId);
            }

            var valueClasses = AssignValueClassNames(context, assigned.Values);

            var model = new DartModel
            {
                Context = context,
                HandleClasses = handleClasses,
                DtoClasses = dtoClasses,
                EnumClasses = enumClasses,
                ValueClasses = valueClasses,
                HandleTypeInfos = handleTypeInfos,
                CapabilitiesByTarget = GroupCapabilitiesByTarget(context.Capabilities, handleTypeIds),
                DtoProperties = new Dictionary<string, List<DartProperty>>(StringComparer.Ordinal),
                RuntimeProvidedTypeIds = runtimeProvided,
                CallbackShapes = new Dictionary<string, DartCallbackShape>(StringComparer.Ordinal),
                UnionExpansions = BuildUnionExpansions(context, handleClasses)
            };

            // A typedef name shares the one Dart scope, so it is reserved after every class name.
            var used = new HashSet<string>(assigned.Values, StringComparer.Ordinal);
            used.UnionWith(valueClasses.Values);
            used.UnionWith(s_reservedClassNames);
            PopulateCallbackShapes(model, used);
            PopulateDtoProperties(model);

            return model;
        }

        /// <summary>
        /// Assigns one typedef to every distinct callback signature in the model.
        /// </summary>
        private static void PopulateCallbackShapes(DartModel model, HashSet<string> used)
        {
            var candidates = new List<(string Key, IReadOnlyList<AtsCallbackParameterInfo> Parameters, AtsTypeRef? ReturnType)>();

            void Add(IReadOnlyList<AtsCallbackParameterInfo>? parameters, AtsTypeRef? returnType)
            {
                if (parameters is null)
                {
                    return;
                }

                candidates.Add((ShapeKey(model, parameters, returnType), parameters, returnType));
            }

            foreach (var capability in model.Context.Capabilities)
            {
                foreach (var parameter in capability.Parameters)
                {
                    if (parameter.IsCallback)
                    {
                        Add(parameter.CallbackParameters, parameter.CallbackReturnType);
                    }
                }
            }

            foreach (var dto in model.Context.DtoTypes)
            {
                foreach (var property in dto.Properties)
                {
                    if (property.IsCallback)
                    {
                        Add(property.CallbackParameters, property.CallbackReturnType);
                    }
                }
            }

            foreach (var candidate in candidates
                .GroupBy(entry => entry.Key, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => group.First()))
            {
                var parameters = candidate.Parameters;
                var writeBackIndexes = new List<int>();

                for (var index = 0; index < parameters.Count; index++)
                {
                    if (parameters[index].Type.Category == AtsTypeCategory.Dto
                        && model.DtoClasses.ContainsKey(parameters[index].Type.TypeId))
                    {
                        writeBackIndexes.Add(index);
                    }
                }

                var returnsValue = candidate.ReturnType is not null
                    && !string.Equals(candidate.ReturnType.TypeId, AtsConstants.Void, StringComparison.Ordinal);

                string dartReturnType;
                string? writeBackDtoClass = null;

                if (returnsValue)
                {
                    dartReturnType = MapType(model, candidate.ReturnType, forDataObject: false, isOptional: true);
                }
                else if (writeBackIndexes.Count == 1)
                {
                    writeBackDtoClass = model.DtoClasses[parameters[writeBackIndexes[0]].Type.TypeId];
                    dartReturnType = writeBackDtoClass + "?";
                }
                else if (writeBackIndexes.Count > 1)
                {
                    dartReturnType = "Object?";
                }
                else
                {
                    dartReturnType = "void";
                }

                var candidateName = ShapeNameCandidate(model, parameters);
                var name = candidateName;
                var counter = 1;
                while (!used.Add(name))
                {
                    counter++;
                    name = string.Create(CultureInfo.InvariantCulture, $"{candidateName}{counter}");
                }

                var parameterNames = AssignUniqueNames(
                    parameters.Select(parameter => parameter.Name).ToList(),
                    ToDartLocalName);

                model.CallbackShapes[candidate.Key] = new DartCallbackShape(
                    name,
                    parameters,
                    parameters.Select(parameter => parameterNames[parameter.Name]).ToList(),
                    returnsValue,
                    dartReturnType,
                    writeBackIndexes,
                    writeBackDtoClass);
            }
        }

        private static void PopulateDtoProperties(DartModel model)
        {
            foreach (var dto in model.Context.DtoTypes)
            {
                var properties = dto.Properties.ToList();
                var names = AssignUniqueNames(
                    properties.Select(property => property.Name).ToList(),
                    ToDartMemberName);

                model.DtoProperties[dto.TypeId] = properties
                    .Select(property =>
                    {
                        var shape = property.IsCallback && property.CallbackParameters is { } callbackParameters
                            ? model.CallbackShapes.GetValueOrDefault(
                                ShapeKey(model, callbackParameters, property.CallbackReturnType))
                            : null;

                        var callbackType = shape?.Name ?? "Function";
                        var dartType = property.IsCallback
                            ? callbackType + "?"
                            : MakeNullable(MapType(model, property.Type, forDataObject: true, isOptional: true));

                        return new DartProperty(
                            names[property.Name],
                            property.Name,
                            property.Type,
                            property.IsCallback,
                            property.Description,
                            property.Documentation,
                            dartType,
                            shape,
                            callbackType);
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Maps every handle type id to the classes a caller can pass for it.
        /// </summary>
        /// <remarks>
        /// A Dart class never extends another generated class, so an interface member of a union has
        /// to name every concrete wrapper that implements it. Two members of one union often expand
        /// to the same class, so the caller of this map deduplicates the result.
        /// </remarks>
        private static Dictionary<string, List<string>> BuildUnionExpansions(
            AtsContext context,
            Dictionary<string, string> handleClasses)
        {
            var expansions = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

            void Add(string typeId, string className)
            {
                if (!expansions.TryGetValue(typeId, out var classes))
                {
                    classes = new SortedSet<string>(StringComparer.Ordinal);
                    expansions[typeId] = classes;
                }

                classes.Add(className);
            }

            foreach (var (typeId, className) in handleClasses)
            {
                Add(typeId, className);
            }

            foreach (var typeInfo in context.HandleTypes)
            {
                if (typeInfo.IsInterface || !handleClasses.TryGetValue(typeInfo.AtsTypeId, out var concreteClass))
                {
                    continue;
                }

                foreach (var implemented in typeInfo.ImplementedInterfaces)
                {
                    if (handleClasses.ContainsKey(implemented.TypeId))
                    {
                        Add(implemented.TypeId, concreteClass);
                    }
                }

                foreach (var baseType in typeInfo.BaseTypeHierarchy)
                {
                    if (handleClasses.ContainsKey(baseType.TypeId))
                    {
                        Add(baseType.TypeId, concreteClass);
                    }
                }
            }

            return expansions.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToList(),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, string> AssignValueClassNames(AtsContext context, IEnumerable<string> usedNames)
        {
            var used = new HashSet<string>(usedNames, StringComparer.Ordinal);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            var paths = context.ExportedValues
                .Where(value => value.PathSegments.Count > 1)
                .Select(value => string.Join(".", value.PathSegments.Take(value.PathSegments.Count - 1)))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal);

            foreach (var path in paths)
            {
                var candidate = string.Concat(path.Split('.').Select(ToPascalCase));
                candidate = s_reservedClassNames.Contains(candidate) ? candidate + "Type" : candidate;

                var name = candidate;
                var counter = 1;
                while (!used.Add(name))
                {
                    counter++;
                    name = string.Create(CultureInfo.InvariantCulture, $"{candidate}{counter}");
                }

                result[path] = name;
            }

            return result;
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
                Add(typeInfo.AtsTypeId);
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
                    Add(typeRef.TypeId);
                }
            }

            void Add(string typeId)
            {
                if (dtoTypeIds.Contains(typeId) || IsCancellationTokenTypeId(typeId))
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

        /// <summary>
        /// Assigns one Dart class name to every ATS type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Dart has no module namespace, so the name has to carry the whole identity. The generator
        /// tries three forms in order and takes the first that is unique:
        /// </para>
        /// <list type="number">
        ///   <item><description>the simple type name, with a leading <c>I</c> dropped for an interface;</description></item>
        ///   <item><description>the simple type name as it is, which separates <c>IFoo</c> from <c>Foo</c>;</description></item>
        ///   <item><description>the assembly segment followed by the simple type name, such as <c>RedisRedisResource</c>.</description></item>
        /// </list>
        /// <para>
        /// A name that a runtime file or <c>dart:core</c> already defines takes the <c>Type</c>
        /// suffix. Two types that still map to the same name fail the generation, because a silent
        /// rename would move capabilities onto the wrong class.
        /// </para>
        /// </remarks>
        private static Dictionary<string, string> AssignClassNames(IReadOnlyList<ClassNameCandidate> candidates)
        {
            var assigned = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var group in candidates.GroupBy(candidate => candidate.PreferredName, StringComparer.Ordinal))
            {
                var members = group.ToList();
                foreach (var member in members)
                {
                    assigned[member.TypeId] = Reserve(members.Count == 1 ? member.PreferredName : member.SimpleName);
                }
            }

            var byName = candidates.ToDictionary(candidate => candidate.TypeId, StringComparer.Ordinal);

            foreach (var group in assigned.GroupBy(pair => pair.Value, StringComparer.Ordinal).Where(group => group.Count() > 1).ToList())
            {
                foreach (var pair in group.ToList())
                {
                    assigned[pair.Key] = Reserve(byName[pair.Key].QualifiedName);
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
                    collisions.Select(group =>
                        $"{group.Key} <- [{string.Join(", ", group.Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal))}]"));

                throw new InvalidOperationException(
                    "Dart code generation cannot continue because two or more ATS types map to the same Dart class name. " +
                    "Rename one of the types, or move it to another assembly. Collisions: " + details);
            }

            return assigned;

            static string Reserve(string name) =>
                s_reservedClassNames.Contains(name) ? name + "Type" : name;
        }
    }

    /// <summary>
    /// The three name forms that one ATS type can take in Dart, from the shortest to the most
    /// qualified.
    /// </summary>
    private sealed record ClassNameCandidate(
        string TypeId,
        string PreferredName,
        string SimpleName,
        string QualifiedName)
    {
        public static ClassNameCandidate ForType(string typeId, bool isInterface)
        {
            var simpleName = ToPascalCase(ExtractSimpleTypeName(typeId));
            var preferred = isInterface ? StripLeadingInterfacePrefix(simpleName) : simpleName;
            return new ClassNameCandidate(typeId, preferred, simpleName, AssemblySegment(typeId) + simpleName);
        }

        public static ClassNameCandidate ForEnum(AtsEnumTypeInfo enumType)
        {
            // An enum type id has the form "enum:{FullTypeName}". It carries no assembly, so the
            // qualified name flattens the namespace instead.
            var fullName = enumType.TypeId.StartsWith(AtsConstants.EnumPrefix, StringComparison.Ordinal)
                ? enumType.TypeId[AtsConstants.EnumPrefix.Length..]
                : enumType.TypeId;

            var simpleName = ToPascalCase(ExtractSimpleName(fullName));
            var qualified = string.Concat(fullName.Split('.', '+').Select(ToPascalCase));

            return new ClassNameCandidate(enumType.TypeId, simpleName, simpleName, qualified);
        }

        private static string StripLeadingInterfacePrefix(string name) =>
            name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]) ? name[1..] : name;

        /// <summary>
        /// Returns the part of the assembly name that separates it from <c>Aspire.Hosting</c>.
        /// </summary>
        private static string AssemblySegment(string typeId)
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

            return string.Concat(remainder.Split('.', '+').Select(ToPascalCase));
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

    private static string ToPascalCase(string name)
    {
        var sanitized = SanitizeIdentifier(name);
        if (sanitized.Length == 0)
        {
            return "Value";
        }

        if (char.IsUpper(sanitized[0]))
        {
            return sanitized;
        }

        return char.ToUpperInvariant(sanitized[0]) + sanitized[1..];
    }

    /// <summary>
    /// A property of a generated data object.
    /// </summary>
    private sealed record DartProperty(
        string DartName,
        string WireName,
        AtsTypeRef? Type,
        bool IsCallback,
        string? Description,
        AtsDocumentationInfo? Documentation,
        string DartType,
        DartCallbackShape? Shape,
        string CallbackType);

    /// <summary>
    /// One distinct callback signature. Every callback that takes the same argument types and
    /// returns the same type shares this typedef.
    /// </summary>
    /// <param name="Name">The Dart typedef name.</param>
    /// <param name="Parameters">The ATS arguments the host sends, in order.</param>
    /// <param name="ParameterNames">The Dart name of every argument, in the same order.</param>
    /// <param name="ReturnsValue">True when the host reads the value the callback returns.</param>
    /// <param name="DartReturnType">The type inside <c>FutureOr&lt;…&gt;</c>.</param>
    /// <param name="WriteBackIndexes">
    /// The positions of the data object arguments. A generated data object never changes in place,
    /// so the callback returns the changed object and the wrapper writes it back.
    /// </param>
    /// <param name="WriteBackDtoClass">
    /// The one data object class the callback returns, or null when there is not exactly one.
    /// </param>
    private sealed record DartCallbackShape(
        string Name,
        IReadOnlyList<AtsCallbackParameterInfo> Parameters,
        IReadOnlyList<string> ParameterNames,
        bool ReturnsValue,
        string DartReturnType,
        IReadOnlyList<int> WriteBackIndexes,
        string? WriteBackDtoClass);

    /// <summary>
    /// Returns the key that groups two callbacks onto one typedef.
    /// </summary>
    private static string ShapeKey(
        DartModel model,
        IReadOnlyList<AtsCallbackParameterInfo> parameters,
        AtsTypeRef? returnType)
    {
        var returnsValue = returnType is not null
            && !string.Equals(returnType.TypeId, AtsConstants.Void, StringComparison.Ordinal);

        var arguments = string.Join(
            ",",
            parameters.Select(parameter => CallbackParameterType(model, parameter)));

        var result = returnsValue
            ? MapType(model, returnType, forDataObject: false, isOptional: true)
            : "void";

        return arguments + "->" + result;
    }

    /// <summary>
    /// Returns the preferred typedef name of a callback signature.
    /// </summary>
    /// <remarks>
    /// The name is built from the argument types, because that is what tells two callbacks apart.
    /// A trailing <c>Context</c> is dropped, so <c>EnvironmentCallbackContext</c> gives
    /// <c>EnvironmentCallback</c>.
    /// </remarks>
    private static string ShapeNameCandidate(DartModel model, IReadOnlyList<AtsCallbackParameterInfo> parameters)
    {
        if (parameters.Count == 0)
        {
            return "AspireCallback";
        }

        var builder = new StringBuilder();
        foreach (var parameter in parameters)
        {
            builder.Append(ShapeRoleName(model, parameter.Type));
        }

        var name = builder.ToString();
        return name.EndsWith("Callback", StringComparison.Ordinal) ? name : name + "Callback";
    }

    private static string ShapeRoleName(DartModel model, AtsTypeRef typeRef)
    {
        var dartType = MapType(model, typeRef, forDataObject: false, isOptional: false);

        var generic = dartType.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0)
        {
            dartType = dartType[..generic];
        }

        dartType = ToPascalCase(dartType.TrimEnd('?'));

        const string ContextSuffix = "Context";
        return dartType.Length > ContextSuffix.Length && dartType.EndsWith(ContextSuffix, StringComparison.Ordinal)
            ? dartType[..^ContextSuffix.Length]
            : dartType;
    }

    private sealed record GeneratedFile(string FileName, string Source);

    private sealed record DartDeclaration(string Source, int LineCount)
    {
        public static DartDeclaration From(DartWriter writer)
        {
            var source = writer.ToSource();
            return new DartDeclaration(source, source.Count(character => character == '\n'));
        }
    }

    // ── Writer ───────────────────────────────────────────────────────────────

    private sealed class DartWriter
    {
        private readonly StringBuilder _builder = new();

        private int _indentLevel;

        public void Indent() => _indentLevel++;

        public void Outdent() => _indentLevel = Math.Max(0, _indentLevel - 1);

        public void WriteLine(string value = "")
        {
            if (value.Length == 0)
            {
                _builder.Append('\n');
                return;
            }

            _builder.Append(' ', _indentLevel * 2).Append(value).Append('\n');
        }

        public string ToSource() => _builder.ToString();
    }
}
