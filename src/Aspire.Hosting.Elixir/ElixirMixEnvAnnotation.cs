// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Holds the Mix environment of an Elixir application.
/// </summary>
/// <remarks>
/// The application, the Mix setup siblings, and the generated Dockerfile must all use one Mix
/// environment. A build in one environment and a start in another environment makes Mix compile the
/// project again, or makes a release read a configuration that it was not built for. The annotation is
/// the one place that holds the value, so every part reads the same environment.
/// </remarks>
/// <param name="env">The Mix environment, for example <c>dev</c>, <c>test</c>, or <c>prod</c>.</param>
internal sealed class ElixirMixEnvAnnotation(string env) : IResourceAnnotation
{
    /// <summary>The Mix environment.</summary>
    public string Env { get; } = env;

    /// <summary>
    /// Reads the Mix environment of a resource.
    /// </summary>
    /// <param name="resource">The Elixir application, or the container resource that replaces it.</param>
    /// <remarks>
    /// The fallback is <c>prod</c>. A resource without the annotation is a container resource in a
    /// publish pipeline, and <c>prod</c> is the publish default.
    /// </remarks>
    public static string Resolve(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.TryGetLastAnnotation<ElixirMixEnvAnnotation>(out var annotation)
            ? annotation.Env
            : "prod";
    }
}
