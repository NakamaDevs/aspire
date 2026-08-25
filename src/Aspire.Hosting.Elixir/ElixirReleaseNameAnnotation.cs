// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Holds the Mix release name that the generated Dockerfile builds and starts.
/// </summary>
/// <param name="ReleaseName">The release name, for example <c>my_app</c>.</param>
internal sealed record ElixirReleaseNameAnnotation(string ReleaseName) : IResourceAnnotation;
