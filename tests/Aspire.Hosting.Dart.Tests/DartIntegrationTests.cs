// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;

namespace Aspire.Hosting.Dart.Tests;

/// <summary>
/// Starts real Dart applications with the <c>dart</c> command that the machine supplies.
/// </summary>
/// <remarks>
/// <para>
/// The other test classes assert the application model. This class asserts the runtime behaviour: the
/// process starts, the pub setup sibling runs first, the environment reaches the application, the HTTP
/// endpoint answers, the logs reach the dashboard, and a source change restarts the resource.
/// </para>
/// <para>
/// The class fixture resolves the package one time. Each test then copies the prepared package, so one
/// test cannot change the source that another test reads.
/// </para>
/// </remarks>
[RequiresTools(["dart"])]
public class DartIntegrationTests(DartServerAppFixture serverApp, ITestOutputHelper outputHelper)
    : IClassFixture<DartServerAppFixture>
{
    /// <summary>The time that a Dart start is allowed to take before a test fails.</summary>
    private static readonly TimeSpan s_startTimeout =
        TimeSpan.FromSeconds(PlatformDetection.IsRunningOnCI ? 300 : 120);

    [Fact]
    public async Task DartResourceFinishesSuccessfully()
    {
        using var consoleApp = TempDartAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddDartApp("api", consoleApp.Path);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        var finished = await app.ResourceNotifications.WaitForResourceAsync(
            "api", e => e.Snapshot.State?.Text == KnownResourceStates.Finished, cts.Token);

        // `dart run` reports the exit code of the program, and the program returns from main.
        Assert.Equal(0, finished.Snapshot.ExitCode);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task PubGetSiblingRunsBeforeApp()
    {
        using var consoleApp = TempDartAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddDartApp("api", consoleApp.Path);

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
            Assert.Equal(["api-pub-get", "api"], order);
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
                        // `dart pub get` is a step, so its interesting transition is the completion.
                        "api-pub-get" => state == KnownResourceStates.Finished,

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
    public async Task DartAppLogsAppearInResourceLogs()
    {
        using var consoleApp = TempDartAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        builder.AddDartApp("api", consoleApp.Path);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        // The helper reads the same ResourceLoggerService stream that the dashboard reads.
        await app.WaitForTextAsync(TempDartAppDirectory.StartupMarker, "api", cts.Token);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task DartAppReceivesReferencedConnectionString()
    {
        const string ConnectionString = "postgres://postgres:secret@localhost:5432/appdb";

        using var serverAppDirectory = serverApp.CreateApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var database = builder.AddConnectionString("db", ReferenceExpression.Create($"{ConnectionString}"));
        builder.AddDartApp("api", serverAppDirectory.Path)
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
    public async Task DartHttpAppRespondsOnAllocatedPort()
    {
        using var serverAppDirectory = serverApp.CreateApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // Aspire allocates the port and passes it in PORT, so the application never picks its own.
        builder.AddDartApp("web", serverAppDirectory.Path)
            .WithHttpEndpoint(env: "PORT");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);
        await WaitForServerAsync(app, "web", cts.Token);

        using var client = app.CreateHttpClient("web", "http");

        var body = await GetStringWithRetryAsync(client, "/", cts.Token);

        Assert.Equal(TempDartAppDirectory.DefaultRootResponse, body);

        // Aspire passed the port in PORT, and the application bound it. A direct request to that
        // port proves that the process listens where Aspire told it to listen, and not on a port
        // that the program picked.
        var reportedPort = await GetStringWithRetryAsync(client, "/env/PORT", cts.Token);
        Assert.True(int.TryParse(reportedPort, CultureInfo.InvariantCulture, out var port));

        using var directClient = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

        Assert.Equal(
            TempDartAppDirectory.DefaultRootResponse,
            await GetStringWithRetryAsync(directClient, "/", cts.Token));

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task LiveReloadRestartsDartAppOnLibChange()
    {
        const string ChangedResponse = "hello from dart after the change";

        using var serverAppDirectory = serverApp.CreateApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // AddDartApp turns live reload on, because `dart run` does not reload code.
        builder.AddDartApp("api", serverAppDirectory.Path)
            .WithHttpEndpoint(env: "PORT");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);
        await WaitForServerAsync(app, "api", cts.Token);

        using var client = app.CreateHttpClient("api", "http");

        Assert.Equal(
            TempDartAppDirectory.DefaultRootResponse,
            await GetStringWithRetryAsync(client, "/", cts.Token));

        // The watcher looks at the lib directory, so a change there must restart the application.
        serverAppDirectory.WriteResponseLibrary(ChangedResponse);

        var body = await GetStringWithRetryAsync(client, "/", cts.Token, expected: ChangedResponse);

        Assert.Equal(ChangedResponse, body);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task WithRunCommand_RunsCustomCommand()
    {
        using var consoleApp = TempDartAppDirectory.CreateConsoleApp();
        using var cts = new CancellationTokenSource(s_startTimeout);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // The command line replaces `dart run bin/main.dart`, so the second entrypoint runs and the
        // marker of the default entrypoint never appears.
        builder.AddDartApp("api", consoleApp.Path)
            .WithRunCommand("dart", "run", TempDartAppDirectory.AlternateEntrypoint);

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        await app.WaitForTextAsync(TempDartAppDirectory.AlternateEntrypointMarker, "api", cts.Token);

        var finished = await app.ResourceNotifications.WaitForResourceAsync(
            "api", e => e.Snapshot.State?.Text == KnownResourceStates.Finished, cts.Token);

        Assert.Equal(0, finished.Snapshot.ExitCode);

        await app.StopAsync(cts.Token);
    }

    /// <summary>
    /// Waits until the resource runs and its Dart application accepts connections.
    /// </summary>
    /// <remarks>
    /// The orchestrator reports the running state when the process starts. The Dart VM then compiles
    /// the program and binds the socket, so the socket is not open yet. The application prints the
    /// marker after the bind, which is the first moment that a request can succeed.
    /// </remarks>
    private static async Task WaitForServerAsync(
        DistributedApplication app, string resourceName, CancellationToken cancellationToken)
    {
        await app.ResourceNotifications.WaitForResourceAsync(
            resourceName, KnownResourceStates.Running, cancellationToken);

        await app.WaitForTextAsync(TempDartAppDirectory.StartupMarker, resourceName, cancellationToken);
    }

    /// <summary>
    /// Reads a path until the application answers, and, when <paramref name="expected"/> is set, until
    /// the answer holds that text.
    /// </summary>
    /// <remarks>
    /// The Aspire proxy accepts a connection before the Dart application listens, and it holds that
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
