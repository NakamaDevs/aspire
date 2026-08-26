// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the Dart file that <c>dart run</c> starts, for example <c>bin/main.dart</c>.
/// </summary>
internal sealed class DartEntrypointAnnotation(string entrypoint) : IResourceAnnotation
{
    /// <summary>The default entrypoint of a Dart application.</summary>
    public const string DefaultEntrypoint = "bin/main.dart";

    /// <summary>The path of the entrypoint, relative to the working directory of the application.</summary>
    public string Entrypoint { get; } = entrypoint;

    /// <summary>Returns the entrypoint of <paramref name="resource"/>, or the default value.</summary>
    public static string Resolve(DartAppResource resource)
        => resource.TryGetLastAnnotation<DartEntrypointAnnotation>(out var annotation)
            ? annotation.Entrypoint
            : DefaultEntrypoint;
}
