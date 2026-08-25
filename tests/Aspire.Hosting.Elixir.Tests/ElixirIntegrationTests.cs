// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;

namespace Aspire.Hosting.Elixir.Tests;

/// <summary>
/// Starts real Elixir applications with the <c>mix</c> command that the machine supplies.
/// </summary>
/// <remarks>
/// <para>
/// The other test classes assert the application model. This class asserts the runtime behaviour: the
/// process starts, the setup siblings run in order, the environment reaches the application, the HTTP
/// endpoint answers, and the logs reach the dashboard.
/// </para>
/// <para>
/// The class fixture fetches and compiles the Hex dependencies one time. Each test then copies the
/// prepared project, so one test cannot change the source that another test reads.
/// </para>
/// </remarks>
[RequiresTools(["mix", "elixir"])]
public class ElixirIntegrationTests(ElixirServerAppFixture serverApp, ITestOutputHelper outputHelper)
    : IClassFixture<ElixirServerAppFixture>
{
    /// <summary>The time that a Mix start is allowed to take before a test fails.</summary>
    private static readonly TimeSpan s_startTimeout =
        TimeSpan.FromSeconds(PlatformDetection.IsRunningOnCI ? 300 : 120);

    [Fact]
    public async Task ElixirResourceFinishesSuccessfully()
    {
        using var consoleApp = TempElixirAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddElixirApp("api", consoleApp.Path);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        var finished = await app.ResourceNotifications.WaitForResourceAsync(
            "api", e => e.Snapshot.State?.Text == KnownResourceStates.Finished, cts.Token);

        // `mix run --no-halt` reports the exit code of the virtual machine, and the application asks
        // for zero.
        Assert.Equal(0, finished.Snapshot.ExitCode);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task MixDepsSiblingRunsBeforeApp()
    {
        using var consoleApp = TempElixirAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddElixirApp("api", consoleApp.Path);

        using var app = builder.Build();

        // The watch starts before the application, so the stream holds every transition in the order
        // that the orchestrator reported it. It ends when both transitions arrive, or when the test
        // cancels its token after the application stops.
        var order = new List<string>();
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        var watchTask = WatchStartOrderAsync(app, order, watchCts.Token);

        await app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(
            "api", e => e.Snapshot.State?.Text == KnownResourceStates.Finished, cts.Token);

        await app.StopAsync(cts.Token);

        watchCts.Cancel();
        await watchTask;

        lock (order)
        {
            Assert.Equal(["api-mix-deps", "api"], order);
        }

        // Records the first interesting transition of each resource, in the order the stream reports it.
        static async Task WatchStartOrderAsync(
            DistributedApplication app, List<string> order, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(cancellationToken))
                {
                    var name = resourceEvent.Resource.Name;
                    var state = resourceEvent.Snapshot.State?.Text;

                    var interesting = name switch
                    {
                        // `mix deps.get` is a step, so its interesting transition is the completion.
                        "api-mix-deps" => state == KnownResourceStates.Finished,

                        // The application must not leave the waiting state before that completion.
                        "api" => state == KnownResourceStates.Running || state == KnownResourceStates.Finished,
                        _ => false
                    };

                    if (!interesting)
                    {
                        continue;
                    }

                    lock (order)
                    {
                        if (!order.Contains(name))
                        {
                            order.Add(name);
                        }

                        if (order.Count == 2)
                        {
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The application stopped before both transitions arrived. The assertion reports it.
            }
        }
    }

    [Fact]
    public async Task ElixirAppLogsAppearInResourceLogs()
    {
        using var consoleApp = TempElixirAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddElixirApp("api", consoleApp.Path);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        // The helper reads the same ResourceLoggerService stream that the dashboard reads.
        await app.WaitForTextAsync(TempElixirAppDirectory.StartupMarker, "api", cts.Token);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ElixirAppReceivesReferencedConnectionString()
    {
        const string ConnectionString = "ecto://postgres:secret@localhost:5432/appdb";

        using var serverAppDirectory = serverApp.CreateApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var database = builder.AddConnectionString("db", ReferenceExpression.Create($"{ConnectionString}"));
        builder.AddElixirApp("api", serverAppDirectory.Path)
            .WithHttpEndpoint(env: "PORT")
            .WithReference(database);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);
        await WaitForServerAsync(app, "api", cts.Token);

        using var client = app.CreateHttpClient("api", "http");

        // The application returns the value of the environment variable that the path names, so the
        // test reads what Aspire put in the process environment.
        var value = await GetStringWithRetryAsync(client, "/env/ConnectionStrings__db", cts.Token);

        Assert.Equal(ConnectionString, value);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task PhoenixLikeAppRespondsOnAllocatedPort()
    {
        using var serverAppDirectory = serverApp.CreateApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // Phoenix reads PORT in the same way, so this covers the endpoint wiring of AddPhoenixApp
        // without a Phoenix installation.
        builder.AddElixirApp("web", serverAppDirectory.Path)
            .WithHttpEndpoint(env: "PORT");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);
        await WaitForServerAsync(app, "web", cts.Token);

        using var client = app.CreateHttpClient("web", "http");

        var body = await GetStringWithRetryAsync(client, "/", cts.Token);

        Assert.Equal(TempElixirAppDirectory.DefaultRootResponse, body);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task LiveReloadRestartsElixirAppOnLibChange()
    {
        const string ChangedResponse = "hello from elixir after the change";

        using var serverAppDirectory = serverApp.CreateApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // AddElixirApp turns live reload on, because `mix run` does not reload code.
        builder.AddElixirApp("api", serverAppDirectory.Path)
            .WithHttpEndpoint(env: "PORT");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);
        await WaitForServerAsync(app, "api", cts.Token);

        using var client = app.CreateHttpClient("api", "http");

        Assert.Equal(
            TempElixirAppDirectory.DefaultRootResponse,
            await GetStringWithRetryAsync(client, "/", cts.Token));

        // The watcher looks at the lib directory, so a change there must restart the application.
        serverAppDirectory.WriteServerModule(ChangedResponse);

        var body = await GetStringWithRetryAsync(client, "/", cts.Token, expected: ChangedResponse);

        Assert.Equal(ChangedResponse, body);

        await app.StopAsync(cts.Token);
    }

    /// <summary>
    /// Waits until the resource runs and its Elixir application accepts connections.
    /// </summary>
    /// <remarks>
    /// The orchestrator reports the running state when the process starts. Mix then compiles the
    /// project and starts the web server, so the socket is not open yet. The application prints the
    /// marker after the supervisor starts, which is the first moment that a request can succeed.
    /// </remarks>
    private static async Task WaitForServerAsync(
        DistributedApplication app, string resourceName, CancellationToken cancellationToken)
    {
        await app.ResourceNotifications.WaitForResourceAsync(
            resourceName, KnownResourceStates.Running, cancellationToken);

        await app.WaitForTextAsync(TempElixirAppDirectory.StartupMarker, resourceName, cancellationToken);
    }

    /// <summary>
    /// Reads a path until the application answers, and, when <paramref name="expected"/> is set, until
    /// the answer holds that text.
    /// </summary>
    /// <remarks>
    /// The Aspire proxy accepts a connection before the Elixir application listens, and it holds that
    /// connection open. Each attempt therefore gets its own short deadline, and the loop, not the
    /// request, controls how long the test waits.
    /// </remarks>
    private static async Task<string> GetStringWithRetryAsync(
        HttpClient client, string path, CancellationToken cancellationToken, string? expected = null)
    {
        Exception? lastError = null;
        string? lastBody = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                var response = await client.GetAsync(path, attemptCts.Token);
                lastBody = await response.Content.ReadAsStringAsync(attemptCts.Token);

                if (response.StatusCode == HttpStatusCode.OK && (expected is null || lastBody == expected))
                {
                    return lastBody;
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // A restart, a closed socket, and an attempt deadline all appear here. The outer token
                // is the only deadline that ends the loop.
                lastError = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new TimeoutException(
            $"'{path}' did not return the expected answer. Last body: '{lastBody}'.", lastError);
    }
}
