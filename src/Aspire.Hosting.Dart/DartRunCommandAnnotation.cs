// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds a complete command line that replaces <c>dart run &lt;entrypoint&gt;</c>.
/// </summary>
/// <remarks>
/// A Dart web framework can supply its own command-line tool, for example <c>dart_frog dev</c> or
/// <c>flutter run -d web-server</c>. The annotation holds that command and its arguments.
/// </remarks>
/// <param name="command">The command that starts the application.</param>
/// <param name="args">The arguments of the command.</param>
internal sealed class DartRunCommandAnnotation(string command, object[] args) : IResourceAnnotation
{
    /// <summary>The command that starts the application.</summary>
    public string Command { get; } = command;

    /// <summary>The arguments of the command.</summary>
    public object[] Args { get; } = args;
}
