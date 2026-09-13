// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Describes a build command that writes a directory that holds a native executable and the files
/// that the executable reads at run time.
/// </summary>
/// <remarks>
/// Without this annotation the image runs <c>dart compile exe</c> on the publish entrypoint. That
/// step builds one file, so it fits a project that ships only a binary. A framework that also builds
/// a browser bundle needs its own command and its own output directory, and it needs the complete
/// directory in the image, because the executable finds the bundle next to itself.
/// </remarks>
/// <param name="Command">The build command, for example <c>jaspr</c>.</param>
/// <param name="Args">The arguments of the build command, for example <c>build</c>.</param>
/// <param name="OutputDirectory">
/// The directory that the command writes, relative to the application directory, for example
/// <c>build/jaspr</c>. The image receives the complete directory.
/// </param>
/// <param name="Executable">
/// The name of the executable inside <paramref name="OutputDirectory"/>, for example <c>app</c>.
/// </param>
internal sealed record DartCompiledBuildAnnotation(
    string Command,
    string[] Args,
    string OutputDirectory,
    string Executable) : IResourceAnnotation;
