// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Represents a Mix release that Mix already built.
/// </summary>
/// <remarks>
/// <para>
/// A Mix release is a self-contained directory. It holds the compiled application, its dependencies,
/// and, in the usual configuration, the Erlang runtime system. The release starts through the
/// launcher script in its <c>bin</c> directory, so this resource does not use Mix and does not add
/// the Mix setup steps that <see cref="ElixirAppResource"/> adds.
/// </para>
/// <para>
/// Use <c>AddElixirApp</c> or <c>AddPhoenixApp</c> when Aspire must build the application from
/// source. Use this resource when a different build step already produced the release.
/// </para>
/// </remarks>
/// <example>
/// Add a prebuilt Mix release to the application model:
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
///
/// builder.AddMixRelease("api", "../elixir-api/_build/prod/rel/my_app");
///
/// builder.Build().Run();
/// </code>
/// </example>
/// <param name="name">The name of the resource in the application model.</param>
/// <param name="command">The path to the release launcher script in the <c>bin</c> directory.</param>
/// <param name="releaseDirectory">The root directory of the release.</param>
/// <param name="releaseName">The release name, which is also the name of the launcher script.</param>
[AspireExport(ExposeProperties = true)]
public class MixReleaseResource(string name, string command, string releaseDirectory, string releaseName)
    : ExecutableResource(name, command, releaseDirectory), IResourceWithServiceDiscovery, IContainerFilesDestinationResource
{
    /// <summary>
    /// Gets the release name. The launcher script in the <c>bin</c> directory has this name.
    /// </summary>
    public string ReleaseName { get; } = releaseName;
}
