// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Elixir.Tests;

/// <summary>
/// Prepares one Bandit and Plug application, so every test in the class starts from compiled code.
/// </summary>
/// <remarks>
/// <para>
/// A Hex fetch and a first compilation are the slowest part of an Elixir test. The fixture pays that
/// cost one time. <see cref="CreateApp"/> then copies the prepared project, including <c>deps</c> and
/// <c>_build</c>, so Mix compiles the application module only.
/// </para>
/// <para>
/// The copy also isolates the tests. A test that writes to <c>lib</c> to exercise live reload cannot
/// change the source that another test reads.
/// </para>
/// </remarks>
public sealed class ElixirServerAppFixture : IAsyncLifetime
{
    private TempElixirAppDirectory? _template;

    public ValueTask InitializeAsync()
    {
        var template = TempElixirAppDirectory.CreateServerApp();

        // `mix deps.get` needs the network one time. After it, the lock file and the deps directory
        // make every later call work without network access.
        template.RunMix("deps.get");
        template.RunMix("compile");

        _template = template;
        return ValueTask.CompletedTask;
    }

    /// <summary>Copies the prepared project into a directory that one test owns.</summary>
    public TempElixirAppDirectory CreateApp()
    {
        var template = _template
            ?? throw new InvalidOperationException("The fixture is not initialized.");

        return TempElixirAppDirectory.CreateCopyOf(template);
    }

    public ValueTask DisposeAsync()
    {
        _template?.Dispose();
        return ValueTask.CompletedTask;
    }
}
