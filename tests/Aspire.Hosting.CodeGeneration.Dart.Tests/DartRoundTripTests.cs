// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;
using Aspire.TestUtilities;
using Aspire.TypeSystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.CodeGeneration.Dart.Tests;

/// <summary>
/// End-to-end tests for the generated Dart SDK against a real RemoteHost JSON-RPC server.
/// </summary>
/// <remarks>
/// The class generates the SDK once from the real <c>Aspire.Hosting</c> assembly plus the shared ATS
/// test types, and it runs <c>dart pub get</c> once. Each test then starts the server in this process
/// on a temporary Unix socket, writes its own <c>apphost.dart</c>, runs it with <c>dart run</c>, and
/// asserts on the .NET objects the dispatcher created and on the exit code of the script.
/// </remarks>
[RequiresTools(["dart"])]
public class DartRoundTripTests(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private static readonly TimeSpan s_readyTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan s_exitTimeout = TimeSpan.FromSeconds(15);

    private TemporaryWorkspace? _workspace;

    public async ValueTask InitializeAsync()
    {
        _workspace = TemporaryWorkspace.Create(outputHelper);
        await DartRoundTripHost.GenerateSdkAsync(_workspace.Path);
        await DartRoundTripHost.RestoreAsync(_workspace.Path, outputHelper);
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
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);

              report('builder', builder.handle.id);
              report('builder_type', builder.handle.type);
              report('builder_class', builder.runtimeType);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.Equal(AtsConstants.BuilderTypeId, run.Value("builder_type"));

        // IDistributedApplicationBuilder is a .NET interface, so DistributedApplicationBuilder is
        // an abstract Dart class and the value is its private implementation class.
        Assert.Equal("_DistributedApplicationBuilderImpl", run.Value("builder_class"));

        var builder = host.GetHandleObject<IDistributedApplicationBuilder>(run.Value("builder"));
        Assert.NotNull(builder.Resources);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_AddContainer_AppearsInModel()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);
              final ContainerResource container =
                  await builder.addContainer('cache', 'redis:7.4');

              report('builder', builder.handle.id);
              report('container', container.handle.id);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
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
    public async Task RoundTrip_InterfaceParameter_AcceptsConcreteResource()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        // `waitFor` declares IResource. TestRedisResource implements the Dart class of that
        // interface, so the concrete resource passes without a conversion at the call site.
        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);
              final TestRedisResource redis = await builder.addTestRedis('cache');
              final ContainerResource container =
                  await builder.addContainer('api', 'nginx:1.27');

              await container.waitFor(redis);

              report('container', container.handle.id);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
        await run.WaitForReadyAsync(s_readyTimeout);

        var containerBuilder = host.GetHandleObject<IResourceBuilder<ContainerResource>>(run.Value("container"));
        var wait = Assert.Single(containerBuilder.Resource.Annotations.OfType<WaitAnnotation>());
        Assert.Equal("cache", wait.Resource.Name);
        Assert.Equal(WaitType.WaitUntilHealthy, wait.WaitType);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_EnvironmentCallback_IsInvokedFromHost()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);
              final ContainerResource container =
                  await builder.addContainer('cache', 'redis:7.4');

              await container.withEnvironmentCallback((EnvironmentCallbackContext context) async {
                final EnvironmentEditor editor = await context.environment();
                await editor.set_('FROM_DART', 'dart-callback');
              });

              report('container', container.handle.id);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
        await run.WaitForReadyAsync(s_readyTimeout);

        var containerBuilder = host.GetHandleObject<IResourceBuilder<ContainerResource>>(run.Value("container"));

        // Evaluating the environment runs the callback, which the host invokes back into the still
        // connected Dart process. The script waits on stdin asynchronously, so its event loop is free
        // to answer the invokeCallback request.
        var environment = await GetEnvironmentAsync(containerBuilder.Resource);

        Assert.True(
            environment.TryGetValue("FROM_DART", out var value),
            $"The environment does not hold FROM_DART. Keys: {string.Join(", ", environment.Keys)}\n{run.Output}");
        Assert.Equal("dart-callback", value);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_CapabilityError_SurfacesAsAspireError()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);

              final ContainerResource container =
                  await builder.addContainer('cache', 'redis:7.4');

              // A wrapper is a handle plus a transport, so a handle the host never issued builds the
              // same class and reaches the same capability.
              final ContainerResource unknown = ContainerResource(
                AspireHandle('999999', container.handle.type),
                builder.transport,
              );

              try {
                await unknown.withEnvironment('KEY', 'value');
                report('unexpected', 'the call returned a value');
              } on AspireError catch (error) {
                report('error_class', error.runtimeType);
                report('error_code', error.code);
                report('error_capability', error.capability);
                report('error_message', error.message);
              }

              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.False(run.HasValue("unexpected"), $"The failed capability returned a value.\n{run.Output}");
        Assert.Equal("AspireError", run.Value("error_class"));
        Assert.Equal("HANDLE_NOT_FOUND", run.Value("error_code"));
        Assert.Equal("Aspire.Hosting/withEnvironment", run.Value("error_capability"));
        Assert.Contains("999999", run.Value("error_message"), StringComparison.Ordinal);

        run.Release();

        // The script caught the error, so it keeps running and exits normally.
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_CancellationToken_CancelsHostOperation()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);
              final TestRedisResource redis = await builder.addTestRedis('cache');

              final CancellationToken token = CancellationToken.create();
              final String? status = await redis.getStatusAsync(cancellationToken: token);
              final bool cancelled = await token.cancel(builder.transport);

              report('status', status);
              report('token', token.id);
              report('cancelled', cancelled);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
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
    public async Task RoundTrip_FluentChain_AddRedisWithPersistence()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);

              // Every step of the chain must return the receiver type. `withEnvironment` declares
              // `IResourceWithEnvironment`, so without the ReturnsBuilder handling in the generator
              // the third call does not compile.
              final TestRedisResource redis = await (await (await builder
                      .addTestRedis('cache', port: 6380))
                  .withEnvironment('REDIS_MODE', 'standalone'))
                  .withPersistence(mode: TestPersistenceMode.bind);

              report('builder', builder.handle.id);
              report('redis', redis.handle.id);
              report('redis_class', redis.runtimeType);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
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
        // receiver class.
        Assert.Equal("TestRedisResource", run.Value("redis_class"));

        var redisBuilder = host.GetHandleObject<IResourceBuilder<TestRedisResource>>(run.Value("redis"));
        Assert.Same(redis, redisBuilder.Resource);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    [Fact]
    public async Task RoundTrip_ExportedValue_DecodesToClass()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        await WriteAppHostAsync("""
              final TestConfigDto config = TestConfigs.default_!;

              report('config_class', config.runtimeType);
              report('name', config.name);
              report('port', config.port);
              report('enabled', config.enabled);
              report('optional_field', config.optionalField);
              report('greeting', TestConfigs.unicodeGreeting);

              // The snapped value goes back to the host, so the value makes a full round trip.
              final DistributedApplicationBuilder builder = await createBuilder(args);
              final ContainerResource container =
                  await builder.addContainer('cache', 'redis:7.4');
              await container.withEnvironment('CONFIG_NAME', config.name);

              report('container', container.handle.id);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
        await run.WaitForReadyAsync(s_readyTimeout);

        Assert.Equal("TestConfigDto", run.Value("config_class"));
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

    [Fact]
    public async Task RoundTrip_DtoCallback_ReturnsChangedObject()
    {
        await using var host = await DartRoundTripHost.StartAsync(outputHelper);

        // A generated data object is immutable, so a callback that receives one returns the changed
        // object. The transport puts it in the positional write-back map, and the host copies the
        // properties onto the .NET object that it passed in.
        await WriteAppHostAsync("""
              final DistributedApplicationBuilder builder = await createBuilder(args);
              final ContainerResource container =
                  await builder.addContainer('cache', 'redis:7.4');

              await container.withUrlForEndpoint('tcp', (ResourceUrlAnnotation? url) async {
                report('callback_url', url?.url);
                return ResourceUrlAnnotation(url: '/dart', displayText: 'Dart home');
              });

              report('container', container.handle.id);
              await ready();
            """);

        await using var run = host.StartScript(WorkspacePath, "apphost.dart");
        await run.WaitForReadyAsync(s_readyTimeout);

        var containerBuilder = host.GetHandleObject<IResourceBuilder<ContainerResource>>(run.Value("container"));
        var resource = containerBuilder.Resource;

        // WithUrlForEndpoint matches on the endpoint name, and the marshaller reads the port of the
        // endpoint when it serializes the URL, so the resource needs an allocated endpoint.
        var endpoint = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "tcp", port: 8080);
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 8080, EndpointBindingMode.SingleAddress, targetPortExpression: null, networkId: null);
        resource.Annotations.Add(endpoint);

        var annotation = new ResourceUrlAnnotation
        {
            Url = "http://localhost:8080",
            Endpoint = new EndpointReference(resource, "tcp")
        };

        var context = new ResourceUrlsCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resource,
            [annotation]);

        var callback = Assert.Single(resource.Annotations.OfType<ResourceUrlsCallbackAnnotation>());
        await callback.Callback(context);

        Assert.Equal("/dart", annotation.Url);
        Assert.Equal("Dart home", annotation.DisplayText);

        run.Release();
        Assert.Equal(0, await run.WaitForExitAsync(s_exitTimeout));
    }

    /// <summary>
    /// Evaluates the environment of a resource. Evaluation runs every environment callback, which is
    /// how a callback that lives in the Dart guest reaches the host.
    /// </summary>
    private static async Task<Dictionary<string, string>> GetEnvironmentAsync(IResource resource)
    {
        var configuration = await ExecutionConfigurationBuilder.Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), NullLogger.Instance);

        return configuration.EnvironmentVariables.ToDictionary();
    }

    /// <summary>
    /// Writes <c>apphost.dart</c>. The prologue imports the generated SDK and defines the two helpers
    /// every script uses: <c>report</c> reports a value to the .NET test, and <c>ready</c> holds the
    /// connection open until the test releases the script.
    /// </summary>
    /// <remarks>
    /// <c>ready</c> reads standard input through a stream. A synchronous read blocks the Dart event
    /// loop, and the transport could then not answer a host callback while the test inspects the
    /// model. The epilogue closes the transport, because an open socket keeps the event loop alive
    /// and the process would never exit.
    /// </remarks>
    private async Task WriteAppHostAsync(string body)
    {
        var script = $$"""
            // ignore_for_file: unused_import
            import 'dart:async';
            import 'dart:convert';
            import 'dart:io';

            import '{{DartRoundTripHost.ModulesDirectory}}/aspire.dart';

            void report(String key, Object? value) {
              stdout.writeln('{{DartRoundTripHost.ValuePrefix}}$key=$value');
            }

            Future<void> ready() async {
              stdout.writeln('{{DartRoundTripHost.ReadyMarker}}');
              await stdin
                  .transform(utf8.decoder)
                  .transform(const LineSplitter())
                  .first;
            }

            Future<void> main(List<String> args) async {
            {{body}}

              if (AspireTransport.hasDefaultInstance) {
                await AspireTransport.defaultInstance.close();
              }
            }
            """;

        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "apphost.dart"), script);
    }
}
