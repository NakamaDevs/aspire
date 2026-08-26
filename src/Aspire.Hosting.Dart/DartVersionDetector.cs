// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Detects the Dart version that an application directory asks for.
/// </summary>
/// <remarks>
/// <para>
/// The detector reads <c>.tool-versions</c> first, because that file pins the exact toolchain. It
/// then reads the <c>sdk:</c> constraint in the <c>environment:</c> block of <c>pubspec.yaml</c>. If
/// neither file gives a version, the detector returns <see cref="DefaultDartVersion"/>.
/// </para>
/// <para>
/// A <c>.tool-versions</c> file can pin Flutter instead of Dart, for example
/// <c>flutter 3.44.8-stable</c>. Each Flutter release carries one Dart version, but the file does not
/// give it, and a version table would go out of date. The detector therefore ignores a <c>flutter</c>
/// entry and continues with <c>pubspec.yaml</c> and then the default.
/// </para>
/// </remarks>
internal static partial class DartVersionDetector
{
    /// <summary>The Dart version to use when no file gives one.</summary>
    public const string DefaultDartVersion = "3.12.2";

    /// <summary>
    /// Detects the Dart version for <paramref name="appDirectory"/>.
    /// </summary>
    /// <param name="appDirectory">The directory that contains <c>pubspec.yaml</c>.</param>
    /// <returns>The detected version and the file that supplied it.</returns>
    public static DartVersionInfo Detect(string appDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);

        if (TryDetectFromToolVersions(appDirectory, out var toolVersions))
        {
            return toolVersions;
        }

        if (TryDetectFromPubspec(appDirectory, out var pubspec))
        {
            return pubspec;
        }

        return new DartVersionInfo(DefaultDartVersion, DartVersionSource.Default);
    }

    private static bool TryDetectFromToolVersions(string appDirectory, out DartVersionInfo info)
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

    private static bool TryParseToolVersions(string toolVersionsPath, out DartVersionInfo info)
    {
        info = null!;

        foreach (var rawLine in File.ReadLines(toolVersionsPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !string.Equals(parts[0], "dart", StringComparison.OrdinalIgnoreCase))
            {
                // A `flutter` entry names no Dart version, so the detector passes over it.
                continue;
            }

            var match = VersionRegex().Match(parts[1]);
            if (!match.Success)
            {
                continue;
            }

            info = new DartVersionInfo(NormalizeVersion(match.Value), DartVersionSource.ToolVersions);
            return true;
        }

        return false;
    }

    private static bool TryDetectFromPubspec(string appDirectory, out DartVersionInfo info)
    {
        info = null!;

        var pubspecPath = Path.Combine(appDirectory, "pubspec.yaml");
        if (!File.Exists(pubspecPath))
        {
            return false;
        }

        // The `sdk:` key appears in the `environment:` block and also below a package that comes from
        // the Flutter SDK. Only the value in the `environment:` block is a Dart version, so the reader
        // follows the indentation of the block instead of the first match in the file.
        var inEnvironment = false;

        foreach (var rawLine in File.ReadLines(pubspecPath))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                // A key at the left margin ends the block that came before it.
                inEnvironment = EnvironmentKeyRegex().IsMatch(line);
                continue;
            }

            if (!inEnvironment)
            {
                continue;
            }

            var sdkMatch = SdkConstraintRegex().Match(line);
            if (!sdkMatch.Success)
            {
                continue;
            }

            // The constraint holds an operator and a version, for example `^3.10.0` or
            // `'>=3.10.0 <4.0.0'`. The first version in the value is the lower bound.
            var versionMatch = VersionRegex().Match(sdkMatch.Groups["constraint"].Value);
            if (!versionMatch.Success)
            {
                continue;
            }

            info = new DartVersionInfo(NormalizeVersion(versionMatch.Value), DartVersionSource.Pubspec);
            return true;
        }

        return false;
    }

    private static string NormalizeVersion(string version)
    {
        // A pub constraint can omit the patch part. Add the missing parts so the value is a full version.
        var parts = version.Split('.').Length;
        return parts switch
        {
            1 => $"{version}.0.0",
            2 => $"{version}.0",
            _ => version
        };
    }

    // Matches the first version number in a string.
    [GeneratedRegex(@"\d+(?:\.\d+)*")]
    private static partial Regex VersionRegex();

    // Matches the `environment:` key at the left margin.
    [GeneratedRegex(@"^environment\s*:")]
    private static partial Regex EnvironmentKeyRegex();

    // Matches: sdk: ^3.10.0  or  sdk: '>=3.10.0 <4.0.0'
    [GeneratedRegex(@"^\s+sdk\s*:\s*(?<constraint>.+)$")]
    private static partial Regex SdkConstraintRegex();
}
