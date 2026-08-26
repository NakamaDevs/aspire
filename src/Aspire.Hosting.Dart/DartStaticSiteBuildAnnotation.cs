// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Describes a build command that writes a directory of static files, which a web server sends to
/// the browser without a Dart process.
/// </summary>
/// <param name="Command">The build command, for example <c>flutter</c> or <c>jaspr</c>.</param>
/// <param name="Args">The arguments of the build command, for example <c>build</c>, <c>web</c>.</param>
/// <param name="OutputDirectory">
/// The directory that the command writes, relative to the application directory, for example
/// <c>build/web</c>.
/// </param>
/// <param name="SpaFallback">
/// <see langword="true"/> when the web server must send <c>index.html</c> for a path that names no
/// file. A single-page application needs that rule, because the browser owns the routes.
/// </param>
/// <param name="BuildImage">
/// The image of the build stage, or <see langword="null"/> for the official <c>dart</c> image. A
/// build command that the Dart SDK does not supply needs an image that holds it, for example
/// <c>ghcr.io/cirruslabs/flutter:stable</c> for <c>flutter build web</c>.
/// </param>
internal sealed record DartStaticSiteBuildAnnotation(
    string Command,
    string[] Args,
    string OutputDirectory,
    bool SpaFallback,
    string? BuildImage = null) : IResourceAnnotation;
