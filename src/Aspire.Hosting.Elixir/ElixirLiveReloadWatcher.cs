// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Watches the source directories of one Elixir application and reports a debounced change.
/// </summary>
/// <remarks>
/// <para>
/// The watcher looks at <c>lib</c> and <c>config</c> below the application directory. It accepts the
/// Elixir source extensions only, and it ignores the build output in <c>_build</c>, the dependency
/// sources in <c>deps</c>, and the language server cache in <c>.elixir_ls</c>.
/// </para>
/// <para>
/// A compiler writes many files in a short time, so the watcher waits for a quiet period before it
/// calls back. The callback holds the path of the last file that changed.
/// </para>
/// </remarks>
internal sealed class ElixirLiveReloadWatcher : IDisposable
{
    /// <summary>The file extensions that make the application restart.</summary>
    private static readonly string[] s_watchedExtensions = [".ex", ".exs", ".heex", ".eex"];

    /// <summary>The directories below the application directory that the watcher looks at.</summary>
    private static readonly string[] s_watchedDirectories = ["lib", "config"];

    /// <summary>Directory names that never make the application restart, at any depth.</summary>
    private static readonly string[] s_ignoredDirectories = ["_build", "deps", ".elixir_ls"];

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

    public ElixirLiveReloadWatcher(
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
    /// Starts one file system watcher for every watched directory that exists.
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

            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            foreach (var extension in s_watchedExtensions)
            {
                watcher.Filters.Add($"*{extension}");
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
                continue;
            }

            _watchers.Add(watcher);
        }
    }

    /// <summary>
    /// Reports whether a change to <paramref name="fullPath"/> must restart the application.
    /// </summary>
    /// <param name="appDirectory">The application directory, the directory that holds <c>mix.exs</c>.</param>
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
        if (segments.Length < 2)
        {
            // Only files below a watched directory count, so a file in the root does not.
            return false;
        }

        // The build output, the dependency sources, and the language server cache hold Elixir files
        // that the developer did not write, so a change there must not restart the application.
        if (segments.Any(segment => s_ignoredDirectories.Contains(segment, StringComparer.Ordinal)))
        {
            return false;
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
