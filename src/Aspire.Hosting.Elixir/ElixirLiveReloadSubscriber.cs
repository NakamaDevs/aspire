// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Restarts every Elixir application that asked for live reload when its source files change.
/// </summary>
/// <remarks>
/// The subscriber starts the watchers after Aspire creates the resources, because the lifecycle
/// commands, and therefore the restart command, exist only from that point. It runs in run mode only.
/// </remarks>
internal sealed class ElixirLiveReloadSubscriber(
    ResourceCommandService commandService,
    ResourceLoggerService loggerService) : IDistributedApplicationEventingSubscriber, IDisposable
{
    private readonly ConcurrentDictionary<string, ElixirLiveReloadWatcher> _watchers = new();
    private bool _disposed;

    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing,
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventing);
        ArgumentNullException.ThrowIfNull(executionContext);

        if (!executionContext.IsRunMode)
        {
            return Task.CompletedTask;
        }

        eventing.Subscribe<AfterResourcesCreatedEvent>(OnAfterResourcesCreated);
        return Task.CompletedTask;
    }

    private Task OnAfterResourcesCreated(AfterResourcesCreatedEvent @event, CancellationToken cancellationToken)
    {
        foreach (var resource in @event.Model.Resources.OfType<ElixirAppResource>())
        {
            if (!resource.HasAnnotationOfType<ElixirLiveReloadAnnotation>())
            {
                continue;
            }

            StartWatcher(resource);
        }

        return Task.CompletedTask;
    }

    private void StartWatcher(ElixirAppResource resource)
    {
        if (_disposed || _watchers.ContainsKey(resource.Name))
        {
            return;
        }

        var watcher = new ElixirLiveReloadWatcher(
            resource.WorkingDirectory,
            (path, token) => RestartAsync(resource, path, token));

        if (!_watchers.TryAdd(resource.Name, watcher))
        {
            watcher.Dispose();
            return;
        }

        watcher.Start();
    }

    private async Task RestartAsync(ElixirAppResource resource, string changedFile, CancellationToken cancellationToken)
    {
        var logger = loggerService.GetLogger(resource);

        logger.LogInformation("Restarting {Name}: {File} changed", resource.Name, changedFile);

        var result = await commandService
            .ExecuteCommandAsync(resource, KnownResourceCommands.RestartCommand, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            // A restart fails when the resource is already starting or stopping. The next change
            // starts a new restart, so the failure must not stop the watcher.
            logger.LogWarning("The restart of {Name} did not complete: {Message}", resource.Name, result.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
    }
}
