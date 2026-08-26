// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the name of a pub package that the build stage activates before it runs the build command.
/// </summary>
/// <remarks>
/// <c>dart pub global activate</c> installs the executables of the package below
/// <c>/root/.pub-cache/bin</c>. A build command such as <c>jaspr</c> comes from a package with a
/// different name, so the annotation holds the package name and not the command name.
/// </remarks>
/// <param name="Package">The pub package name, for example <c>jaspr_cli</c>.</param>
internal sealed record DartPublishToolAnnotation(string Package) : IResourceAnnotation;
