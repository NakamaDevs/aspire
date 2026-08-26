// Runtime helpers for the Aspire Dart SDK.
//
// This file is copied verbatim into `.aspire/modules/`. It uses the Dart SDK
// libraries only, so a generated project needs no package dependency.
//
// The generated code in `aspire_generated*.dart` calls these helpers. Keeping
// the shared logic here keeps the generated files small.
//
// Keep `base.dart` and `transport.dart` in the same directory.

import 'base.dart';
import 'transport.dart';

/// The base class of every generated handle wrapper.
///
/// A wrapper holds the [AspireHandle] that the host returned and the
/// [AspireTransport] that produced it. Every capability call goes back to the
/// same transport, so one AppHost can hold more than one connection.
abstract class AspireObject {
  /// Builds a wrapper around a host handle.
  const AspireObject(this.handle, this.transport);

  /// The handle that the host assigned to the object.
  final AspireHandle handle;

  /// The transport that returned the handle.
  final AspireTransport transport;

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
  static String? asString(Object? value) =>
      value == null ? null : value is String ? value : '$value';

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
}

/// A mutable `List<T>` that lives in the .NET AppHost.
///
/// The host returns a handle, and every operation is a capability call. The
/// element values are plain JSON values, or an [AspireHandle] when the list
/// holds handle types.
class AspireList extends AspireObject {
  /// Wraps a list handle.
  const AspireList(super.handle, super.transport);

  /// Returns every element as a Dart list.
  Future<List<Object?>> toList() async {
    final Object? result = await _invoke('toArray');
    return result is List ? List<Object?>.of(result) : <Object?>[];
  }

  /// Returns the number of elements.
  Future<int> get length async {
    final Object? result = await _invoke('length');
    return result is num ? result.toInt() : 0;
  }

  /// Returns the element at [index].
  Future<Object?> elementAt(int index) => _invoke('get', <String, Object?>{
    'index': index,
  });

  /// Adds [value] to the end of the list.
  Future<void> add(Object? value) =>
      _invoke('add', <String, Object?>{'item': value});

  /// Replaces the element at [index].
  Future<void> setAt(int index, Object? value) =>
      _invoke('set', <String, Object?>{'index': index, 'item': value});

  /// Inserts [value] at [index].
  Future<void> insert(int index, Object? value) =>
      _invoke('insert', <String, Object?>{'index': index, 'item': value});

  /// Returns the index of [value], or -1.
  Future<int> indexOf(Object? value) async {
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
class AspireDict extends AspireObject {
  /// Wraps a dictionary handle.
  const AspireDict(super.handle, super.transport);

  /// Returns every entry as a Dart map.
  Future<Map<String, Object?>> toMap() async {
    final Object? result = await _invoke('toObject');
    return AspireRuntime.asObject(result);
  }

  /// Returns the number of entries.
  Future<int> get length async {
    final Object? result = await _invoke('count');
    return result is num ? result.toInt() : 0;
  }

  /// Returns the value of [key].
  Future<Object?> operator [](Object? key) =>
      _invoke('get', <String, Object?>{'key': key});

  /// Writes [value] under [key].
  Future<void> set(Object? key, Object? value) =>
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
  Future<List<Object?>> values() async {
    final Object? result = await _invoke('values');
    return result is List ? List<Object?>.of(result) : <Object?>[];
  }

  /// Removes every entry.
  Future<void> clear() => _invoke('clear');

  Future<Object?> _invoke(String name, [Map<String, Object?>? args]) =>
      transport.invokeCapability('Aspire.Hosting/Dict.$name', <String, Object?>{
        'dict': handle,
        ...?args,
      });
}
