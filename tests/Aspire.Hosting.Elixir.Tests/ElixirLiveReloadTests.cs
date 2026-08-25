// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Elixir.Tests;

public class ElixirLiveReloadTests(ITestOutputHelper outputHelper)
{
    // ---- WithLiveReload --------------------------------------------------------

    [Fact]
    public void WithLiveReload_AddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory)
            .WithLiveReload();

        Assert.Single(app.Resource.Annotations.OfType<ElixirLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_IsDefaultForAddElixirApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddElixirApp("api", builder.AppHostDirectory);

        Assert.Single(app.Resource.Annotations.OfType<ElixirLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_IsOffByDefaultForAddPhoenixApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddPhoenixApp("web", builder.AppHostDirectory);

        Assert.Empty(app.Resource.Annotations.OfType<ElixirLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_NotAppliedInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var elixir = builder.AddElixirApp("api", builder.AppHostDirectory);
        var phoenix = builder.AddPhoenixApp("web", builder.AppHostDirectory)
            .WithLiveReload();

        Assert.Empty(elixir.Resource.Annotations.OfType<ElixirLiveReloadAnnotation>());
        Assert.Empty(phoenix.Resource.Annotations.OfType<ElixirLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_FalseRemovesAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddElixirApp("api", builder.AppHostDirectory)
            .WithLiveReload(false);

        Assert.Empty(app.Resource.Annotations.OfType<ElixirLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_ThrowsOnNullBuilder()
    {
        IResourceBuilder<ElixirAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => builder.WithLiveReload());

        Assert.Equal(nameof(builder), exception.ParamName);
    }

    // ---- The watcher -----------------------------------------------------------

    [Fact]
    public void LiveReloadWatcher_IgnoresBuildAndDepsDirectories()
    {
        var appDirectory = Path.Combine(Path.GetTempPath(), "elixir-app");

        // The source files that the developer writes must restart the application.
        Assert.True(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "api.ex")));
        Assert.True(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "api_web", "page.heex")));
        Assert.True(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "mailer.eex")));
        Assert.True(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "config", "dev.exs")));

        // The build output, the dependency sources, and the language server cache must not.
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "_build", "dev", "lib", "api.ex")));
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "deps", "phoenix", "lib", "phoenix.ex")));
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, ".elixir_ls", "build", "lib", "api.ex")));
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "_build", "api.ex")));

        // A different extension, a file outside a watched directory, and a file outside the
        // application directory must not restart the application either.
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "README.md")));
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "mix.exs")));
        Assert.False(ElixirLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(Path.GetTempPath(), "other", "lib", "api.ex")));
    }

    [Fact]
    public async Task LiveReloadWatcher_DebouncesBurstOfChanges()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appDirectory = workspace.CreateDirectory("app").FullName;

        var restarts = 0;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var watcher = new ElixirLiveReloadWatcher(
            appDirectory,
            (_, _) =>
            {
                Interlocked.Increment(ref restarts);
                completed.TrySetResult();
                return Task.CompletedTask;
            },
            debounceInterval: TimeSpan.FromMilliseconds(200));

        // A compiler writes many files at one time, so the burst must produce one restart.
        for (var i = 0; i < 10; i++)
        {
            watcher.OnChanged(Path.Combine(appDirectory, "lib", $"module_{i}.ex"));
        }

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Wait for more than one debounce interval, so a second restart would be visible.
        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.Equal(1, restarts);
    }

    [Fact]
    public async Task LiveReloadWatcher_TriggersRestartOnLibChange()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appDirectory = workspace.CreateDirectory("app").FullName;
        var libDirectory = Directory.CreateDirectory(Path.Combine(appDirectory, "lib"));

        var changedFile = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var watcher = new ElixirLiveReloadWatcher(
            appDirectory,
            (path, _) =>
            {
                changedFile.TrySetResult(path);
                return Task.CompletedTask;
            },
            debounceInterval: TimeSpan.FromMilliseconds(100));

        watcher.Start();

        var sourceFile = Path.Combine(libDirectory.FullName, "api.ex");

        // The file system watcher can start after the first write, so write until it reports.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!changedFile.Task.IsCompleted && DateTime.UtcNow < deadline)
        {
            await File.WriteAllTextAsync(sourceFile, $"defmodule Api do\n  # {Guid.NewGuid()}\nend\n");
            await Task.WhenAny(changedFile.Task, Task.Delay(TimeSpan.FromMilliseconds(250)));
        }

        Assert.Equal(sourceFile, await changedFile.Task.WaitAsync(TimeSpan.FromSeconds(30)));
    }
}
