// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart.Tests;

/// <summary>
/// Prepares one <c>dart:io</c> HTTP application, so every test in the class starts from a resolved
/// package.
/// </summary>
/// <remarks>
/// <para>
/// A <c>dart pub get</c> is the slowest part of a Dart test. The fixture pays that cost one time.
/// <see cref="CreateApp"/> then copies the prepared package, including <c>pubspec.lock</c> and
/// <c>.dart_tool</c>.
/// </para>
/// <para>
/// The copy also isolates the tests. A test that writes to <c>lib</c> to exercise live reload cannot
/// change the source that another test reads.
/// </para>
/// </remarks>
public sealed class DartServerAppFixture : IAsyncLifetime
{
    private TempDartAppDirectory? _template;

    public ValueTask InitializeAsync()
    {
        var template = TempDartAppDirectory.CreateServerApp();

        // The package declares no dependency, so this call needs no network access. It writes
        // pubspec.lock and .dart_tool/package_config.json, which `dart run` reads.
        template.RunDart("pub", "get");

        _template = template;
        return ValueTask.CompletedTask;
    }

    /// <summary>Copies the prepared package into a directory that one test owns.</summary>
    public TempDartAppDirectory CreateApp()
    {
        var template = _template
            ?? throw new InvalidOperationException("The fixture is not initialized.");

        return TempDartAppDirectory.CreateCopyOf(template);
    }

    public ValueTask DisposeAsync()
    {
        _template?.Dispose();
        return ValueTask.CompletedTask;
    }
}
