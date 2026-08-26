// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;

#pragma warning disable ASPIREEXTENSION001 // Launch configuration types are experimental.

namespace Aspire.Hosting.Elixir;

/// <summary>
/// The launch configuration that an IDE uses to start an Elixir application under the debugger.
/// </summary>
/// <remarks>
/// The fields follow the ElixirLS <c>mix_task</c> debug adapter. That adapter starts the debug
/// session itself with <c>mix &lt;task&gt; &lt;taskArgs&gt;</c> in <c>projectDir</c>, so Aspire does not
/// give it the <c>mix</c> command. See https://github.com/elixir-lsp/elixir-ls for the adapter.
/// </remarks>
internal sealed class ElixirLaunchConfiguration() : ExecutableLaunchConfiguration("elixir")
{
    /// <summary>
    /// The absolute path to the Mix project directory, the directory that holds <c>mix.exs</c>.
    /// Corresponds to the <c>projectDir</c> field of the ElixirLS <c>mix_task</c> configuration.
    /// </summary>
    [JsonPropertyName("project_dir")]
    public string ProjectDir { get; set; } = string.Empty;

    /// <summary>
    /// The Mix task to run, for example <c>run</c> or <c>phx.server</c>.
    /// Corresponds to the <c>task</c> field of the ElixirLS <c>mix_task</c> configuration.
    /// </summary>
    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;

    /// <summary>
    /// The arguments for the Mix task. The application arguments follow a <c>--</c> separator, which
    /// matches the command line that Aspire builds for process execution.
    /// Corresponds to the <c>taskArgs</c> field of the ElixirLS <c>mix_task</c> configuration.
    /// </summary>
    [JsonPropertyName("task_args")]
    public string[] TaskArgs { get; set; } = [];

    /// <summary>
    /// The Mix environment, the resolved value of <c>MIX_ENV</c>. The value is <see langword="null"/>
    /// when the environment is not resolved, and the field is then absent from the JSON.
    /// </summary>
    [JsonPropertyName("mix_env")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MixEnv { get; set; }

    /// <summary>
    /// The working directory for the debug session. It is the resource working directory, so
    /// <c>WithWorkingDirectory</c> changes it.
    /// </summary>
    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = string.Empty;
}
