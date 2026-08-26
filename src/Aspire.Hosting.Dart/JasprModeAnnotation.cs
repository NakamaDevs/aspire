// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the rendering mode of a Jaspr application.
/// </summary>
/// <remarks>
/// <c>AddJasprApp</c> reads the mode from the <c>jaspr</c> block of <c>pubspec.yaml</c>. When the
/// file holds no mode, the annotation holds <see cref="JasprMode.Static"/>, which is the Jaspr
/// default.
/// </remarks>
internal sealed class JasprModeAnnotation(JasprMode mode) : IResourceAnnotation
{
    /// <summary>The rendering mode of the application.</summary>
    public JasprMode Mode { get; } = mode;
}
