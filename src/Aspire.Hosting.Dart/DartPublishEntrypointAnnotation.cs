// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the Dart file that <c>dart compile exe</c> builds for the published image.
/// </summary>
/// <remarks>
/// A project can start one file in run mode and compile a different file for the image, for example
/// a file that reads no development configuration. Without this annotation the image compiles the
/// run entrypoint.
/// </remarks>
internal sealed record DartPublishEntrypointAnnotation(string Entrypoint) : IResourceAnnotation
{
    /// <summary>
    /// Returns the file that the image compiles: the publish entrypoint, or the run entrypoint.
    /// </summary>
    public static string Resolve(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource.TryGetLastAnnotation<DartPublishEntrypointAnnotation>(out var annotation))
        {
            return annotation.Entrypoint;
        }

        return resource.TryGetLastAnnotation<DartEntrypointAnnotation>(out var runEntrypoint)
            ? runEntrypoint.Entrypoint
            : DartEntrypointAnnotation.DefaultEntrypoint;
    }
}
