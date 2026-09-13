// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Represents a Dart application resource in the distributed application model.
/// </summary>
/// <remarks>
/// <para>
/// This resource runs a Dart application with the <c>dart</c> command. The resource manages the
/// working directory and the lifecycle of the Dart application.
/// </para>
/// <para>
/// Dart applications can expose HTTP endpoints, communicate with other services, and participate in
/// service discovery like other Aspire resources.
/// </para>
/// </remarks>
/// <example>
/// Add a Dart application to the distributed application model:
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
///
/// var api = builder.AddDartApp("api", "../dart-api")
///     .WithHttpEndpoint(port: 8080);
///
/// builder.Build().Run();
/// </code>
/// </example>
[AspireExport(ExposeProperties = true)]
public class DartAppResource
    : ExecutableResource, IResourceWithServiceDiscovery, IContainerFilesDestinationResource
{
    /// <param name="name">The name of the resource in the application model.</param>
    /// <param name="workingDirectory">The working directory for the Dart application, typically the directory that contains <c>pubspec.yaml</c>.</param>
    public DartAppResource(string name, string workingDirectory)
        : this(name, "dart", workingDirectory)
    {
    }

    /// <summary>
    /// Creates a Dart application resource that a different command starts.
    /// </summary>
    /// <remarks>
    /// A Dart framework can supply its own command-line tool, for example <c>jaspr</c>. That tool
    /// starts the application instead of <c>dart</c>, but the resource keeps the Dart behavior.
    /// </remarks>
    /// <param name="name">The name of the resource in the application model.</param>
    /// <param name="command">The command that starts the application.</param>
    /// <param name="workingDirectory">The working directory for the Dart application, typically the directory that contains <c>pubspec.yaml</c>.</param>
    protected DartAppResource(string name, string command, string workingDirectory)
        : base(name, command, workingDirectory)
    {
    }
}
