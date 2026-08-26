// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart;

/// <summary>
/// Represents a Jaspr web application resource in the distributed application model.
/// </summary>
/// <remarks>
/// <para>
/// This resource runs a Jaspr application with <c>jaspr serve</c>. The <c>jaspr</c> command-line
/// tool starts the application, so the command of the resource is <c>jaspr</c> and not <c>dart</c>.
/// </para>
/// <para>
/// The resource has one HTTP endpoint. <c>jaspr serve</c> reads the port of that endpoint from the
/// <c>-p</c> option.
/// </para>
/// </remarks>
/// <example>
/// Add a Jaspr application to the distributed application model:
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
///
/// builder.AddJasprApp("web", "../jaspr-web")
///        .WithExternalHttpEndpoints();
///
/// builder.Build().Run();
/// </code>
/// </example>
/// <param name="name">The name of the resource in the application model.</param>
/// <param name="workingDirectory">The working directory for the Jaspr application, the directory that contains <c>pubspec.yaml</c>.</param>
[AspireExport(ExposeProperties = true)]
public class JasprAppResource(string name, string workingDirectory)
    : DartAppResource(name, "jaspr", workingDirectory);
