// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dart.Tests;

public class DartLiveReloadTests(ITestOutputHelper outputHelper)
{
    // ---- WithLiveReload --------------------------------------------------------

    [Fact]
    public void WithLiveReload_AddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddJasprApp("web", builder.AppHostDirectory)
            .WithLiveReload();

        Assert.Single(app.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_IsDefaultForAddDartApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddDartApp("api", builder.AppHostDirectory);

        Assert.Single(app.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_IsDefaultForAddServerpodApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddServerpodApp("api", builder.AppHostDirectory);

        Assert.Single(app.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_IsOffByDefaultForAddJasprApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // `jaspr serve` holds its own file watcher, so an Aspire restart would stop that work.
        var app = builder.AddJasprApp("web", builder.AppHostDirectory);

        Assert.Empty(app.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_NotAppliedInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var dart = builder.AddDartApp("api", builder.AppHostDirectory);
        var jaspr = builder.AddJasprApp("web", builder.AppHostDirectory)
            .WithLiveReload();

        Assert.Empty(dart.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
        Assert.Empty(jaspr.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_FalseRemovesAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var app = builder.AddDartApp("api", builder.AppHostDirectory)
            .WithLiveReload(false);

        Assert.Empty(app.Resource.Annotations.OfType<DartLiveReloadAnnotation>());
    }

    [Fact]
    public void WithLiveReload_ThrowsOnNullBuilder()
    {
        IResourceBuilder<DartAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => builder.WithLiveReload());

        Assert.Equal(nameof(builder), exception.ParamName);
    }

    // ---- The watcher -----------------------------------------------------------

    [Fact]
    public void LiveReloadWatcher_IgnoresDartToolAndBuild()
    {
        var appDirectory = Path.Combine(Path.GetTempPath(), "dart-app");

        // The source files that the developer writes must restart the application.
        Assert.True(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "api.dart")));
        Assert.True(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "src", "routes.dart")));
        Assert.True(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "bin", "main.dart")));

        // The manifest changes the dependency set, so it must restart the application as well.
        Assert.True(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "pubspec.yaml")));

        // The package cache and the build output hold Dart files that the developer did not write.
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, ".dart_tool", "package_config.dart")));
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "build", "web", "main.dart")));
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "build", "generated.dart")));
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, ".git", "lib", "api.dart")));

        // A different extension, a file outside a watched directory, and a file outside the
        // application directory must not restart the application either.
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "lib", "README.md")));
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(appDirectory, "analysis_options.yaml")));
        Assert.False(DartLiveReloadWatcher.ShouldReload(appDirectory, Path.Combine(Path.GetTempPath(), "other", "lib", "api.dart")));
    }

    [Fact]
    public async Task LiveReloadWatcher_DebouncesBurstOfChanges()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appDirectory = workspace.CreateDirectory("app").FullName;

        var restarts = 0;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var watcher = new DartLiveReloadWatcher(
            appDirectory,
            (_, _) =>
            {
                Interlocked.Increment(ref restarts);
                completed.TrySetResult();
                return Task.CompletedTask;
            },
            debounceInterval: TimeSpan.FromMilliseconds(200));

        // A code generator writes many files at one time, so the burst must produce one restart.
        for (var i = 0; i < 10; i++)
        {
            watcher.OnChanged(Path.Combine(appDirectory, "lib", $"model_{i}.dart"));
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

        using var watcher = new DartLiveReloadWatcher(
            appDirectory,
            (path, _) =>
            {
                changedFile.TrySetResult(path);
                return Task.CompletedTask;
            },
            debounceInterval: TimeSpan.FromMilliseconds(100));

        watcher.Start();

        var sourceFile = Path.Combine(libDirectory.FullName, "api.dart");

        // The file system watcher can start after the first write, so write until it reports.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!changedFile.Task.IsCompleted && DateTime.UtcNow < deadline)
        {
            await File.WriteAllTextAsync(sourceFile, $"// {Guid.NewGuid()}\nvoid main() {{}}\n");
            await Task.WhenAny(changedFile.Task, Task.Delay(TimeSpan.FromMilliseconds(250)));
        }

        Assert.Equal(sourceFile, await changedFile.Task.WaitAsync(TimeSpan.FromSeconds(30)));
    }
}
