// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the Serverpod run mode, for example <c>development</c> or <c>production</c>.
/// </summary>
/// <remarks>
/// The mode selects the configuration file below <c>config</c>, and it becomes the value of the
/// <c>--mode</c> option and of <c>SERVERPOD_RUN_MODE</c>.
/// </remarks>
internal sealed class ServerpodModeAnnotation(string mode) : IResourceAnnotation
{
    /// <summary>The mode that a local run uses.</summary>
    public const string Development = "development";

    /// <summary>The mode that a deployed server uses.</summary>
    public const string Production = "production";

    /// <summary>The run mode of the server.</summary>
    public string Mode { get; } = mode;

    /// <summary>Returns the run mode of <paramref name="resource"/>, or the default value.</summary>
    public static string Resolve(IResource resource)
        => resource.TryGetLastAnnotation<ServerpodModeAnnotation>(out var annotation)
            ? annotation.Mode
            : Development;
}
