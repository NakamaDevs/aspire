// Base types for the Aspire Dart SDK.
//
// This file is copied verbatim into `.aspire/modules/`. It uses the Dart SDK
// libraries only, so a generated project needs no package dependency.
//
// Keep `base.dart` and `transport.dart` in the same directory.
// `CancellationToken.cancel` needs the transport, and the transport needs the
// types in this file. Dart allows the import cycle.

import 'transport.dart';

/// An object that knows its own ATS wire form.
///
/// [AspireMarshal.encode] calls [toWire], so a generated data object, a
/// generated enum, a handle wrapper and a reference expression all travel
/// without a separate conversion at the call site.
abstract interface class AspireWireValue {
  /// Returns the wire form of the value.
  Object? toWire();
}

/// A reference to an object that lives in the .NET AppHost.
///
/// A handle travels on the wire as `{"$handle": id, "$type": type}`.
class AspireHandle {
  /// Builds a handle.
  const AspireHandle(this.id, this.type);

  /// Builds a handle from its wire form.
  factory AspireHandle.fromJson(Map<Object?, Object?> json) {
    final Object? id = json[r'$handle'];
    final Object? type = json[r'$type'];
    if (id is! String || type is! String) {
      throw ArgumentError.value(
        json,
        'json',
        'is not the wire form of a handle',
      );
    }
    return AspireHandle(id, type);
  }

  /// The handle identifier that the host assigned.
  final String id;

  /// The ATS type identifier, such as `Aspire.Hosting/Aspire.Hosting.DistributedApplication`.
  final String type;

  /// Returns the wire form of the handle.
  Map<String, Object?> toJson() => <String, Object?>{
    r'$handle': id,
    r'$type': type,
  };

  /// Returns true when the value is the wire form of a handle.
  static bool isHandleJson(Object? value) =>
      value is Map && value[r'$handle'] is String && value[r'$type'] is String;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is AspireHandle && other.id == id && other.type == type);

  @override
  int get hashCode => Object.hash(id, type);

  @override
  String toString() => 'AspireHandle($type, $id)';
}

/// The error codes that the host puts in `result.$error`.
abstract final class AspireErrorCodes {
  /// The host does not know the capability identifier.
  static const String capabilityNotFound = 'CAPABILITY_NOT_FOUND';

  /// The handle does not exist, or the host disposed it.
  static const String handleNotFound = 'HANDLE_NOT_FOUND';

  /// The handle type does not satisfy the type constraint of the capability.
  static const String typeMismatch = 'TYPE_MISMATCH';

  /// An argument is absent, or it has the wrong type.
  static const String invalidArgument = 'INVALID_ARGUMENT';

  /// An argument value is outside the permitted range.
  static const String argumentOutOfRange = 'ARGUMENT_OUT_OF_RANGE';

  /// A guest callback failed.
  static const String callbackError = 'CALLBACK_ERROR';

  /// The host failed for an unexpected reason.
  static const String internalError = 'INTERNAL_ERROR';

  /// The `REMOTE_APP_HOST_SOCKET_PATH` environment variable is not set.
  static const String missingSocketPath = 'MISSING_SOCKET_PATH';

  /// The guest cannot open the AppHost socket.
  static const String connectionFailed = 'CONNECTION_FAILED';

  /// The AppHost closed the connection.
  static const String connectionClosed = 'CONNECTION_CLOSED';

  /// The AppHost rejected the guest token.
  static const String authenticationFailed = 'AUTHENTICATION_FAILED';

  /// The socket failed.
  static const String transportError = 'TRANSPORT_ERROR';

  /// The caller used [AspireTransport.defaultInstance] before it connected.
  static const String notConnected = 'NOT_CONNECTED';
}

/// An error that the AppHost returned, or a transport failure.
///
/// | Code | Source |
/// |---|---|
/// | `CAPABILITY_NOT_FOUND`, `HANDLE_NOT_FOUND`, `TYPE_MISMATCH`, `INVALID_ARGUMENT`, `ARGUMENT_OUT_OF_RANGE`, `CALLBACK_ERROR`, `INTERNAL_ERROR` | the host, in `result.$error` |
/// | `MISSING_SOCKET_PATH`, `CONNECTION_FAILED`, `CONNECTION_CLOSED`, `AUTHENTICATION_FAILED`, `TRANSPORT_ERROR`, `NOT_CONNECTED` | the guest transport |
class AspireError implements Exception {
  /// Builds an error.
  AspireError({required this.message, this.code, this.data, this.capability});

  /// Builds an error from the `$error` object of an ATS result.
  factory AspireError.fromAts(Map<Object?, Object?> error) => AspireError(
    code: _asString(error['code']),
    message: _asString(error['message']) ?? 'Capability invocation failed',
    capability: _asString(error['capability']),
    data: error,
  );

  /// Builds an error from a JSON-RPC `error` object.
  ///
  /// The host puts the ATS code in `data.code` when it has one. The numeric
  /// JSON-RPC code is the fallback.
  factory AspireError.fromJsonRpc(Map<Object?, Object?> error) {
    final Object? data = error['data'];
    final Map<Object?, Object?>? details = data is Map<Object?, Object?>
        ? data
        : null;
    return AspireError(
      code: _asString(details?['code']) ?? _asString(error['code']),
      message: _asString(error['message']) ?? 'JSON-RPC error',
      capability: _asString(details?['capability']),
      data: data,
    );
  }

  /// A machine-readable code. See [AspireErrorCodes].
  final String? code;

  /// A human-readable message.
  final String message;

  /// The payload that the host sent, when it sent one.
  final Object? data;

  /// The capability that failed, when the host named one.
  final String? capability;

  /// Returns a readable rendering of the error on more than one line.
  String get formatted => <String>[
    'Aspire Error: $message',
    if (code != null) '  Code: $code',
    if (capability != null) '  Capability: $capability',
  ].join('\n');

  @override
  String toString() =>
      code == null ? 'AspireError: $message' : 'AspireError($code): $message';

  static String? _asString(Object? value) => value is String
      ? value
      : value == null
      ? null
      : '$value';
}

int _tokenCounter = 0;

/// A cooperative cancellation token.
///
/// The identifier travels on the wire as a plain string. [cancel] sends the
/// `cancelToken` request to the host.
class CancellationToken {
  /// Wraps an identifier that the host supplied.
  const CancellationToken(this.id);

  /// Creates a token with a new identifier.
  factory CancellationToken.create() => CancellationToken(
    'ct_${++_tokenCounter}_${DateTime.now().millisecondsSinceEpoch}',
  );

  /// The opaque token identifier. Do not parse it.
  final String id;

  /// Cancels the token on the host.
  ///
  /// Returns true when the host found and cancelled the token. The default
  /// transport is [AspireTransport.defaultInstance].
  Future<bool> cancel([AspireTransport? transport]) =>
      (transport ?? AspireTransport.defaultInstance).cancelToken(id);

  @override
  bool operator ==(Object other) =>
      identical(this, other) || (other is CancellationToken && other.id == id);

  @override
  int get hashCode => id.hashCode;

  @override
  String toString() => 'CancellationToken($id)';
}

/// Converts values between Dart objects and the ATS wire form.
///
/// Generated code calls [encode] before it sends arguments and [decode] after
/// it receives a result.
abstract final class AspireMarshal {
  /// Converts a Dart object into a JSON-ready object.
  ///
  /// A handle becomes `{"$handle": id, "$type": type}`, a cancellation token
  /// becomes its identifier, and a [DateTime] becomes an ISO 8601 string. An
  /// [AspireWireValue], such as a generated data object, a generated enum or a
  /// handle wrapper, becomes the value that its `toWire` returns.
  ///
  /// A function has no wire form. Register it with
  /// [AspireTransport.registerCallback] first, or pass it as a capability
  /// argument, because [AspireTransport.invokeCapability] registers it.
  static Object? encode(Object? value) {
    if (value is AspireHandle) {
      return value.toJson();
    }
    if (value is AspireWireValue) {
      return encode(value.toWire());
    }
    if (value is CancellationToken) {
      return value.id;
    }
    if (value is DateTime) {
      return value.toIso8601String();
    }
    if (value is Function) {
      throw ArgumentError.value(
        value,
        'value',
        'a function has no wire form: register it with '
            'AspireTransport.registerCallback, or pass it to invokeCapability',
      );
    }
    if (value is Map) {
      return <String, Object?>{
        for (final MapEntry<Object?, Object?> entry in value.entries)
          '${entry.key}': encode(entry.value),
      };
    }
    if (value is Iterable) {
      return <Object?>[for (final Object? item in value) encode(item)];
    }
    return value;
  }

  /// Converts a decoded JSON object into a Dart object.
  ///
  /// Every `{"$handle": id, "$type": type}` map becomes an [AspireHandle],
  /// inside maps and lists as well.
  static Object? decode(Object? value) {
    if (value is Map) {
      if (AspireHandle.isHandleJson(value)) {
        return AspireHandle.fromJson(value.cast<Object?, Object?>());
      }
      return <String, Object?>{
        for (final MapEntry<Object?, Object?> entry in value.entries)
          '${entry.key}': decode(entry.value),
      };
    }
    if (value is List) {
      return <Object?>[for (final Object? item in value) decode(item)];
    }
    return value;
  }
}
