// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;
using Aspire.TestUtilities;
using Aspire.TypeSystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.CodeGeneration.Elixir.Tests;

/// <summary>
/// End-to-end tests for the generated Elixir SDK against a real RemoteHost JSON-RPC server.
/// </summary>
/// <remarks>
/// Each test generates the SDK from the real <c>Aspire.Hosting</c> assembly plus the shared ATS test
/// types, starts the server in this process on a temporary Unix socket, runs an <c>apphost.exs</c>
/// script with the <c>elixir</c> executable, and then asserts on the .NET objects the dispatcher
/// created and on the exit code of the script.
/// </remarks>
[RequiresTools(["elixir"])]
public class ElixirRoundTripTests(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private static readonly TimeSpan s_readyTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan s_exitTimeout = TimeSpan.FromSeconds(15);

    private TemporaryWorkspace? _workspace;

    public async ValueTask InitializeAsync()
    {
        _workspace = TemporaryWorkspace.Create(outputHelper);
        await ElixirRoundTripHost.GenerateSdkAsync(_workspace.Path);
    }

    public ValueTask DisposeAsync()
    {
        _workspace?.Dispose();
        return ValueTask.CompletedTask;
    }

    private string WorkspacePath => _workspace!.Path;

    [Fact]
    public async Task RoundTrip_CreateBuilder_ReturnsBuilderHandle()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            builder = Aspire.create_builder!()

            AppHostTest.report("builder", builder.handle.id)
            AppHostTest.report("builder_type", builder.handle.type)
            AppHostTest.report("builder_module", inspect(builder.__struct__))
            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.Equal(AtsConstants.BuilderTypeId, run.Value("builder_type"));
        Assert.Equal("Aspire.DistributedApplicationBuilder", run.Value("builder_module"));

        var builder = host.GetHandleObject<IDistributedApplicationBuilder>(run.Value("builder"));
        Assert.NotNull(builder.Resources);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_AddContainer_AppearsInModel()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            builder = Aspire.create_builder!()
            container = Aspire.DistributedApplicationBuilder.add_container!(builder, "cache", "redis:7.4")

            AppHostTest.report("builder", builder.handle.id)
            AppHostTest.report("container", container.handle.id)
            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        var builder = host.GetHandleObject<IDistributedApplicationBuilder>(run.Value("builder"));
        var container = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("cache", container.Name);

        var image = Assert.Single(container.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("redis", image.Image);
        Assert.Equal("7.4", image.Tag);

        // The handle the guest holds is the resource builder for the same resource.
        var containerBuilder = host.GetHandleObject<IResourceBuilder<ContainerResource>>(run.Value("container"));
        Assert.Same(container, containerBuilder.Resource);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_EnvironmentCallback_IsInvokedFromHost()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            builder = Aspire.create_builder!()
            container = Aspire.DistributedApplicationBuilder.add_container!(builder, "cache", "redis:7.4")

            Aspire.ContainerResource.with_environment_callback!(container, fn context ->
              context
              |> Aspire.EnvironmentCallbackContext.environment!()
              |> Aspire.EnvironmentEditor.set!("FROM_ELIXIR", "elixir-callback")
            end)

            AppHostTest.report("container", container.handle.id)
            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        var containerBuilder = host.GetHandleObject<IResourceBuilder<ContainerResource>>(run.Value("container"));

        // Evaluating the environment runs the callback, which the host invokes back into the still
        // connected Elixir process.
        var environment = await GetEnvironmentAsync(containerBuilder.Resource);

        Assert.True(
            environment.TryGetValue("FROM_ELIXIR", out var value),
            $"The environment does not hold FROM_ELIXIR. Keys: {string.Join(", ", environment.Keys)}\n{run.Output}");
        Assert.Equal("elixir-callback", value);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_CapabilityError_SurfacesAsAspireError()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            builder = Aspire.create_builder!()

            # A map update keeps the struct and its transport, and it does not need the module at
            # the time the script is compiled. Code.require_file loads the SDK when the script runs.
            unknown = %{builder | handle: Aspire.Handle.new("999999", builder.handle.type)}

            case Aspire.DistributedApplicationBuilder.add_container(unknown, "cache", "redis:7.4") do
              {:error, error} ->
                AppHostTest.report("error_struct", inspect(error.__struct__))
                AppHostTest.report("error_code", error.code)
                AppHostTest.report("error_capability", error.capability)
                AppHostTest.report("error_message", error.message)

              other ->
                AppHostTest.report("unexpected", inspect(other))
            end

            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.Equal("Aspire.Error", run.Value("error_struct"));
        Assert.Equal("HANDLE_NOT_FOUND", run.Value("error_code"));
        Assert.Equal("Aspire.Hosting/addContainer", run.Value("error_capability"));
        Assert.Contains("999999", run.Value("error_message"), StringComparison.Ordinal);

        run.Release();

        // A capability error is a value in Elixir, so the script keeps running and exits normally.
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_CancellationToken_CancelsHostOperation()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            builder = Aspire.create_builder!()
            redis = Aspire.DistributedApplicationBuilder.add_test_redis!(builder, "cache")

            token = Aspire.CancellationToken.new()

            status =
              Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.get_status_async!(
                redis,
                cancellation_token: token
              )

            {:ok, cancelled} = Aspire.CancellationToken.cancel(token, builder.transport)

            AppHostTest.report("status", status)
            AppHostTest.report("token", token.id)
            AppHostTest.report("cancelled", cancelled)
            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.Equal("running", run.Value("status"));
        Assert.Equal("true", run.Value("cancelled"));

        // The host registered the token the guest created and cancelled it when the guest sent the
        // cancelToken request.
        Assert.True(
            host.IsCancellationRequested(run.Value("token")),
            $"The host did not cancel the token the guest created.\n{run.Output}");

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_PipeChain_AddRedisWithPersistence()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            builder = Aspire.create_builder!()

            # Every step of the chain must return the receiver type. `with_environment` declares
            # `IResourceWithEnvironment`, so without the ReturnsBuilder handling in the generator the
            # third call raises FunctionClauseError.
            redis =
              builder
              |> Aspire.DistributedApplicationBuilder.add_test_redis!("cache", port: 6380)
              |> Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_environment!("REDIS_MODE", "standalone")
              |> Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_persistence!(mode: :bind)

            AppHostTest.report("builder", builder.handle.id)
            AppHostTest.report("redis", redis.handle.id)
            AppHostTest.report("redis_module", inspect(redis.__struct__))
            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        var builder = host.GetHandleObject<IDistributedApplicationBuilder>(run.Value("builder"));
        var redis = Assert.Single(builder.Resources.OfType<TestRedisResource>());
        Assert.Equal("cache", redis.Name);

        var endpoint = Assert.Single(redis.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(6380, endpoint.Port);

        var persistence = Assert.Single(redis.Annotations.OfType<TestPersistenceAnnotation>());
        Assert.Equal(TestPersistenceMode.Bind, persistence.Mode);

        var environment = await GetEnvironmentAsync(redis);
        Assert.Equal("standalone", environment["REDIS_MODE"]);

        // The middle call declares IResourceWithEnvironment, and the chain still carries the
        // receiver struct.
        Assert.Equal("Aspire.CodeGeneration.Elixir.Tests.TestRedisResource", run.Value("redis_module"));

        var redisBuilder = host.GetHandleObject<IResourceBuilder<TestRedisResource>>(run.Value("redis"));
        Assert.Same(redis, redisBuilder.Resource);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_ExportedValue_DecodesToStruct()
    {
        await using var host = await ElixirRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
            config = Aspire.Values.TestConfigs.default()

            AppHostTest.report("struct", inspect(config.__struct__))
            AppHostTest.report("name", config.name)
            AppHostTest.report("port", config.port)
            AppHostTest.report("enabled", config.enabled)
            AppHostTest.report("optional_field", config.optional_field)
            AppHostTest.report("greeting", Aspire.Values.TestConfigs.unicode_greeting())

            # The snapped value goes back to the host, so the value makes a full round trip.
            builder = Aspire.create_builder!()
            container = Aspire.DistributedApplicationBuilder.add_container!(builder, "cache", "redis:7.4")
            Aspire.ContainerResource.with_environment!(container, "CONFIG_NAME", config.name)

            AppHostTest.report("container", container.handle.id)
            AppHostTest.ready()
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.exs");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.Equal("Aspire.CodeGeneration.Elixir.Tests.TestConfigDto", run.Value("struct"));
        Assert.Equal("default", run.Value("name"));
        Assert.Equal("6379", run.Value("port"));
        Assert.Equal("true", run.Value("enabled"));
        Assert.Equal("cache", run.Value("optional_field"));
        Assert.Equal("你好こんにちは", run.Value("greeting"));

        var containerBuilder = host.GetHandleObject<IResourceBuilder<ContainerResource>>(run.Value("container"));
        var environment = await GetEnvironmentAsync(containerBuilder.Resource);
        Assert.Equal("default", environment["CONFIG_NAME"]);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    /// <summary>
    /// Evaluates the environment of a resource. Evaluation runs every environment callback, which is
    /// how a callback that lives in the Elixir guest reaches the host.
    /// </summary>
    private static async Task<Dictionary<string, string>> GetEnvironmentAsync(IResource resource)
    {
        var configuration = await ExecutionConfigurationBuilder.Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), NullLogger.Instance);

        return configuration.EnvironmentVariables.ToDictionary();
    }

    /// <summary>
    /// Writes <c>apphost.exs</c>. The prologue loads the generated SDK and defines the two helpers
    /// every script uses: <c>AppHostTest.report/2</c> reports a value to the .NET test, and
    /// <c>AppHostTest.ready/0</c> holds the connection open until the test releases the script.
    /// </summary>
    private async Task WriteAppHostAsync(string body)
    {
        var script = $$"""
            Code.require_file("aspire.ex", __DIR__)

            defmodule AppHostTest do
              def report(key, value) do
                IO.puts("{{ElixirRoundTripHost.ValuePrefix}}" <> key <> "=" <> to_string(value))
              end

              def ready do
                IO.puts("{{ElixirRoundTripHost.ReadyMarker}}")
                IO.read(:stdio, :line)
              end
            end

            {{body}}
            """;

        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "apphost.exs"), script);
    }
}
