// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the options of <c>dart run</c> itself, for example <c>--enable-asserts</c>.
/// </summary>
/// <remarks>
/// The options come before the entrypoint, because <c>dart run</c> gives every argument after the
/// entrypoint to the program.
/// </remarks>
internal sealed class DartRunArgsAnnotation(string[] args) : IResourceAnnotation
{
    /// <summary>The options of <c>dart run</c>.</summary>
    public string[] Args { get; } = args;
}
