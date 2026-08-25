// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.RemoteHost;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.TypeScript.Tests;

/// <summary>
/// Covers the ATS surface that the Elixir hosting assembly exports to the guest SDKs.
/// </summary>
public class ElixirAssemblyExportTests
{
    private readonly AtsTypeScriptCodeGenerator _generator = new();

    [Fact]
    public void Scanner_ElixirAssembly_ExposesAddElixirAppCapability()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadElixirAssemblies());

        var addElixirApp = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.Elixir/addElixirApp");
        Assert.NotNull(addElixirApp);

        // The signature is the contract that every guest SDK generates from.
        Assert.Equal(
            ["name", "appDirectory"],
            addElixirApp.Parameters.Select(p => p.Name));
        Assert.Equal(
            AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Elixir.ElixirAppResource)),
            addElixirApp.ReturnType.TypeId);

        // The other entry points are part of the same surface.
        var capabilityIds = result.Capabilities.Select(c => c.CapabilityId).ToList();
        Assert.Contains("Aspire.Hosting.Elixir/addPhoenixApp", capabilityIds);
        Assert.Contains("Aspire.Hosting.Elixir/addMixRelease", capabilityIds);

        // A generic method with a second IResourceBuilder parameter needs an explicit capability
        // name, so ASPIREEXPORT009 does not report it.
        Assert.Contains("Aspire.Hosting.Elixir/withElixirEctoDatabase", capabilityIds);
        Assert.Contains("Aspire.Hosting.Elixir/withElixirNodeName", capabilityIds);
    }

    [Fact]
    public void Scanner_ElixirAssembly_WithMixDepsExpandsToAllElixirResourceTypes()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadElixirAssemblies());

        var withMixDeps = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.Elixir/withMixDeps");
        Assert.NotNull(withMixDeps);

        var expandedTypeIds = withMixDeps.ExpandedTargetTypes.Select(t => t.TypeId).ToList();

        var elixirAppTypeId = AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Elixir.ElixirAppResource));
        var phoenixAppTypeId = AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Elixir.PhoenixAppResource));
        var mixReleaseTypeId = AtsTypeMapping.DeriveTypeId(typeof(Aspire.Hosting.Elixir.MixReleaseResource));

        Assert.Contains(elixirAppTypeId, expandedTypeIds);
        Assert.Contains(phoenixAppTypeId, expandedTypeIds);

        // MixReleaseResource does not derive from ElixirAppResource, because a built release runs
        // through its launcher script and never runs a Mix task.
        Assert.DoesNotContain(mixReleaseTypeId, expandedTypeIds);
    }

    [Fact]
    public void Scanner_ElixirAssembly_HasNoDiagnostics()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadElixirAssemblies());

        // Info diagnostics report every capability that the scanner found, so only a warning or an
        // error reports a problem in the exported surface.
        Assert.Empty(result.Diagnostics
            .Where(d => d.Severity > AtsDiagnosticSeverity.Info)
            .Select(d => $"{d.Severity}: {d.Message} [{d.Location}]"));
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithElixirAssembly_GeneratesTypeScript()
    {
        var result = AtsCapabilityScanner.ScanAssemblies(LoadElixirAssemblies());
        var atsContext = result.ToAtsContext();

        var files = _generator.GenerateDistributedApplication(atsContext);

        Assert.Contains("aspire.mts", files.Keys);

        var aspireTs = files["aspire.mts"];
        Assert.Contains("class ElixirAppResource", aspireTs);
        Assert.Contains("class PhoenixAppResource", aspireTs);
        Assert.Contains("addElixirApp", aspireTs);

        // Only the declarations of the Elixir capabilities are a snapshot. The complete module also
        // holds the shared hosting surface, which changes for reasons that have nothing to do with
        // this integration.
        // Property capabilities carry general names such as "command", so only the extension methods
        // select the Elixir declarations.
        var elixirMethodNames = result.Capabilities
            .Where(c => c.CapabilityId.StartsWith("Aspire.Hosting.Elixir/", StringComparison.Ordinal)
                && c.CapabilityKind == AtsCapabilityKind.Method)
            .Select(c => c.MethodName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var elixirDeclarations = string.Join(
            Environment.NewLine,
            aspireTs
                .Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .Where(line => elixirMethodNames.Any(name => line.Contains(name, StringComparison.Ordinal)))
                .Select(line => line.Trim()));

        await Verify(elixirDeclarations, extension: "ts")
            .UseFileName("ElixirGeneratedAspire");
    }

    private static Assembly[] LoadElixirAssemblies()
    {
        return
        [
            typeof(DistributedApplication).Assembly,
            typeof(Aspire.Hosting.Elixir.ElixirAppResource).Assembly
        ];
    }
}
