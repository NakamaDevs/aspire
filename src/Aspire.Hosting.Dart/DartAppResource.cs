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
/// <param name="name">The name of the resource in the application model.</param>
/// <param name="workingDirectory">The working directory for the Dart application, typically the directory that contains <c>pubspec.yaml</c>.</param>
[AspireExport(ExposeProperties = true)]
public class DartAppResource(string name, string workingDirectory)
    : ExecutableResource(name, "dart", workingDirectory), IResourceWithServiceDiscovery, IContainerFilesDestinationResource;
