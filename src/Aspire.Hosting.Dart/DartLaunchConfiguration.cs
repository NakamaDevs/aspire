// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;

#pragma warning disable ASPIREEXTENSION001 // Launch configuration types are experimental.

namespace Aspire.Hosting.Dart;

/// <summary>
/// The launch configuration that an IDE uses to start a Dart application under the debugger.
/// </summary>
/// <remarks>
/// <para>
/// The fields follow the Dart-Code <c>dart</c> debug adapter. That adapter starts the debug session
/// itself with <c>dart run &lt;toolArgs&gt; &lt;program&gt; &lt;args&gt;</c> in <c>cwd</c>, so Aspire does not
/// give it the <c>dart</c> command. See https://github.com/Dart-Code/Dart-Code for the adapter.
/// </para>
/// <para>
/// The names use snake case, and the extension renames <c>tool_args</c> to the <c>toolArgs</c> field
/// of Dart-Code. Every other name already matches.
/// </para>
/// </remarks>
internal sealed class DartLaunchConfiguration() : ExecutableLaunchConfiguration("dart")
{
    /// <summary>
    /// The absolute path of the Dart file that starts the application, for example
    /// <c>/src/api/bin/main.dart</c>. Corresponds to the <c>program</c> field of Dart-Code.
    /// </summary>
    [JsonPropertyName("program")]
    public string Program { get; set; } = string.Empty;

    /// <summary>
    /// The absolute path of the directory that holds <c>pubspec.yaml</c>. Dart-Code resolves the
    /// package from this directory. Corresponds to the <c>cwd</c> field of Dart-Code.
    /// </summary>
    [JsonPropertyName("cwd")]
    public string Cwd { get; set; } = string.Empty;

    /// <summary>
    /// The arguments of the program. <c>dart run</c> gives every argument after the file name to the
    /// program. Corresponds to the <c>args</c> field of Dart-Code.
    /// </summary>
    [JsonPropertyName("args")]
    public string[] Args { get; set; } = [];

    /// <summary>
    /// The options of <c>dart run</c> itself: the <c>--define</c> options, the options of
    /// <c>WithDartRunArgs</c>, and <c>--enable-vm-service</c>. Corresponds to the <c>toolArgs</c>
    /// field of Dart-Code.
    /// </summary>
    [JsonPropertyName("tool_args")]
    public string[] ToolArgs { get; set; } = [];

    /// <summary>
    /// The working directory for the debug session. It is the resource working directory, so
    /// <c>WithWorkingDirectory</c> changes it.
    /// </summary>
    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = string.Empty;
}
