// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Represents an Elixir application resource in the distributed application model.
/// </summary>
/// <remarks>
/// <para>
/// This resource runs an Elixir application with the Mix build tool. The resource manages the
/// working directory and the lifecycle of the Elixir application.
/// </para>
/// <para>
/// Elixir applications can expose HTTP endpoints, communicate with other services, and participate
/// in service discovery like other Aspire resources.
/// </para>
/// </remarks>
/// <example>
/// Add an Elixir application to the distributed application model:
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
///
/// var api = builder.AddElixirApp("api", "../elixir-api")
///     .WithHttpEndpoint(port: 4000);
///
/// builder.Build().Run();
/// </code>
/// </example>
/// <param name="name">The name of the resource in the application model.</param>
/// <param name="workingDirectory">The working directory for the Elixir application, typically the directory that contains <c>mix.exs</c>.</param>
[AspireExport(ExposeProperties = true)]
public class ElixirAppResource(string name, string workingDirectory)
    : ExecutableResource(name, "mix", workingDirectory), IResourceWithServiceDiscovery, IContainerFilesDestinationResource;
