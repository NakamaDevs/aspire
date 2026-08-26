// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.RemoteHost;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.TypeScript.Tests;

/// <summary>
/// Covers the ATS surface that the Dart hosting assembly exports to the guest SDKs.
/// </summary>
public class DartAssemblyExportTests
{
    private readonly AtsTypeScriptCodeGenerator _generator = new();

    [Fact]
    public void Scanner_DartAssembly_ExposesAddDartAppCapability()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadDartAssemblies());

        var addDartApp = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.Dart/addDartApp");
        Assert.NotNull(addDartApp);

        // The signature is the contract that every guest SDK generates from.
        Assert.Equal(
            ["name", "appDirectory", "entrypoint"],
            addDartApp.Parameters.Select(p => p.Name));
        Assert.Equal(
            AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Dart.DartAppResource)),
            addDartApp.ReturnType.TypeId);

        // The other entry points are part of the same surface.
        var capabilityIds = result.Capabilities.Select(c => c.CapabilityId).ToList();
        Assert.Contains("Aspire.Hosting.Dart/addServerpodApp", capabilityIds);
        Assert.Contains("Aspire.Hosting.Dart/addJasprApp", capabilityIds);

        // A generic method with a second IResourceBuilder parameter needs an explicit capability
        // name, so ASPIREEXPORT009 does not report it.
        Assert.Contains("Aspire.Hosting.Dart/withDartServerpodDatabase", capabilityIds);
        Assert.Contains("Aspire.Hosting.Dart/withDartServerpodRedis", capabilityIds);
    }

    [Fact]
    public void Scanner_DartAssembly_WithPubGetExpandsToAllDartResourceTypes()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadDartAssemblies());

        var withPubGet = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.Dart/withPubGet");
        Assert.NotNull(withPubGet);

        var expandedTypeIds = withPubGet.ExpandedTargetTypes.Select(t => t.TypeId).ToList();

        // Every Dart resource type runs `dart pub get`, because Serverpod and Jaspr are presets on
        // top of the generic Dart application resource.
        Assert.Contains(
            AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Dart.DartAppResource)),
            expandedTypeIds);
        Assert.Contains(
            AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Dart.ServerpodAppResource)),
            expandedTypeIds);
        Assert.Contains(
            AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Dart.JasprAppResource)),
            expandedTypeIds);
    }

    [Fact]
    public void Scanner_DartAssembly_HasNoDiagnostics()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadDartAssemblies());

        // Info diagnostics report every capability that the scanner found, so only a warning or an
        // error reports a problem in the exported surface.
        Assert.Empty(result.Diagnostics
            .Where(d => d.Severity > AtsDiagnosticSeverity.Info)
            .Select(d => $"{d.Severity}: {d.Message} [{d.Location}]"));
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithDartAssembly_GeneratesTypeScript()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadDartAssemblies());
        var atsContext = result.ToAtsContext();

        var files = _generator.GenerateDistributedApplication(atsContext);

        Assert.Contains("aspire.mts", files.Keys);

        var aspireTs = files["aspire.mts"];
        Assert.Contains("class DartAppResource", aspireTs);
        Assert.Contains("class ServerpodAppResource", aspireTs);
        Assert.Contains("class JasprAppResource", aspireTs);
        Assert.Contains("addDartApp", aspireTs);

        // Only the declarations of the Dart capabilities are a snapshot. The complete module also
        // holds the shared hosting surface, which changes for reasons that have nothing to do with
        // this integration.
        // Property capabilities carry general names such as "command", so only the extension methods
        // select the Dart declarations.
        var dartMethodNames = result.Capabilities
            .Where(c => c.CapabilityId.StartsWith("Aspire.Hosting.Dart/", StringComparison.Ordinal)
                && c.CapabilityKind == AtsCapabilityKind.Method)
            .Select(c => c.MethodName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var dartDeclarations = string.Join(
            Environment.NewLine,
            aspireTs
                .Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .Where(line => dartMethodNames.Any(name => line.Contains(name, StringComparison.Ordinal)))
                .Select(line => line.Trim()));

        await Verify(dartDeclarations, extension: "ts")
            .UseFileName("DartGeneratedAspire");
    }

    private static Assembly[] LoadDartAssemblies()
    {
        return
        [
            typeof(DistributedApplication).Assembly,
            typeof(Aspire.Hosting.Dart.DartAppResource).Assembly
        ];
    }
}
