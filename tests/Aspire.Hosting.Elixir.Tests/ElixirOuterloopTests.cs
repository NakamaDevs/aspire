// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;

namespace Aspire.Hosting.Elixir.Tests;

/// <summary>
/// Starts real Elixir applications that need more than the Mix command: a Hex fetch of the
/// OpenTelemetry packages, or a PostgreSQL container and a complete Phoenix project.
/// </summary>
/// <remarks>
/// Every test in this class carries the <c>outerloop</c> trait. Each one takes minutes, because it
/// fetches and compiles a large dependency tree, so the tests stay out of the normal test run.
/// </remarks>
[RequiresTools(["mix", "elixir"])]
public class ElixirOuterloopTests(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_timeout =
        TimeSpan.FromSeconds(PlatformDetection.IsRunningOnCI ? 900 : 600);

    /// <summary>
    /// Confirms that the OpenTelemetry environment that Aspire writes makes the Erlang SDK send spans
    /// to the endpoint that the dashboard would supply.
    /// </summary>
    /// <remarks>
    /// The test supplies the endpoint through the configuration key that the dashboard uses, so
    /// <c>WithOtlpExporter</c> resolves it in the normal way. The receiver counts requests only. The
    /// content is a protocol buffer message, and the count already proves that the export path works.
    /// </remarks>
    [Fact]
    [OuterloopTest("Fetches and compiles the OpenTelemetry packages from Hex.")]
    public async Task ElixirAppExportsTraceToDashboardOtlpEndpoint()
    {
        using var cts = new CancellationTokenSource(s_timeout);

        using var otelApp = TempElixirAppDirectory.CreateServerApp(
            appName: "aspire_otel_app",
            extraDeps: ", {:opentelemetry_api, \"~> 1.4\"}, {:opentelemetry, \"~> 1.5\"}, {:opentelemetry_exporter, \"~> 1.8\"}",
            extraApplications: ", :opentelemetry_exporter, :opentelemetry",
            startupCode: "spawn(fn -> AspireOtelApp.Telemetry.emit_spans() end)",
            extraModuleCode: """
            defmodule AspireOtelApp.Telemetry do
              require OpenTelemetry.Tracer

              # One span is enough, but a repeat removes the race between the export and the test.
              def emit_spans do
                Process.sleep(500)

                OpenTelemetry.Tracer.with_span "aspire-test-span" do
                  :ok
                end

                emit_spans()
              end
            end
            """);

        otelApp.RunMix("deps.get");
        otelApp.RunMix("compile");

        using var receiver = new OtlpTraceReceiver();

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // Only the HTTP endpoint is configured, so the resolver selects http/protobuf. The gRPC
        // exporter of the Erlang SDK would need a second server in the test.
        builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = receiver.Url;
        builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = null;

        builder.AddElixirApp("api", otelApp.Path)
            .WithHttpEndpoint(env: "PORT")
            // The batch processor waits five seconds by default, which is longer than necessary here.
            .WithEnvironment("OTEL_BSP_SCHEDULE_DELAY", "1000");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("api", KnownResourceStates.Running, cts.Token);

        await receiver.WaitForTraceRequestAsync(cts.Token);

        await app.StopAsync(cts.Token);
    }

    /// <summary>
    /// Confirms that the Phoenix code reloader answers a source change, and that Aspire does not
    /// restart the resource to make that happen.
    /// </summary>
    /// <remarks>
    /// <c>AddPhoenixApp</c> leaves live reload off, because Phoenix recompiles a changed module on the
    /// next request. The test therefore asserts both halves: the new response arrives, and the resource
    /// stays in the running state.
    /// </remarks>
    [Fact]
    [OuterloopTest("Needs a PostgreSQL container and the complete Phoenix dependency tree.")]
    [RequiresFeature(TestFeature.ContainerRuntime)]
    public async Task CodeChangeIsPickedUpWithoutRestart_Phoenix()
    {
        var source = FindPlaygroundPhoenixApp();
        Assert.SkipWhen(source is null, "The playground Phoenix application is not in this checkout.");

        using var cts = new CancellationTokenSource(s_timeout);

        // Another part of the repository owns the playground project, so the test never writes to it.
        // It works on a copy that carries the fetched and compiled dependencies.
        using var phoenixApp = TempElixirAppDirectory.CreateCopyOfDirectory(source!, "phoenix_web");
        var controller = Path.Combine(phoenixApp.Path, "lib", "phoenix_web_web", "controllers", "hello_controller.ex");

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        var database = builder.AddPostgres("pg").AddDatabase("appdb");

        builder.AddPhoenixApp("web", phoenixApp.Path)
            .WithEctoDatabase(database)
            .WithEctoMigrate();

        using var app = builder.Build();

        var restarts = 0;

        // The watch ends only when its token is cancelled, so it gets a token of its own.
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        var watchTask = CountRestartsAsync(app, "web", () => restarts++, watchCts.Token);

        await app.StartAsync(cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("web", KnownResourceStates.Running, cts.Token);

        using var client = app.CreateHttpClient("web", "http");

        var before = await GetWithRetryAsync(client, "/api/hello", cts.Token, expected: "\"version\":1");
        Assert.Contains("hello from phoenix", before, StringComparison.Ordinal);

        // The controller is the only file that changes, and only the version number in it changes.
        var source1 = await File.ReadAllTextAsync(controller, cts.Token);
        await File.WriteAllTextAsync(controller, source1.Replace("version: 1", "version: 2", StringComparison.Ordinal), cts.Token);

        var restartsBeforeTheChange = restarts;

        // The Phoenix code reloader compiles the changed module when the next request arrives.
        await GetWithRetryAsync(client, "/api/hello", cts.Token, expected: "\"version\":2");

        Assert.Equal(restartsBeforeTheChange, restarts);

        await app.StopAsync(cts.Token);

        watchCts.Cancel();
        await watchTask;
    }

    /// <summary>
    /// Finds the Phoenix project of the playground, or returns <see langword="null"/> when the checkout
    /// does not hold it.
    /// </summary>
    private static string? FindPlaygroundPhoenixApp()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            // A worktree holds a .git file instead of a .git directory, so both shapes count.
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                var app = Path.Combine(directory.FullName, "playground", "ElixirApps", "phoenix_web");

                return File.Exists(Path.Combine(app, "mix.exs")) ? app : null;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>Counts every return of a resource to the running state after the first one.</summary>
    private static async Task CountRestartsAsync(
        DistributedApplication app, string resourceName, Action onRestart, CancellationToken cancellationToken)
    {
        var running = false;

        try
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(cancellationToken))
            {
                if (resourceEvent.Resource.Name != resourceName)
                {
                    continue;
                }

                var state = resourceEvent.Snapshot.State?.Text;

                if (state == KnownResourceStates.Running)
                {
                    running = true;
                }
                else if (running && state is not null && state != KnownResourceStates.Running)
                {
                    // The resource left the running state, so a restart is in progress.
                    running = false;
                    onRestart();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The application stopped. The counter holds every transition until that point.
        }
    }

    private static async Task<string> GetWithRetryAsync(
        HttpClient client, string path, CancellationToken cancellationToken, string expected)
    {
        string? lastBody = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                var response = await client.GetAsync(path, attemptCts.Token);
                lastBody = await response.Content.ReadAsStringAsync(attemptCts.Token);

                if (response.StatusCode == HttpStatusCode.OK && lastBody.Contains(expected, StringComparison.Ordinal))
                {
                    return lastBody;
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // A compile error page, a closed socket, and an attempt deadline all appear here.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"'{path}' never held '{expected}'. Last body: '{lastBody}'.");
    }

    /// <summary>
    /// Accepts OTLP over HTTP and counts the requests that reach <c>/v1/traces</c>.
    /// </summary>
    /// <remarks>
    /// The receiver stands in for the dashboard. It answers every request with status 200 and an empty
    /// body, which the Erlang exporter accepts as a successful export.
    /// </remarks>
    private sealed class OtlpTraceReceiver : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource _firstTraceRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public OtlpTraceReceiver()
        {
            var port = FreePort();
            Url = $"http://127.0.0.1:{port}";
            _listener.Prefixes.Add($"{Url}/");
            _listener.Start();

            _ = Task.Run(AcceptAsync);
        }

        /// <summary>The endpoint that the exporter must reach.</summary>
        public string Url { get; }

        public Task WaitForTraceRequestAsync(CancellationToken cancellationToken)
            => _firstTraceRequest.Task.WaitAsync(cancellationToken);

        private async Task AcceptAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    // The listener stopped, which is the normal end of the receiver.
                    return;
                }

                if (context.Request.Url?.AbsolutePath == "/v1/traces")
                {
                    _firstTraceRequest.TrySetResult();
                }

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/x-protobuf";
                context.Response.ContentLength64 = 0;
                context.Response.Close();
            }
        }

        private static int FreePort()
        {
            using var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return port;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Close();
            _cts.Dispose();
        }
    }
}
