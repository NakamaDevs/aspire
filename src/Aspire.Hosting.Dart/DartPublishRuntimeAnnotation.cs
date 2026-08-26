// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Collects the arguments and the environment variables that the generated Dockerfile gives to the
/// compiled application.
/// </summary>
internal sealed class DartPublishRuntimeContext
{
    /// <summary>The arguments that follow the executable in the <c>CMD</c> statement.</summary>
    public List<string> Args { get; } = [];

    /// <summary>The environment variables that the runtime stage sets.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = [];
}

/// <summary>
/// Supplies the arguments and the environment variables of the published image.
/// </summary>
/// <remarks>
/// A preset such as <c>AddServerpodApp</c> adds this annotation, so
/// <see cref="DartDockerfileGenerator"/> stays framework-agnostic. The callback runs when Aspire
/// generates the Dockerfile, so it reads the values that the developer set after the preset.
/// </remarks>
/// <param name="Configure">Writes the arguments and the environment variables for one resource.</param>
internal sealed record DartPublishRuntimeAnnotation(
    Action<IResource, DartPublishRuntimeContext> Configure) : IResourceAnnotation;
