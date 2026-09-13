// aspire_generated.dart - Generated Aspire declarations
// GENERATED CODE - DO NOT EDIT

part of 'aspire.dart';

/// A callback that the AppHost invokes. Every argument arrives as its generated Dart type.
typedef CancellationTokenCallback = FutureOr<void> Function(CancellationToken? arg);

/// A callback that the AppHost invokes. Every argument arrives as its generated Dart type.
typedef TestCallback = FutureOr<void> Function(TestCallbackContext arg);

/// A callback that the AppHost invokes. Every argument arrives as its generated Dart type.
typedef TestCallbackTestEnvironmentCallback = FutureOr<void> Function(TestCallbackContext arg1, TestEnvironmentContext arg2);

/// A callback that the AppHost invokes. Every argument arrives as its generated Dart type.
typedef TestEnvironmentCallback = FutureOr<void> Function(TestEnvironmentContext arg);

/// A callback that the AppHost invokes. Every argument arrives as its generated Dart type.
typedef TestResourceCallback = FutureOr<bool?> Function(TestResourceContext arg);

/// Test persistence mode enum.
enum TestPersistenceMode implements AspireWireValue {
  /// The `None` value.
  none('None'),
  /// The `Volume` value.
  volume('Volume'),
  /// The `Bind` value.
  bind('Bind');

  const TestPersistenceMode(this.wireName);

  /// The .NET member name that travels on the wire.
  final String wireName;

  /// Returns the wire form of the value.
  @override
  String toWire() => wireName;

  /// Returns the wire form of [value].
  ///
  /// [value] is a `TestPersistenceMode`, or a string that names one. Throws
  /// [ArgumentError] when nothing matches. The message lists every value.
  static String toWireOf(Object? value) {
    if (value is TestPersistenceMode) {
      return value.wireName;
    }
    final TestPersistenceMode? match = fromWire(value);
    if (match != null) {
      return match.wireName;
    }
    throw ArgumentError.value(
      value,
      'value',
      'TestPersistenceMode does not accept it. It accepts: none, volume, bind',
    );
  }

  /// Returns the value that [wire] names, or null when no value matches.
  static TestPersistenceMode? fromWire(Object? wire) {
    for (final value in values) {
      if (value.wireName == wire) {
        return value;
      }
    }
    return null;
  }
}

/// Test enum for type generation verification.
///
/// ## Values
///
/// * [pending] — The resource is pending.
/// * [running] — The resource is running.
/// * [stopped] — The resource is stopped.
/// * [failed] — The resource failed.
enum TestResourceStatus implements AspireWireValue {
  /// The resource is pending.
  pending('Pending'),
  /// The resource is running.
  running('Running'),
  /// The resource is stopped.
  stopped('Stopped'),
  /// The resource failed.
  failed('Failed');

  const TestResourceStatus(this.wireName);

  /// The .NET member name that travels on the wire.
  final String wireName;

  /// Returns the wire form of the value.
  @override
  String toWire() => wireName;

  /// Returns the wire form of [value].
  ///
  /// [value] is a `TestResourceStatus`, or a string that names one. Throws
  /// [ArgumentError] when nothing matches. The message lists every value.
  static String toWireOf(Object? value) {
    if (value is TestResourceStatus) {
      return value.wireName;
    }
    final TestResourceStatus? match = fromWire(value);
    if (match != null) {
      return match.wireName;
    }
    throw ArgumentError.value(
      value,
      'value',
      'TestResourceStatus does not accept it. It accepts: pending, running, stopped, failed',
    );
  }

  /// Returns the value that [wire] names, or null when no value matches.
  static TestResourceStatus? fromWire(Object? wire) {
    for (final value in values) {
      if (value.wireName == wire) {
        return value;
      }
    }
    return null;
  }
}

/// Test DTO to verify [AspireDto] generates TypeScript interfaces.
///
/// ## Properties
///
/// * [name] — The name of the test config.
/// * [port] — The port used by the test config.
/// * [enabled] — A value indicating whether the test config is enabled.
/// * [optionalField] — An optional test config field.
class TestConfigDto implements AspireWireValue {
  /// Builds a `TestConfigDto`.
  const TestConfigDto({
    this.name,
    this.port,
    this.enabled,
    this.optionalField,
  });

  /// Builds the data object from its wire form.
  factory TestConfigDto.fromJson(Map<String, Object?> json) => TestConfigDto(
    name: AspireRuntime.asString(json['name']),
    port: AspireRuntime.asNum(json['port']),
    enabled: AspireRuntime.asBool(json['enabled']),
    optionalField: AspireRuntime.asString(json['optionalField']),
  );

  /// Returns the data object that [wire] holds, or null when [wire] is not an object.
  static TestConfigDto? fromWire(Object? wire) => wire is Map
    ? TestConfigDto.fromJson(AspireRuntime.asObject(wire))
    : null;

  /// The name of the test config.
  final String? name;

  /// The port used by the test config.
  final num? port;

  /// A value indicating whether the test config is enabled.
  final bool? enabled;

  /// An optional test config field.
  final String? optionalField;

  /// Returns the wire form of the data object.
  ///
  /// A property that is null is left out, so the host keeps its own default.
  Map<String, Object?> toJson() {
    final Map<String, Object?> json = <String, Object?>{};
    if (name != null) {
      json['name'] = name;
    }
    if (port != null) {
      json['port'] = port;
    }
    if (enabled != null) {
      json['enabled'] = enabled;
    }
    if (optionalField != null) {
      json['optionalField'] = optionalField;
    }
    return json;
  }

  /// Returns the wire form of the data object. See [toJson].
  @override
  Object? toWire() => toJson();
}

/// Test DTO with deeply nested generic types.
///
/// ## Properties
///
/// * [nestedData] — Deeply nested generic: Dictionary containing List of DTOs.
/// * [metadataArray] — Array of dictionaries.
class TestDeeplyNestedDto implements AspireWireValue {
  /// Builds a `TestDeeplyNestedDto`.
  const TestDeeplyNestedDto({
    this.nestedData,
    this.metadataArray,
  });

  /// Builds the data object from its wire form.
  factory TestDeeplyNestedDto.fromJson(Map<String, Object?> json) => TestDeeplyNestedDto(
    nestedData: AspireRuntime.asMap<List<TestConfigDto?>?>(json['nestedData'], (Object? item) => AspireRuntime.asList<TestConfigDto?>(item, (Object? item) => TestConfigDto.fromWire(item))),
    metadataArray: AspireRuntime.asList<Map<String, String?>?>(json['metadataArray'], (Object? item) => AspireRuntime.asMap<String?>(item, (Object? item) => AspireRuntime.asString(item))),
  );

  /// Returns the data object that [wire] holds, or null when [wire] is not an object.
  static TestDeeplyNestedDto? fromWire(Object? wire) => wire is Map
    ? TestDeeplyNestedDto.fromJson(AspireRuntime.asObject(wire))
    : null;

  /// Deeply nested generic: Dictionary containing List of DTOs.
  final Map<String, List<TestConfigDto?>?>? nestedData;

  /// Array of dictionaries.
  final List<Map<String, String?>?>? metadataArray;

  /// Returns the wire form of the data object.
  ///
  /// A property that is null is left out, so the host keeps its own default.
  Map<String, Object?> toJson() {
    final Map<String, Object?> json = <String, Object?>{};
    if (nestedData != null) {
      json['nestedData'] = nestedData;
    }
    if (metadataArray != null) {
      json['metadataArray'] = metadataArray;
    }
    return json;
  }

  /// Returns the wire form of the data object. See [toJson].
  @override
  Object? toWire() => toJson();
}

/// Test DTO with complex nested types.
class TestNestedDto implements AspireWireValue {
  /// Builds a `TestNestedDto`.
  const TestNestedDto({
    this.id,
    this.config,
    this.tags,
    this.counts,
  });

  /// Builds the data object from its wire form.
  factory TestNestedDto.fromJson(Map<String, Object?> json) => TestNestedDto(
    id: AspireRuntime.asString(json['id']),
    config: TestConfigDto.fromWire(json['config']),
    tags: AspireRuntime.asList<String?>(json['tags'], (Object? item) => AspireRuntime.asString(item)),
    counts: AspireRuntime.asMap<num?>(json['counts'], (Object? item) => AspireRuntime.asNum(item)),
  );

  /// Returns the data object that [wire] holds, or null when [wire] is not an object.
  static TestNestedDto? fromWire(Object? wire) => wire is Map
    ? TestNestedDto.fromJson(AspireRuntime.asObject(wire))
    : null;

  /// The `id` property.
  final String? id;

  /// The `config` property.
  final TestConfigDto? config;

  /// The `tags` property.
  final List<String?>? tags;

  /// The `counts` property.
  final Map<String, num?>? counts;

  /// Returns the wire form of the data object.
  ///
  /// A property that is null is left out, so the host keeps its own default.
  Map<String, Object?> toJson() {
    final Map<String, Object?> json = <String, Object?>{};
    if (id != null) {
      json['id'] = id;
    }
    if (config != null) {
      json['config'] = config!.toJson();
    }
    if (tags != null) {
      json['tags'] = tags;
    }
    if (counts != null) {
      json['counts'] = counts;
    }
    return json;
  }

  /// Returns the wire form of the data object. See [toJson].
  @override
  Object? toWire() => toJson();
}

/// The exported `TestConfigs` values.
///
/// The values are snapped when the SDK is generated.
abstract final class TestConfigs {
  /// The default test configuration.
  static TestConfigDto? get default_ => TestConfigDto.fromWire(const <String, Object?>{'name': 'default', 'port': 6379, 'enabled': true, 'optionalField': 'cache'});

  /// The exported `TestConfigs.Secure` value.
  static TestConfigDto? get secure => TestConfigDto.fromWire(const <String, Object?>{'name': 'secure', 'port': 6380, 'enabled': true, 'optionalField': null});

  /// The exported `TestConfigs.UnicodeGreeting` value.
  static String get unicodeGreeting => '你好こんにちは';
}

/// The exported `TestConfigs.Profiles` values.
///
/// The values are snapped when the SDK is generated.
abstract final class TestConfigsProfiles {
  /// The exported `TestConfigs.Profiles.Development` value.
  static TestConfigDto? get development => TestConfigDto.fromWire(const <String, Object?>{'name': 'development', 'port': 5001, 'enabled': false, 'optionalField': null});
}

/// A handle to an object that satisfies the `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder` contract in the AppHost.
abstract class DistributedApplicationBuilder extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder` object.
  const DistributedApplicationBuilder(super.handle, super.transport);

  /// Adds a test Redis resource from ATS documentation.
  ///
  /// ## Parameters
  ///
  /// * [name] — The ATS resource name.
  ///
  /// ## Returns
  ///
  /// The ATS test Redis resource builder.
  Future<TestRedisResource> addTestRedis(String name, {num? port}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['name'] = name;
    if (port != null) {
      args['port'] = port;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/addTestRedis', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/addTestRedis'), transport);
  }

  /// Adds a test vault resource
  ///
  /// ## Parameters
  ///
  /// * [name] — The resource name.
  ///
  /// ## Returns
  ///
  /// The interface vault resource builder.
  Future<ITestVaultResource> addTestVault(String name) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['name'] = name;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/addTestVault', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/addTestVault'), transport);
  }
}

/// A handle to an object that satisfies the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource` contract in the AppHost.
abstract class ITestVaultResource extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource` object.
  const ITestVaultResource(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<ITestVaultResource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<ITestVaultResource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<ITestVaultResource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<ITestVaultResource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<ITestVaultResource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<ITestVaultResource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<ITestVaultResource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<ITestVaultResource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<ITestVaultResource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<ITestVaultResource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<ITestVaultResource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<ITestVaultResource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<ITestVaultResource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<ITestVaultResource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<ITestVaultResource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<ITestVaultResource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<ITestVaultResource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<ITestVaultResource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<ITestVaultResource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<ITestVaultResource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<ITestVaultResource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<ITestVaultResource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }

  /// Configures vault using direct interface target
  Future<ITestVaultResource> withVaultDirect(String option) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['option'] = option;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withVaultDirect', args);
    return _ITestVaultResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withVaultDirect'), transport);
  }
}

/// A handle to an object that satisfies the `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource` contract in the AppHost.
abstract class Resource extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource` object.
  const Resource(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<Resource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<Resource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<Resource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<Resource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<Resource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<Resource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<Resource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<Resource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<Resource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<Resource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<Resource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<Resource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<Resource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<Resource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<Resource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<Resource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<Resource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<Resource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<Resource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<Resource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<Resource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<Resource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _ResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// A handle to an object that satisfies the `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString` contract in the AppHost.
abstract class ResourceWithConnectionString extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString` object.
  const ResourceWithConnectionString(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<ResourceWithConnectionString> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<ResourceWithConnectionString> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<ResourceWithConnectionString> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the connection string using a reference expression
  Future<ResourceWithConnectionString> withConnectionString(ReferenceExpression connectionString) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['connectionString'] = connectionString;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionString', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionString'), transport);
  }

  /// Sets connection string using direct interface target
  Future<ResourceWithConnectionString> withConnectionStringDirect(String connectionString) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['connectionString'] = connectionString;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionStringDirect', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionStringDirect'), transport);
  }

  /// Sets the correlation ID
  Future<ResourceWithConnectionString> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<ResourceWithConnectionString> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<ResourceWithConnectionString> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<ResourceWithConnectionString> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<ResourceWithConnectionString> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<ResourceWithConnectionString> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<ResourceWithConnectionString> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<ResourceWithConnectionString> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<ResourceWithConnectionString> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<ResourceWithConnectionString> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<ResourceWithConnectionString> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<ResourceWithConnectionString> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<ResourceWithConnectionString> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<ResourceWithConnectionString> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<ResourceWithConnectionString> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<ResourceWithConnectionString> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<ResourceWithConnectionString> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<ResourceWithConnectionString> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<ResourceWithConnectionString> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _ResourceWithConnectionStringImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// A handle to an object that satisfies the `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment` contract in the AppHost.
abstract class ResourceWithEnvironment extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment` object.
  const ResourceWithEnvironment(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<ResourceWithEnvironment> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Configures environment with callback (test version)
  Future<ResourceWithEnvironment> testWithEnvironmentCallback(TestEnvironmentCallback callback) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['callback'] = (Object? a0) async {
      final TestEnvironmentContext p0 = TestEnvironmentContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
      await callback(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
  }

  /// Performs a cancellable operation
  Future<ResourceWithEnvironment> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<ResourceWithEnvironment> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<ResourceWithEnvironment> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<ResourceWithEnvironment> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<ResourceWithEnvironment> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<ResourceWithEnvironment> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Sets environment variables
  Future<ResourceWithEnvironment> withEnvironmentVariables(Map<String, String?> variables) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['variables'] = variables;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables'), transport);
  }

  /// Configures a named endpoint
  Future<ResourceWithEnvironment> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<ResourceWithEnvironment> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<ResourceWithEnvironment> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<ResourceWithEnvironment> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<ResourceWithEnvironment> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<ResourceWithEnvironment> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<ResourceWithEnvironment> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<ResourceWithEnvironment> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<ResourceWithEnvironment> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<ResourceWithEnvironment> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<ResourceWithEnvironment> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<ResourceWithEnvironment> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<ResourceWithEnvironment> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<ResourceWithEnvironment> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<ResourceWithEnvironment> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _ResourceWithEnvironmentImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// Test callback context for WithCustomCallback. Also used to verify [AspireExport(ExposeProperties = true)] scanning.
class TestCallbackContext extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext` object.
  const TestCallbackContext(super.handle, super.transport);

  /// CancellationToken is supported by ATS.
  Future<CancellationToken?> cancellationToken() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken', args);
    return AspireRuntime.asCancellationToken(result);
  }

  /// Gets the Name property
  Future<String?> name() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name', args);
    return AspireRuntime.asString(result);
  }

  /// CancellationToken is supported by ATS.
  ///
  /// ## Parameters
  ///
  /// * [value] — CancellationToken is supported by ATS.
  Future<TestCallbackContext> setCancellationToken(CancellationToken value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken', args);
    return TestCallbackContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken'), transport);
  }

  /// Sets the Name property
  Future<TestCallbackContext> setName(String value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName', args);
    return TestCallbackContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName'), transport);
  }

  /// Sets the Value property
  Future<TestCallbackContext> setValue(num value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue', args);
    return TestCallbackContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue'), transport);
  }

  /// Gets the Value property
  Future<num?> value() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value', args);
    return AspireRuntime.asNum(result);
  }
}

/// Test context with collection properties to verify consistent code generation. Verifies both List and Dictionary properties generate proper getter patterns.
class TestCollectionContext extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext` object.
  const TestCollectionContext(super.handle, super.transport);

  /// List property - should generate AspireList getter like Dictionary properties.
  Future<AspireList<String?>> items() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items', args);
    return AspireList<String?>(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items'), transport, (Object? item) => AspireRuntime.asString(item));
  }

  /// Dictionary property - already works with AspireDict getter.
  Future<AspireDict<String?>> metadata() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata', args);
    return AspireDict<String?>(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata'), transport, (Object? item) => AspireRuntime.asString(item));
  }
}

/// A handle to the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource` object in the AppHost.
class TestDatabaseResource extends AspireObject implements Resource, ResourceWithEnvironment {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource` object.
  const TestDatabaseResource(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<TestDatabaseResource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Configures environment with callback (test version)
  Future<TestDatabaseResource> testWithEnvironmentCallback(TestEnvironmentCallback callback) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['callback'] = (Object? a0) async {
      final TestEnvironmentContext p0 = TestEnvironmentContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
      await callback(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
  }

  /// Performs a cancellable operation
  Future<TestDatabaseResource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestDatabaseResource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<TestDatabaseResource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestDatabaseResource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a data volume
  Future<TestDatabaseResource> withDataVolume({String? name}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (name != null) {
      args['name'] = name;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDataVolume', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDataVolume'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestDatabaseResource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestDatabaseResource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Sets environment variables
  Future<TestDatabaseResource> withEnvironmentVariables(Map<String, String?> variables) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['variables'] = variables;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables'), transport);
  }

  /// Configures a named endpoint
  Future<TestDatabaseResource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestDatabaseResource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestDatabaseResource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestDatabaseResource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestDatabaseResource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestDatabaseResource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestDatabaseResource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestDatabaseResource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestDatabaseResource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<TestDatabaseResource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestDatabaseResource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestDatabaseResource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<TestDatabaseResource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestDatabaseResource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestDatabaseResource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// Test environment context used in callbacks. Verifies property-like object pattern (ctx.name.get(), ctx.name.set()).
class TestEnvironmentContext extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext` object.
  const TestEnvironmentContext(super.handle, super.transport);

  /// Gets the Description property
  Future<String?> description() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description', args);
    return AspireRuntime.asString(result);
  }

  /// Gets the Name property
  Future<String?> name() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name', args);
    return AspireRuntime.asString(result);
  }

  /// Gets the Priority property
  Future<num?> priority() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority', args);
    return AspireRuntime.asNum(result);
  }

  /// Sets the Description property
  Future<TestEnvironmentContext> setDescription(String value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription', args);
    return TestEnvironmentContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription'), transport);
  }

  /// Sets the Name property
  Future<TestEnvironmentContext> setName(String value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName', args);
    return TestEnvironmentContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName'), transport);
  }

  /// Sets the Priority property
  Future<TestEnvironmentContext> setPriority(num value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority', args);
    return TestEnvironmentContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority'), transport);
  }
}

/// A handle to the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestMutableCollectionContext` object in the AppHost.
class TestMutableCollectionContext extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestMutableCollectionContext` object.
  const TestMutableCollectionContext(super.handle, super.transport);

  /// Gets the Counts property
  Future<AspireDict<num?>> counts() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.counts', args);
    return AspireDict<num?>(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.counts'), transport, (Object? item) => AspireRuntime.asNum(item));
  }

  /// Sets the Counts property
  Future<TestMutableCollectionContext> setCounts(AspireDict<num?> value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.setCounts', args);
    return TestMutableCollectionContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.setCounts'), transport);
  }

  /// Sets the Tags property
  Future<TestMutableCollectionContext> setTags(AspireList<String?> value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.setTags', args);
    return TestMutableCollectionContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.setTags'), transport);
  }

  /// Gets the Tags property
  Future<AspireList<String?>> tags() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.tags', args);
    return AspireList<String?>(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.tags'), transport, (Object? item) => AspireRuntime.asString(item));
  }
}

/// A mutable-property-only resource used to verify that property setters do not require Promise wrappers.
abstract class TestMutablePromiseCollisionResource extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestMutablePromiseCollisionResource` object.
  const TestMutablePromiseCollisionResource(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<TestMutablePromiseCollisionResource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<TestMutablePromiseCollisionResource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestMutablePromiseCollisionResource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<TestMutablePromiseCollisionResource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestMutablePromiseCollisionResource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestMutablePromiseCollisionResource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestMutablePromiseCollisionResource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<TestMutablePromiseCollisionResource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestMutablePromiseCollisionResource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestMutablePromiseCollisionResource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestMutablePromiseCollisionResource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestMutablePromiseCollisionResource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestMutablePromiseCollisionResource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestMutablePromiseCollisionResource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestMutablePromiseCollisionResource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestMutablePromiseCollisionResource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<TestMutablePromiseCollisionResource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestMutablePromiseCollisionResource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestMutablePromiseCollisionResource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<TestMutablePromiseCollisionResource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestMutablePromiseCollisionResource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestMutablePromiseCollisionResource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }

  /// Gets or sets the test value.
  ///
  /// ## Parameters
  ///
  /// * [value] — Gets or sets the test value.
  Future<TestMutablePromiseCollisionResource> setValue(String value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/ITestMutablePromiseCollisionResource.setValue', args);
    return _TestMutablePromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/ITestMutablePromiseCollisionResource.setValue'), transport);
  }

  /// Gets or sets the test value.
  Future<String?> value() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/ITestMutablePromiseCollisionResource.value', args);
    return AspireRuntime.asString(result);
  }
}

// aspire_generated_2.dart - Generated Aspire declarations
// GENERATED CODE - DO NOT EDIT

part of 'aspire.dart';

/// A handle to an object that satisfies the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestMutablePromiseCollisionResourcePromise` contract in the AppHost.
abstract class TestMutablePromiseCollisionResourcePromise extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestMutablePromiseCollisionResourcePromise` object.
  const TestMutablePromiseCollisionResourcePromise(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<TestMutablePromiseCollisionResourcePromise> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<TestMutablePromiseCollisionResourcePromise> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestMutablePromiseCollisionResourcePromise> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<TestMutablePromiseCollisionResourcePromise> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestMutablePromiseCollisionResourcePromise> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestMutablePromiseCollisionResourcePromise> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestMutablePromiseCollisionResourcePromise> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<TestMutablePromiseCollisionResourcePromise> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestMutablePromiseCollisionResourcePromise> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestMutablePromiseCollisionResourcePromise> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestMutablePromiseCollisionResourcePromise> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestMutablePromiseCollisionResourcePromise> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestMutablePromiseCollisionResourcePromise> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestMutablePromiseCollisionResourcePromise> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestMutablePromiseCollisionResourcePromise> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestMutablePromiseCollisionResourcePromise> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<TestMutablePromiseCollisionResourcePromise> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestMutablePromiseCollisionResourcePromise> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestMutablePromiseCollisionResourcePromise> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<TestMutablePromiseCollisionResourcePromise> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestMutablePromiseCollisionResourcePromise> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestMutablePromiseCollisionResourcePromise> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _TestMutablePromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// A handle to an object that satisfies the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResource` contract in the AppHost.
abstract class TestPromiseCollisionResource extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResource` object.
  const TestPromiseCollisionResource(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<TestPromiseCollisionResource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<TestPromiseCollisionResource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestPromiseCollisionResource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<TestPromiseCollisionResource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestPromiseCollisionResource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestPromiseCollisionResource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestPromiseCollisionResource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<TestPromiseCollisionResource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestPromiseCollisionResource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestPromiseCollisionResource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestPromiseCollisionResource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestPromiseCollisionResource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestPromiseCollisionResource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestPromiseCollisionResource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestPromiseCollisionResource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestPromiseCollisionResource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<TestPromiseCollisionResource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestPromiseCollisionResource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestPromiseCollisionResource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<TestPromiseCollisionResource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestPromiseCollisionResource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestPromiseCollisionResource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _TestPromiseCollisionResourceImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// A handle to an object that satisfies the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResourcePromise` contract in the AppHost.
abstract class TestPromiseCollisionResourcePromise extends AspireObject implements Resource {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResourcePromise` object.
  const TestPromiseCollisionResourcePromise(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<TestPromiseCollisionResourcePromise> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Performs a cancellable operation
  Future<TestPromiseCollisionResourcePromise> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestPromiseCollisionResourcePromise> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<TestPromiseCollisionResourcePromise> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestPromiseCollisionResourcePromise> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestPromiseCollisionResourcePromise> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestPromiseCollisionResourcePromise> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Configures a named endpoint
  Future<TestPromiseCollisionResourcePromise> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestPromiseCollisionResourcePromise> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestPromiseCollisionResourcePromise> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestPromiseCollisionResourcePromise> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestPromiseCollisionResourcePromise> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestPromiseCollisionResourcePromise> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestPromiseCollisionResourcePromise> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestPromiseCollisionResourcePromise> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestPromiseCollisionResourcePromise> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<TestPromiseCollisionResourcePromise> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestPromiseCollisionResourcePromise> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestPromiseCollisionResourcePromise> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<TestPromiseCollisionResourcePromise> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestPromiseCollisionResourcePromise> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestPromiseCollisionResourcePromise> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return _TestPromiseCollisionResourcePromiseImpl(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// A handle to the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource` object in the AppHost.
class TestRedisResource extends AspireObject implements Resource, ResourceWithConnectionString, ResourceWithEnvironment {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource` object.
  const TestRedisResource(super.handle, super.transport);

  /// Adds a child database to a test Redis resource
  ///
  /// This method tests the factory method codegen pattern where a method on builder type A
  /// returns builder type B (e.g., SqlServerServerResource.AddDatabase returning SqlServerDatabaseResource).
  Future<TestDatabaseResource> addTestChildDatabase(String name, {String? databaseName}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['name'] = name;
    if (databaseName != null) {
      args['databaseName'] = databaseName;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/addTestChildDatabase', args);
    return TestDatabaseResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/addTestChildDatabase'), transport);
  }

  /// Gets the endpoints
  Future<List<String?>> getEndpoints() async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/getEndpoints', args);
    return AspireRuntime.asList<String?>(result, (Object? item) => AspireRuntime.asString(item));
  }

  /// Gets the metadata for the resource
  Future<AspireDict<String?>> getMetadata() async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/getMetadata', args);
    return AspireDict<String?>(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/getMetadata'), transport, (Object? item) => AspireRuntime.asString(item));
  }

  /// Gets the status of the resource asynchronously
  Future<String?> getStatusAsync({CancellationToken? cancellationToken}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (cancellationToken != null) {
      args['cancellationToken'] = cancellationToken;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/getStatusAsync', args);
    return AspireRuntime.asString(result);
  }

  /// Gets the tags for the resource
  Future<AspireList<String?>> getTags() async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/getTags', args);
    return AspireList<String?>(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/getTags'), transport, (Object? item) => AspireRuntime.asString(item));
  }

  /// Waits for another resource (test version)
  Future<TestRedisResource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Configures environment with callback (test version)
  Future<TestRedisResource> testWithEnvironmentCallback(TestEnvironmentCallback callback) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['callback'] = (Object? a0) async {
      final TestEnvironmentContext p0 = TestEnvironmentContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
      await callback(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
  }

  /// Waits for the resource to be ready
  Future<bool?> waitForReadyAsync(num timeout, {CancellationToken? cancellationToken}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['timeout'] = timeout;
    if (cancellationToken != null) {
      args['cancellationToken'] = cancellationToken;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/waitForReadyAsync', args);
    return AspireRuntime.asBool(result);
  }

  /// Performs a cancellable operation
  Future<TestRedisResource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures a Redis resource with the concrete vault resource as a parameter.
  ///
  /// ## Parameters
  ///
  /// * [resource] — The parameter-only concrete vault resource.
  ///
  /// ## Returns
  ///
  /// The Redis resource builder.
  Future<TestRedisResource> withConcreteVaultResource(TestVaultResource resource) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['resource'] = resource.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConcreteVaultResource', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConcreteVaultResource'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestRedisResource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the connection string using a reference expression
  Future<TestRedisResource> withConnectionString(ReferenceExpression connectionString) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['connectionString'] = connectionString;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionString', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionString'), transport);
  }

  /// Sets connection string using direct interface target
  Future<TestRedisResource> withConnectionStringDirect(String connectionString) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['connectionString'] = connectionString;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionStringDirect', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConnectionStringDirect'), transport);
  }

  /// Sets the correlation ID
  Future<TestRedisResource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestRedisResource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a data volume with persistence
  Future<TestRedisResource> withDataVolume({String? name, bool? isReadOnly}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (name != null) {
      args['name'] = name;
    }
    if (isReadOnly != null) {
      args['isReadOnly'] = isReadOnly;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDataVolume', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDataVolume'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestRedisResource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestRedisResource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Sets environment variables
  Future<TestRedisResource> withEnvironmentVariables(Map<String, String?> variables) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['variables'] = variables;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables'), transport);
  }

  /// Configures a named endpoint
  Future<TestRedisResource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestRedisResource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestRedisResource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestRedisResource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestRedisResource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestRedisResource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestRedisResource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestRedisResource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestRedisResource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Tests multi-param callback destructuring
  Future<TestRedisResource> withMultiParamHandleCallback(TestCallbackTestEnvironmentCallback callback) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['callback'] = (Object? a0, Object? a1) async {
      final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMultiParamHandleCallback'), transport);
      final TestEnvironmentContext p1 = TestEnvironmentContext(AspireRuntime.requireHandle(a1, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMultiParamHandleCallback'), transport);
      await callback(p0, p1);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMultiParamHandleCallback', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMultiParamHandleCallback'), transport);
  }

  /// Configures a Redis resource with mutable-property and parameter-only resources whose generated names collide.
  ///
  /// ## Parameters
  ///
  /// * [resource] — The mutable-property-only resource whose unused Promise wrapper would collide.
  /// * [resourcePromise] — The parameter-only resource whose generated name matches that Promise wrapper.
  ///
  /// ## Returns
  ///
  /// The Redis resource builder.
  Future<TestRedisResource> withMutablePromiseCollisionResources(TestMutablePromiseCollisionResource resource, TestMutablePromiseCollisionResourcePromise resourcePromise) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['resource'] = resource.handle;
    args['resourcePromise'] = resourcePromise.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMutablePromiseCollisionResources', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMutablePromiseCollisionResources'), transport);
  }

  /// Configures with nested DTO
  Future<TestRedisResource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestRedisResource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestRedisResource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Configures the Redis resource with persistence
  Future<TestRedisResource> withPersistence({TestPersistenceMode? mode}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (mode != null) {
      args['mode'] = TestPersistenceMode.toWireOf(mode);
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withPersistence', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withPersistence'), transport);
  }

  /// Configures a Redis resource with parameter-only resources whose generated names collide.
  ///
  /// ## Parameters
  ///
  /// * [resource] — The resource whose unused Promise wrapper would collide.
  /// * [resourcePromise] — The resource whose generated name matches that Promise wrapper.
  ///
  /// ## Returns
  ///
  /// The Redis resource builder.
  Future<TestRedisResource> withPromiseCollisionResources(TestPromiseCollisionResource resource, TestPromiseCollisionResourcePromise resourcePromise) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['resource'] = resource.handle;
    args['resourcePromise'] = resourcePromise.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withPromiseCollisionResources', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withPromiseCollisionResources'), transport);
  }

  /// Redis-specific configuration
  Future<TestRedisResource> withRedisSpecific(String option) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['option'] = option;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withRedisSpecific', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withRedisSpecific'), transport);
  }

  /// Sets the resource status
  Future<TestRedisResource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestRedisResource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestRedisResource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return TestRedisResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }
}

/// Test context type with exposed instance methods. Verifies [AspireExport(ExposeMethods=true)] generates async methods.
class TestResourceContext extends AspireObject {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext` object.
  const TestResourceContext(super.handle, super.transport);

  /// Instance method that should be exposed as async method.
  Future<String?> getValueAsync() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync', args);
    return AspireRuntime.asString(result);
  }

  /// Gets the Name property
  Future<String?> name() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name', args);
    return AspireRuntime.asString(result);
  }

  /// Sets the Name property
  Future<TestResourceContext> setName(String value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName', args);
    return TestResourceContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName'), transport);
  }

  /// Sets the Value property
  Future<TestResourceContext> setValue(num value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue', args);
    return TestResourceContext(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue'), transport);
  }

  /// Instance method with parameter.
  Future<void> setValueAsync(String value) async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    args['value'] = value;
    await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync', args);
  }

  /// Instance method with return type.
  Future<bool?> validateAsync() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync', args);
    return AspireRuntime.asBool(result);
  }

  /// Gets the Value property
  Future<num?> value() async {
    final Map<String, Object?> args = <String, Object?>{'context': handle};
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value', args);
    return AspireRuntime.asNum(result);
  }
}

/// A handle to the `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource` object in the AppHost.
class TestVaultResource extends AspireObject implements ITestVaultResource, Resource, ResourceWithEnvironment {
  /// Wraps the handle of a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource` object.
  const TestVaultResource(super.handle, super.transport);

  /// Waits for another resource (test version)
  Future<TestVaultResource> testWaitFor(Resource dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWaitFor'), transport);
  }

  /// Configures environment with callback (test version)
  Future<TestVaultResource> testWithEnvironmentCallback(TestEnvironmentCallback callback) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['callback'] = (Object? a0) async {
      final TestEnvironmentContext p0 = TestEnvironmentContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
      await callback(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/testWithEnvironmentCallback'), transport);
  }

  /// Performs a cancellable operation
  Future<TestVaultResource> withCancellableOperation(CancellationTokenCallback operation) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['operation'] = (Object? a0) async {
      final CancellationToken? p0 = AspireRuntime.asCancellationToken(a0);
      await operation(p0);
      return null;
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCancellableOperation'), transport);
  }

  /// Configures the resource with a DTO
  Future<TestVaultResource> withConfig(TestConfigDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withConfig'), transport);
  }

  /// Sets the correlation ID
  Future<TestVaultResource> withCorrelationId(String correlationId) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['correlationId'] = correlationId;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCorrelationId'), transport);
  }

  /// Sets the created timestamp
  Future<TestVaultResource> withCreatedAt(DateTime createdAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['createdAt'] = createdAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withCreatedAt'), transport);
  }

  /// Adds a dependency on another resource
  Future<TestVaultResource> withDependency(ResourceWithConnectionString dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = dependency.handle;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withDependency'), transport);
  }

  /// Sets the endpoints
  Future<TestVaultResource> withEndpoints(List<String?> endpoints) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpoints'] = endpoints;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEndpoints'), transport);
  }

  /// Sets environment variables
  Future<TestVaultResource> withEnvironmentVariables(Map<String, String?> variables) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['variables'] = variables;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withEnvironmentVariables'), transport);
  }

  /// Configures a named endpoint
  Future<TestVaultResource> withMergeEndpoint(String endpointName, num port) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpoint'), transport);
  }

  /// Configures a named endpoint with scheme
  Future<TestVaultResource> withMergeEndpointScheme(String endpointName, num port, String scheme) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['endpointName'] = endpointName;
    args['port'] = port;
    args['scheme'] = scheme;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeEndpointScheme'), transport);
  }

  /// Adds a label to the resource
  Future<TestVaultResource> withMergeLabel(String label) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabel'), transport);
  }

  /// Adds a categorized label to the resource
  Future<TestVaultResource> withMergeLabelCategorized(String label, String category) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['label'] = label;
    args['category'] = category;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLabelCategorized'), transport);
  }

  /// Configures resource logging
  Future<TestVaultResource> withMergeLogging(String logLevel, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLogging'), transport);
  }

  /// Configures resource logging with file path
  Future<TestVaultResource> withMergeLoggingPath(String logLevel, String logPath, {bool? enableConsole, num? maxFiles}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['logLevel'] = logLevel;
    args['logPath'] = logPath;
    if (enableConsole != null) {
      args['enableConsole'] = enableConsole;
    }
    if (maxFiles != null) {
      args['maxFiles'] = maxFiles;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeLoggingPath'), transport);
  }

  /// Configures a route
  Future<TestVaultResource> withMergeRoute(String path, String method, String handler, num priority) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRoute'), transport);
  }

  /// Configures a route with middleware
  Future<TestVaultResource> withMergeRouteMiddleware(String path, String method, String handler, num priority, String middleware) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['path'] = path;
    args['method'] = method;
    args['handler'] = handler;
    args['priority'] = priority;
    args['middleware'] = middleware;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withMergeRouteMiddleware'), transport);
  }

  /// Sets the modified timestamp
  Future<TestVaultResource> withModifiedAt(DateTime modifiedAt) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['modifiedAt'] = modifiedAt;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withModifiedAt'), transport);
  }

  /// Configures with nested DTO
  Future<TestVaultResource> withNestedConfig(TestNestedDto config) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['config'] = config.toJson();
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withNestedConfig'), transport);
  }

  /// Configures with optional callback
  Future<TestVaultResource> withOptionalCallback({TestCallback? callback}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (callback != null) {
      final TestCallback callbackCallback = callback;
      args['callback'] = (Object? a0) async {
        final TestCallbackContext p0 = TestCallbackContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
        await callbackCallback(p0);
        return null;
      };
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalCallback'), transport);
  }

  /// Adds an optional string parameter
  Future<TestVaultResource> withOptionalString({String? value, bool? enabled}) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    if (value != null) {
      args['value'] = value;
    }
    if (enabled != null) {
      args['enabled'] = enabled;
    }
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withOptionalString'), transport);
  }

  /// Sets the resource status
  Future<TestVaultResource> withStatus(TestResourceStatus status) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['status'] = TestResourceStatus.toWireOf(status);
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withStatus'), transport);
  }

  /// Adds a dependency from a string or another resource
  Future<TestVaultResource> withUnionDependency(Object? dependency) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['dependency'] = AspireRuntime.requireUnion(dependency, (Object? value) => value is ResourceWithConnectionString || value is String || value is TestRedisResource, const <String>['ResourceWithConnectionString', 'String', 'TestRedisResource'], 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', 'dependency');
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withUnionDependency'), transport);
  }

  /// Adds validation callback
  Future<TestVaultResource> withValidator(TestResourceCallback validator) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['validator'] = (Object? a0) async {
      final TestResourceContext p0 = TestResourceContext(AspireRuntime.requireHandle(a0, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
      return await validator(p0);
    };
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withValidator'), transport);
  }

  /// Configures vault using direct interface target
  Future<TestVaultResource> withVaultDirect(String option) async {
    final Map<String, Object?> args = <String, Object?>{'builder': handle};
    args['option'] = option;
    final Object? result = await transport.invokeCapability('Aspire.Hosting.CodeGeneration.Dart.Tests/withVaultDirect', args);
    return TestVaultResource(AspireRuntime.requireHandle(result, 'Aspire.Hosting.CodeGeneration.Dart.Tests/withVaultDirect'), transport);
  }
}

/// The value that a decoder builds for a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestMutablePromiseCollisionResource` handle.
///
/// [TestMutablePromiseCollisionResource] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _TestMutablePromiseCollisionResourceImpl extends TestMutablePromiseCollisionResource {
  const _TestMutablePromiseCollisionResourceImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestMutablePromiseCollisionResourcePromise` handle.
///
/// [TestMutablePromiseCollisionResourcePromise] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _TestMutablePromiseCollisionResourcePromiseImpl extends TestMutablePromiseCollisionResourcePromise {
  const _TestMutablePromiseCollisionResourcePromiseImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResource` handle.
///
/// [TestPromiseCollisionResource] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _TestPromiseCollisionResourceImpl extends TestPromiseCollisionResource {
  const _TestPromiseCollisionResourceImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResourcePromise` handle.
///
/// [TestPromiseCollisionResourcePromise] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _TestPromiseCollisionResourcePromiseImpl extends TestPromiseCollisionResourcePromise {
  const _TestPromiseCollisionResourcePromiseImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource` handle.
///
/// [ITestVaultResource] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _ITestVaultResourceImpl extends ITestVaultResource {
  const _ITestVaultResourceImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource` handle.
///
/// [Resource] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _ResourceImpl extends Resource {
  const _ResourceImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString` handle.
///
/// [ResourceWithConnectionString] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _ResourceWithConnectionStringImpl extends ResourceWithConnectionString {
  const _ResourceWithConnectionStringImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment` handle.
///
/// [ResourceWithEnvironment] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _ResourceWithEnvironmentImpl extends ResourceWithEnvironment {
  const _ResourceWithEnvironmentImpl(super.handle, super.transport);
}

/// The value that a decoder builds for a `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder` handle.
///
/// [DistributedApplicationBuilder] is abstract, because the .NET type is an interface. This class adds no
/// member, so the value keeps every capability of the interface.
class _DistributedApplicationBuilderImpl extends DistributedApplicationBuilder {
  const _DistributedApplicationBuilderImpl(super.handle, super.transport);
}
