// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Detects the Elixir and Erlang/OTP versions that an application directory asks for.
/// </summary>
/// <remarks>
/// The detector reads <c>.tool-versions</c> first, because that file pins the exact toolchain.
/// It then reads the <c>elixir:</c> requirement in <c>mix.exs</c>. If neither file gives a
/// version, the detector returns <see cref="DefaultElixirVersion"/> and <see cref="DefaultOtpVersion"/>.
/// </remarks>
internal static partial class ElixirVersionDetector
{
    /// <summary>The Elixir version to use when no file gives one.</summary>
    public const string DefaultElixirVersion = "1.19.5";

    /// <summary>The Erlang/OTP version to use when no file gives one.</summary>
    public const string DefaultOtpVersion = "28.4.1";

    /// <summary>
    /// Detects the Elixir and OTP versions for <paramref name="appDirectory"/>.
    /// </summary>
    /// <param name="appDirectory">The directory that contains <c>mix.exs</c>.</param>
    /// <returns>The detected versions and the file that supplied them.</returns>
    public static ElixirVersionInfo Detect(string appDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);

        if (TryDetectFromToolVersions(appDirectory, out var toolVersions))
        {
            return toolVersions;
        }

        if (TryDetectFromMixExs(appDirectory, out var mixExs))
        {
            return mixExs;
        }

        return new ElixirVersionInfo(DefaultElixirVersion, DefaultOtpVersion, ElixirVersionSource.Default);
    }

    private static bool TryDetectFromToolVersions(string appDirectory, out ElixirVersionInfo info)
    {
        info = null!;

        // asdf and mise look for the nearest .tool-versions, then continue up the tree.
        var directory = new DirectoryInfo(appDirectory);
        while (directory is not null)
        {
            var toolVersionsPath = Path.Combine(directory.FullName, ".tool-versions");
            if (File.Exists(toolVersionsPath) && TryParseToolVersions(toolVersionsPath, out info))
            {
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    private static bool TryParseToolVersions(string toolVersionsPath, out ElixirVersionInfo info)
    {
        info = null!;

        string? elixirVersion = null;
        string? otpFromElixirEntry = null;
        string? erlangVersion = null;

        foreach (var rawLine in File.ReadLines(toolVersionsPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            if (elixirVersion is null && string.Equals(parts[0], "elixir", StringComparison.OrdinalIgnoreCase))
            {
                // The Elixir entry may carry an OTP suffix, for example `1.19.5-otp-28`.
                var match = ElixirEntryRegex().Match(parts[1]);
                if (match.Success)
                {
                    elixirVersion = match.Groups["version"].Value;
                    otpFromElixirEntry = match.Groups["otp"].Success ? match.Groups["otp"].Value : null;
                }
            }
            else if (erlangVersion is null && string.Equals(parts[0], "erlang", StringComparison.OrdinalIgnoreCase))
            {
                var match = VersionRegex().Match(parts[1]);
                if (match.Success)
                {
                    erlangVersion = match.Value;
                }
            }
        }

        if (elixirVersion is null && erlangVersion is null)
        {
            return false;
        }

        // The erlang entry gives the complete OTP version, so prefer it over the `-otp-NN` suffix.
        var otpVersion = erlangVersion ?? otpFromElixirEntry ?? DefaultOtpVersion;

        info = new ElixirVersionInfo(elixirVersion ?? DefaultElixirVersion, otpVersion, ElixirVersionSource.ToolVersions);
        return true;
    }

    private static bool TryDetectFromMixExs(string appDirectory, out ElixirVersionInfo info)
    {
        info = null!;

        var mixExsPath = Path.Combine(appDirectory, "mix.exs");
        if (!File.Exists(mixExsPath))
        {
            return false;
        }

        foreach (var line in File.ReadLines(mixExsPath))
        {
            var requirementMatch = MixElixirRequirementRegex().Match(line);
            if (!requirementMatch.Success)
            {
                continue;
            }

            // The requirement holds an operator and a version, for example `~> 1.19` or `>= 1.18.0`.
            var versionMatch = VersionRegex().Match(requirementMatch.Groups["requirement"].Value);
            if (!versionMatch.Success)
            {
                continue;
            }

            info = new ElixirVersionInfo(NormalizeVersion(versionMatch.Value), DefaultOtpVersion, ElixirVersionSource.MixExs);
            return true;
        }

        return false;
    }

    private static string NormalizeVersion(string version)
    {
        // A Mix requirement can omit the patch part. Add the missing parts so the value is a full version.
        var parts = version.Split('.').Length;
        return parts switch
        {
            1 => $"{version}.0.0",
            2 => $"{version}.0",
            _ => version
        };
    }

    // Matches: 1.19.5-otp-28  or  1.19.5
    [GeneratedRegex(@"^(?<version>\d+(?:\.\d+)*)(?:-otp-(?<otp>\d+))?$")]
    private static partial Regex ElixirEntryRegex();

    // Matches the first version number in a string.
    [GeneratedRegex(@"\d+(?:\.\d+)*")]
    private static partial Regex VersionRegex();

    // Matches: elixir: "~> 1.19"
    [GeneratedRegex(@"elixir:\s*""(?<requirement>[^""]*)""")]
    private static partial Regex MixElixirRequirementRegex();
}
