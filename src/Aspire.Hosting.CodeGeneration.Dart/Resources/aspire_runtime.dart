// Runtime helpers for the Aspire Dart SDK.
//
// This file is copied verbatim into `.aspire/modules/`. It uses the Dart SDK
// libraries only, so a generated project needs no package dependency.
//
// The generated code in `aspire_generated*.dart` calls these helpers. Keeping
// the shared logic here keeps the generated files small.
//
// Keep `base.dart` and `transport.dart` in the same directory.

import 'dart:async';

import 'base.dart';
import 'transport.dart';

/// The base class of every generated handle wrapper.
///
/// A wrapper holds the [AspireHandle] that the host returned and the
/// [AspireTransport] that produced it. Every capability call goes back to the
/// same transport, so one AppHost can hold more than one connection.
abstract class AspireObject implements AspireWireValue {
  /// Builds a wrapper around a host handle.
  const AspireObject(this.handle, this.transport);

  /// The handle that the host assigned to the object.
  final AspireHandle handle;

  /// The transport that returned the handle.
  final AspireTransport transport;

  @override
  Object? toWire() => handle.toJson();

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is AspireObject &&
          other.runtimeType == runtimeType &&
          other.handle == handle);

  @override
  int get hashCode => Object.hash(runtimeType, handle);

  @override
  String toString() => '$runtimeType(${handle.id})';
}

/// Converts capability results into Dart values.
///
/// Every function accepts the value that
/// [AspireTransport.invokeCapability] returned. That value is already decoded:
/// a handle is an [AspireHandle], and every other value is a plain JSON
/// object.
abstract final class AspireRuntime {
  /// Returns the handle in [value].
  ///
  /// Throws [AspireError] with the code `INVALID_ARGUMENT` when the capability
  /// returned something else. A capability that declares a handle return type
  /// always returns a handle, so a failure here means the host and the
  /// generated SDK disagree.
  static AspireHandle requireHandle(Object? value, String capability) {
    if (value is AspireHandle) {
      return value;
    }
    throw AspireError(
      code: AspireErrorCodes.invalidArgument,
      message:
          'The capability $capability returned ${value == null ? 'null' : value.runtimeType} '
          'where the generated SDK expects a handle.',
      capability: capability,
      data: value,
    );
  }

  /// Returns [value] as a string, or null.
  static String? asString(Object? value) => value == null
      ? null
      : value is String
      ? value
      : '$value';

  /// Returns [value] as a number, or null.
  static num? asNum(Object? value) => value is num
      ? value
      : value is String
      ? num.tryParse(value)
      : null;

  /// Returns [value] as a boolean, or null.
  static bool? asBool(Object? value) => value is bool ? value : null;

  /// Returns [value] as a date and time, or null.
  static DateTime? asDateTime(Object? value) => value is String
      ? DateTime.tryParse(value)
      : value is DateTime
      ? value
      : null;

  /// Returns [value] as a cancellation token, or null.
  static CancellationToken? asCancellationToken(Object? value) =>
      value is CancellationToken
      ? value
      : value is String
      ? CancellationToken(value)
      : null;

  /// Converts every element of [value] with [element].
  ///
  /// A value that is not a list becomes an empty list, so a capability that
  /// returned null never fails the caller.
  static List<T> asList<T>(Object? value, T Function(Object?) element) =>
      value is List
      ? <T>[for (final Object? item in value) element(item)]
      : <T>[];

  /// Converts every value of [value] with [entry], keyed by its string key.
  static Map<String, T> asMap<T>(Object? value, T Function(Object?) entry) =>
      value is Map
      ? <String, T>{
          for (final MapEntry<Object?, Object?> pair in value.entries)
            '${pair.key}': entry(pair.value),
        }
      : <String, T>{};

  /// Returns [value] as a JSON object, or an empty map.
  static Map<String, Object?> asObject(Object? value) => value is Map
      ? <String, Object?>{
          for (final MapEntry<Object?, Object?> pair in value.entries)
            '${pair.key}': pair.value,
        }
      : <String, Object?>{};

  /// Returns the value unchanged. Generated code uses it where a decoder is
  /// required but the wire value already has the right shape.
  static Object? identity(Object? value) => value;

  /// Returns [value] when it is a function of the type [T], or null.
  ///
  /// A callback property of a data object holds a guest function. The host
  /// sends a callback identifier instead, and the guest cannot invoke it, so a
  /// property that arrives from the host stays null.
  static T? asCallback<T extends Function>(Object? value) =>
      value is T ? value : null;

  /// Returns [value] when [accepts] takes it.
  ///
  /// A `[AspireUnion]` parameter accepts more than one type, so Dart types it
  /// as `Object?`. The guard turns a wrong argument into an [ArgumentError]
  /// that names every accepted type, instead of a host error later on.
  static Object? requireUnion(
    Object? value,
    bool Function(Object?) accepts,
    List<String> accepted,
    String capability,
    String parameter,
  ) {
    if (accepts(value)) {
      return value;
    }
    throw ArgumentError.value(
      value,
      parameter,
      'the capability $capability does not accept it. '
      'It accepts: ${accepted.join(', ')}',
    );
  }
}

/// Builds a reference expression from [parts].
///
/// A [String] part is literal text. Every other part becomes a value provider
/// and gets a `{n}` placeholder in the format string. The AppHost resolves the
/// expression when it needs the value.
///
/// ```dart
/// final url = ref(<Object?>['http://', endpoint, '/health']);
/// ```
ReferenceExpression ref(List<Object?> parts) =>
    ReferenceExpression.fromParts(parts);

/// A value that references endpoints, parameters and other value providers.
///
/// [ref] builds an expression in the guest. The wire form is
/// `{"$expr": {"format": "...", "valueProviders": [...]}}`.
///
/// The host also returns a reference expression. Such an expression carries a
/// handle and no format, and [getValueAsync] resolves it on the host.
class ReferenceExpression implements AspireWireValue {
  /// Wraps a reference expression that the host returned.
  const ReferenceExpression(
    AspireHandle this.handle,
    AspireTransport this.transport,
  ) : format = null,
      valueProviders = null;

  const ReferenceExpression._(this.format, this.valueProviders)
    : handle = null,
      transport = null;

  /// Builds an expression from [parts]. See [ref].
  factory ReferenceExpression.fromParts(List<Object?> parts) {
    final StringBuffer format = StringBuffer();
    final List<Object?> providers = <Object?>[];

    for (final Object? part in parts) {
      if (part is String) {
        format.write(part);
        continue;
      }
      format.write('{${providers.length}}');
      providers.add(part);
    }

    return ReferenceExpression._(
      format.toString(),
      List<Object?>.unmodifiable(providers),
    );
  }

  /// The capability that resolves an expression on the host.
  static const String getValueCapability =
      'Aspire.Hosting.ApplicationModel/getValueAsync';

  /// The handle of an expression that the host returned, or null.
  final AspireHandle? handle;

  /// The transport that returned the handle, or null.
  final AspireTransport? transport;

  /// The format string of a guest expression, or null.
  final String? format;

  /// The value providers of a guest expression, or null.
  final List<Object?>? valueProviders;

  @override
  Object? toWire() {
    final AspireHandle? id = handle;
    if (id != null) {
      return id.toJson();
    }

    final List<Object?> providers = valueProviders ?? const <Object?>[];
    final Map<String, Object?> expression = <String, Object?>{
      'format': format ?? '',
      if (providers.isNotEmpty)
        'valueProviders': <Object?>[
          for (final Object? provider in providers) _provider(provider),
        ],
    };

    return <String, Object?>{r'$expr': expression};
  }

  /// Resolves the expression on the host.
  ///
  /// The host resolves only an expression that it returned. A guest expression
  /// carries no handle, so the call throws [AspireError] with the code
  /// `INVALID_ARGUMENT`.
  Future<String?> getValueAsync({CancellationToken? cancellationToken}) async {
    final AspireHandle? id = handle;
    final AspireTransport? connection = transport;
    if (id == null || connection == null) {
      throw AspireError(
        code: AspireErrorCodes.invalidArgument,
        message:
            'getValueAsync needs a reference expression that the host returned.',
        capability: getValueCapability,
      );
    }

    final Object? result = await connection
        .invokeCapability(getValueCapability, <String, Object?>{
          'context': id,
          if (cancellationToken != null) 'cancellationToken': cancellationToken,
        });
    return AspireRuntime.asString(result);
  }

  static Object? _provider(Object? value) {
    if (value is String || value is num) {
      return '$value';
    }
    if (value is AspireWireValue || value is AspireHandle) {
      return AspireMarshal.encode(value);
    }
    throw ArgumentError.value(
      value,
      'part',
      'a reference expression part is a string, a number or a handle',
    );
  }

  @override
  String toString() => handle == null
      ? 'ReferenceExpression($format)'
      : 'ReferenceExpression(${handle?.id})';
}

/// A mutable `List<T>` that lives in the .NET AppHost.
///
/// The host returns a handle, and every operation is a capability call. The
/// generator supplies [decoder], so an element arrives as the Dart type that
/// the ATS element type names. [AspireMarshal.encode] converts an element that
/// travels the other way.
class AspireList<T> extends AspireObject {
  /// Wraps a list handle. [decoder] converts one wire element into a `T`.
  const AspireList(super.handle, super.transport, this.decoder);

  /// Converts one wire element into a `T`.
  final T Function(Object?) decoder;

  /// Returns every element as a Dart list.
  Future<List<T>> toList() async {
    final Object? result = await _invoke('toArray');
    return AspireRuntime.asList<T>(result, decoder);
  }

  /// Returns the number of elements.
  Future<int> get length async {
    final Object? result = await _invoke('length');
    return result is num ? result.toInt() : 0;
  }

  /// Returns the element at [index].
  Future<T> elementAt(int index) async =>
      decoder(await _invoke('get', <String, Object?>{'index': index}));

  /// Adds [value] to the end of the list.
  Future<void> add(T value) => _invoke('add', <String, Object?>{'item': value});

  /// Replaces the element at [index].
  Future<void> setAt(int index, T value) =>
      _invoke('set', <String, Object?>{'index': index, 'item': value});

  /// Inserts [value] at [index].
  Future<void> insert(int index, T value) =>
      _invoke('insert', <String, Object?>{'index': index, 'item': value});

  /// Returns the index of [value], or -1.
  Future<int> indexOf(T value) async {
    final Object? result = await _invoke('indexOf', <String, Object?>{
      'item': value,
    });
    return result is num ? result.toInt() : -1;
  }

  /// Removes the element at [index].
  Future<void> removeAt(int index) =>
      _invoke('removeAt', <String, Object?>{'index': index});

  /// Removes every element.
  Future<void> clear() => _invoke('clear');

  Future<Object?> _invoke(String name, [Map<String, Object?>? args]) =>
      transport.invokeCapability('Aspire.Hosting/List.$name', <String, Object?>{
        'list': handle,
        ...?args,
      });
}

/// A mutable `Dictionary<TKey, TValue>` that lives in the .NET AppHost.
///
/// The generator supplies [decoder], so a value arrives as the Dart type that
/// the ATS value type names.
class AspireDict<T> extends AspireObject {
  /// Wraps a dictionary handle. [decoder] converts one wire value into a `T`.
  const AspireDict(super.handle, super.transport, this.decoder);

  /// Converts one wire value into a `T`.
  final T Function(Object?) decoder;

  /// Returns every entry as a Dart map.
  Future<Map<String, T>> toMap() async {
    final Object? result = await _invoke('toObject');
    return AspireRuntime.asMap<T>(result, decoder);
  }

  /// Returns the number of entries.
  Future<int> get length async {
    final Object? result = await _invoke('count');
    return result is num ? result.toInt() : 0;
  }

  /// Returns the value of [key].
  Future<T> operator [](Object? key) async =>
      decoder(await _invoke('get', <String, Object?>{'key': key}));

  /// Writes [value] under [key].
  Future<void> set(Object? key, T value) =>
      _invoke('set', <String, Object?>{'key': key, 'value': value});

  /// Returns true when [key] exists.
  Future<bool> containsKey(Object? key) async =>
      await _invoke('has', <String, Object?>{'key': key}) == true;

  /// Removes [key]. Returns true when the key existed.
  Future<bool> remove(Object? key) async =>
      await _invoke('remove', <String, Object?>{'key': key}) == true;

  /// Returns every key.
  Future<List<Object?>> keys() async {
    final Object? result = await _invoke('keys');
    return result is List ? List<Object?>.of(result) : <Object?>[];
  }

  /// Returns every value.
  Future<List<T>> values() async {
    final Object? result = await _invoke('values');
    return AspireRuntime.asList<T>(result, decoder);
  }

  /// Removes every entry.
  Future<void> clear() => _invoke('clear');

  Future<Object?> _invoke(String name, [Map<String, Object?>? args]) =>
      transport.invokeCapability('Aspire.Hosting/Dict.$name', <String, Object?>{
        'dict': handle,
        ...?args,
      });
}
