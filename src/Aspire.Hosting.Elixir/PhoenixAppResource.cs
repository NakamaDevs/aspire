// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Represents a Phoenix web application resource in the distributed application model.
/// </summary>
/// <remarks>
/// <para>
/// This resource runs a Phoenix application with <c>mix phx.server</c>. The resource has one HTTP
/// endpoint. Phoenix reads the port of that endpoint from the <c>PORT</c> environment variable.
/// </para>
/// <para>
/// The resource sets <c>PHX_SERVER</c> to <c>true</c> and <c>PHX_HOST</c> to the host of the HTTP
/// endpoint. In publish mode the resource also sets <c>SECRET_KEY_BASE</c> from a generated secret
/// parameter.
/// </para>
/// </remarks>
/// <example>
/// Add a Phoenix application to the distributed application model:
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
///
/// builder.AddPhoenixApp("web", "../phoenix-web")
///        .WithExternalHttpEndpoints();
///
/// builder.Build().Run();
/// </code>
/// </example>
/// <param name="name">The name of the resource in the application model.</param>
/// <param name="workingDirectory">The working directory for the Phoenix application, typically the directory that contains <c>mix.exs</c>.</param>
[AspireExport(ExposeProperties = true)]
public class PhoenixAppResource(string name, string workingDirectory)
    : ElixirAppResource(name, workingDirectory);
