// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Identifies the file that supplied the detected Elixir and OTP versions.
/// </summary>
internal enum ElixirVersionSource
{
    /// <summary>No file supplied a version. The detector used the built-in defaults.</summary>
    Default,

    /// <summary>A <c>.tool-versions</c> file supplied the versions.</summary>
    ToolVersions,

    /// <summary>The <c>elixir:</c> requirement in <c>mix.exs</c> supplied the Elixir version.</summary>
    MixExs
}

/// <summary>
/// Holds the Elixir and OTP versions that the detector found for an application directory.
/// </summary>
/// <param name="ElixirVersion">The Elixir version, for example <c>1.19.5</c>.</param>
/// <param name="OtpVersion">The Erlang/OTP version, for example <c>28.4.1</c> or <c>28</c>.</param>
/// <param name="Source">The file that supplied the versions.</param>
internal sealed record ElixirVersionInfo(string ElixirVersion, string OtpVersion, ElixirVersionSource Source);
