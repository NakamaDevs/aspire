// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart;

/// <summary>
/// Represents a Serverpod server resource in the distributed application model.
/// </summary>
/// <remarks>
/// <para>
/// This resource runs a Serverpod server with <c>dart bin/main.dart</c>. The resource has three HTTP
/// endpoints: <c>api</c>, <c>insights</c>, and <c>web</c>. Serverpod reads the port of each endpoint
/// from an environment variable.
/// </para>
/// <para>
/// A Serverpod project holds the server in a directory with the name <c>{project}_server</c>. That
/// directory contains <c>pubspec.yaml</c> and <c>bin/main.dart</c>.
/// </para>
/// </remarks>
/// <example>
/// Add a Serverpod server to the distributed application model:
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
///
/// var db = builder.AddPostgres("pg").AddDatabase("serverpod");
///
/// builder.AddServerpodApp("api", "../myapp_server")
///        .WithServerpodDatabase(db);
///
/// builder.Build().Run();
/// </code>
/// </example>
/// <param name="name">The name of the resource in the application model.</param>
/// <param name="workingDirectory">The working directory for the Serverpod server, the directory that contains <c>pubspec.yaml</c>.</param>
[AspireExport(ExposeProperties = true)]
public class ServerpodAppResource(string name, string workingDirectory)
    : DartAppResource(name, workingDirectory);
