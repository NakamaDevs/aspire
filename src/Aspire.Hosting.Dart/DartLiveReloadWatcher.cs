// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart;

/// <summary>
/// Watches the source directories of one Dart application and reports a debounced change.
/// </summary>
/// <remarks>
/// <para>
/// The watcher looks at <c>lib</c> and <c>bin</c> below the application directory, and at
/// <c>pubspec.yaml</c> in the application directory. It accepts the Dart source extension and the
/// YAML extension, and it ignores the package cache in <c>.dart_tool</c>, the build output in
/// <c>build</c>, and every other directory whose name starts with a period.
/// </para>
/// <para>
/// An editor and a code generator both write many files in a short time, so the watcher waits for a
/// quiet period before it calls back. The callback holds the path of the last file that changed.
/// </para>
/// </remarks>
internal sealed class DartLiveReloadWatcher : IDisposable
{
    /// <summary>The file extensions that make the application restart.</summary>
    private static readonly string[] s_watchedExtensions = [".dart", ".yaml"];

    /// <summary>The directories below the application directory that the watcher looks at.</summary>
    private static readonly string[] s_watchedDirectories = ["lib", "bin"];

    /// <summary>The file in the application directory that makes the application restart.</summary>
    private const string WatchedRootFile = "pubspec.yaml";

    /// <summary>Directory names that never make the application restart, at any depth.</summary>
    /// <remarks>
    /// A name that starts with a period is ignored as well, which covers <c>.dart_tool</c>,
    /// <c>.git</c>, and the caches of the editors.
    /// </remarks>
    private static readonly string[] s_ignoredDirectories = ["build"];

    /// <summary>The default quiet period between the last change and the restart.</summary>
    private static readonly TimeSpan s_defaultDebounceInterval = TimeSpan.FromMilliseconds(500);

    private readonly string _appDirectory;
    private readonly TimeSpan _debounceInterval;
    private readonly Func<string, CancellationToken, Task> _onChanged;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly object _lock = new();

    private CancellationTokenSource? _debounceCts;
    private bool _disposed;

    public DartLiveReloadWatcher(
        string appDirectory,
        Func<string, CancellationToken, Task> onChanged,
        TimeSpan? debounceInterval = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);
        ArgumentNullException.ThrowIfNull(onChanged);

        _appDirectory = Path.GetFullPath(appDirectory);
        _onChanged = onChanged;
        _debounceInterval = debounceInterval ?? s_defaultDebounceInterval;
    }

    /// <summary>
    /// Starts one file system watcher for every watched directory that exists, and one more for
    /// <c>pubspec.yaml</c> in the application directory.
    /// </summary>
    public void Start()
    {
        foreach (var directory in s_watchedDirectories)
        {
            var path = Path.Combine(_appDirectory, directory);
            if (!Directory.Exists(path))
            {
                continue;
            }

            StartWatcher(path, includeSubdirectories: true, [.. s_watchedExtensions.Select(extension => $"*{extension}")]);
        }

        if (Directory.Exists(_appDirectory))
        {
            // A change to the manifest changes the dependency set, so the application must restart.
            // The manifest is a file in the root, so this watcher does not look at the subdirectories.
            StartWatcher(_appDirectory, includeSubdirectories: false, [WatchedRootFile]);
        }
    }

    private void StartWatcher(string path, bool includeSubdirectories, string[] filters)
    {
        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        foreach (var filter in filters)
        {
            watcher.Filters.Add(filter);
        }

        watcher.Changed += OnFileSystemEvent;
        watcher.Created += OnFileSystemEvent;
        watcher.Deleted += OnFileSystemEvent;
        watcher.Renamed += OnFileSystemEvent;

        // A watcher that cannot start must not stop the application, so ignore the failure.
        try
        {
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception)
        {
            watcher.Dispose();
            return;
        }

        _watchers.Add(watcher);
    }

    /// <summary>
    /// Reports whether a change to <paramref name="fullPath"/> must restart the application.
    /// </summary>
    /// <param name="appDirectory">The application directory, the directory that holds <c>pubspec.yaml</c>.</param>
    /// <param name="fullPath">The absolute path of the file that changed.</param>
    internal static bool ShouldReload(string appDirectory, string fullPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(fullPath);

        var extension = Path.GetExtension(fullPath);
        if (!s_watchedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(Path.GetFullPath(appDirectory), Path.GetFullPath(fullPath));
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            // The file is outside the application directory.
            return false;
        }

        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // The build output holds generated Dart files, and the package cache below .dart_tool holds
        // the sources of every dependency. The developer wrote none of them.
        if (segments.Any(segment =>
                segment.StartsWith('.') || s_ignoredDirectories.Contains(segment, StringComparer.Ordinal)))
        {
            return false;
        }

        if (segments.Length == 1)
        {
            // The manifest is the only file in the root that makes the application restart.
            return string.Equals(segments[0], WatchedRootFile, StringComparison.OrdinalIgnoreCase);
        }

        return s_watchedDirectories.Contains(segments[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// Applies the path filter and the debounce to one change.
    /// </summary>
    /// <remarks>Tests call this method directly, so they do not need a file system event.</remarks>
    internal void OnChanged(string fullPath)
    {
        if (!ShouldReload(_appDirectory, fullPath))
        {
            return;
        }

        CancellationToken token;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            // A new change replaces the pending restart, so a burst produces one restart.
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
            token = _debounceCts.Token;
        }

        _ = RestartAfterQuietPeriodAsync(fullPath, token);
    }

    private async Task RestartAfterQuietPeriodAsync(string fullPath, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounceInterval, cancellationToken).ConfigureAwait(false);
            await _onChanged(fullPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A later change or the shutdown replaced this restart.
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs args) => OnChanged(args.FullPath);

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _disposeCts.Cancel();

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();

        _debounceCts?.Dispose();
        _debounceCts = null;
        _disposeCts.Dispose();
    }
}
