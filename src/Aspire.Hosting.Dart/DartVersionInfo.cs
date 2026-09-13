// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart;

/// <summary>
/// Identifies the file that supplied the detected Dart version.
/// </summary>
internal enum DartVersionSource
{
    /// <summary>No file supplied a version. The detector used the built-in default.</summary>
    Default,

    /// <summary>A <c>.tool-versions</c> file supplied the version.</summary>
    ToolVersions,

    /// <summary>The <c>environment: sdk:</c> constraint in <c>pubspec.yaml</c> supplied the version.</summary>
    Pubspec
}

/// <summary>
/// Holds the Dart version that the detector found for an application directory.
/// </summary>
/// <param name="Version">The Dart version, for example <c>3.12.2</c>.</param>
/// <param name="Source">The file that supplied the version.</param>
internal sealed record DartVersionInfo(string Version, DartVersionSource Source);
