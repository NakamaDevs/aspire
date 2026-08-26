// JSON-RPC transport for the Aspire Dart SDK.
//
// This file is copied verbatim into `.aspire/modules/`. It uses the Dart SDK
// libraries only, so a generated project needs no package dependency. Keep
// `base.dart` in the same directory.

import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'base.dart';

/// A JSON-RPC 2.0 client for the Aspire AppHost.
///
/// The transport connects to the Unix domain socket in
/// `REMOTE_APP_HOST_SOCKET_PATH` and speaks the LSP frame format:
///
/// ```text
/// Content-Length: <byte count>\r\n\r\n<utf8 json>
/// ```
///
/// The transport never blocks on a reply. [request] stores a [Completer] in a
/// pending map, and the read loop completes it when the response frame
/// arrives. A host callback runs in a separate event-loop task, so a callback
/// can invoke a capability without a deadlock.
///
/// ## Guest to host methods
///
/// | Method | Function |
/// |---|---|
/// | `ping` | [ping] |
/// | `invokeCapability` | [invokeCapability] |
/// | `cancelToken` | [cancelToken] |
/// | `authenticate` | sent by [connect] when `ASPIRE_REMOTE_APPHOST_TOKEN` is set |
///
/// ## Host to guest methods
///
/// | Method | Behaviour |
/// |---|---|
/// | `invokeCallback` | runs the function that [registerCallback] stored |
class AspireTransport {
  AspireTransport._(this._socket, this.socketPath) {
    _subscription = _socket.listen(
      _onData,
      onError: _onError,
      onDone: _onDone,
      cancelOnError: false,
    );
  }

  /// The environment variable that holds the AppHost socket path.
  static const String socketPathEnvironmentVariable =
      'REMOTE_APP_HOST_SOCKET_PATH';

  /// The environment variable that holds the guest authentication token.
  static const String authTokenEnvironmentVariable =
      'ASPIRE_REMOTE_APPHOST_TOKEN';

  /// The default connect timeout.
  static const Duration defaultConnectTimeout = Duration(seconds: 10);

  static const List<int> _headerSeparator = <int>[13, 10, 13, 10];
  static const int _callbackErrorCode = -32000;

  static AspireTransport? _defaultInstance;

  /// The socket path that the transport is connected to.
  final String socketPath;

  final Socket _socket;
  final List<int> _buffer = <int>[];
  final Map<int, Completer<Object?>> _pending = <int, Completer<Object?>>{};
  final Map<String, Function> _callbacks = <String, Function>{};
  final Completer<void> _closed = Completer<void>();

  late final StreamSubscription<Uint8List> _subscription;
  int _nextId = 1;
  int _callbackCounter = 0;
  bool _isClosed = false;

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  /// The transport that generated code uses when the caller names none.
  ///
  /// [connect] sets it. The getter throws [AspireError] with the code
  /// `NOT_CONNECTED` before the first connection.
  static AspireTransport get defaultInstance {
    final AspireTransport? transport = _defaultInstance;
    if (transport == null) {
      throw AspireError(
        code: AspireErrorCodes.notConnected,
        message:
            'There is no default Aspire transport. '
            'Call AspireTransport.connect() before you use the generated API.',
      );
    }
    return transport;
  }

  /// Replaces the default transport. Pass null to remove it.
  static set defaultInstance(AspireTransport? transport) =>
      _defaultInstance = transport;

  /// Returns true when a default transport exists.
  static bool get hasDefaultInstance => _defaultInstance != null;

  /// Connects to the AppHost.
  ///
  /// [socketPath] replaces `REMOTE_APP_HOST_SOCKET_PATH`. [authToken] replaces
  /// `ASPIRE_REMOTE_APPHOST_TOKEN`. The transport skips authentication when
  /// neither is set. The new transport becomes [defaultInstance] unless
  /// [setAsDefault] is false.
  ///
  /// Throws [AspireError] when the path is absent, when the socket does not
  /// open, or when the host rejects the token.
  static Future<AspireTransport> connect({
    String? socketPath,
    String? authToken,
    Duration timeout = defaultConnectTimeout,
    bool setAsDefault = true,
  }) async {
    final String path = _resolveSocketPath(socketPath);
    final Socket socket = await _openSocket(path, timeout);
    final AspireTransport transport = AspireTransport._(socket, path);

    try {
      await transport._authenticate(authToken);
    } on Object {
      await transport.close();
      rethrow;
    }

    if (setAsDefault) {
      _defaultInstance = transport;
    }
    return transport;
  }

  static String _resolveSocketPath(String? socketPath) {
    final String path =
        socketPath ?? Platform.environment[socketPathEnvironmentVariable] ?? '';
    if (path.isEmpty) {
      throw AspireError(
        code: AspireErrorCodes.missingSocketPath,
        message:
            'The $socketPathEnvironmentVariable environment variable is not set. '
            'The Aspire CLI sets it when it starts the guest. '
            'Pass socketPath: to AspireTransport.connect() to override it.',
      );
    }
    return path;
  }

  static Future<Socket> _openSocket(String path, Duration timeout) async {
    try {
      return await Socket.connect(
        InternetAddress(path, type: InternetAddressType.unix),
        0,
        timeout: timeout,
      );
    } on Object catch (error) {
      throw AspireError(
        code: AspireErrorCodes.connectionFailed,
        message: 'Cannot connect to the AppHost socket $path: $error',
        data: error,
      );
    }
  }

  Future<void> _authenticate(String? authToken) async {
    final String token =
        authToken ?? Platform.environment[authTokenEnvironmentVariable] ?? '';
    if (token.isEmpty) {
      return;
    }

    final Object? result = await request('authenticate', <Object?>[token]);
    if (result != true) {
      throw AspireError(
        code: AspireErrorCodes.authenticationFailed,
        message: 'The AppHost server rejected the guest token.',
      );
    }
  }

  /// Returns true while the socket is open.
  bool get isConnected => !_isClosed;

  /// Completes when the connection closes.
  Future<void> get onClose => _closed.future;

  /// Closes the connection and fails every pending request.
  Future<void> close() async {
    if (!_isClosed) {
      _socket.destroy();
    }
    _finish(
      AspireError(
        code: AspireErrorCodes.connectionClosed,
        message: 'The AppHost closed the connection.',
      ),
    );
    await _subscription.cancel();
    if (identical(_defaultInstance, this)) {
      _defaultInstance = null;
    }
  }

  // ── Requests ──────────────────────────────────────────────────────────────

  /// Sends `ping`. Returns `pong`.
  Future<String> ping() async => '${await request('ping', const <Object?>[])}';

  /// Invokes a capability.
  ///
  /// [args] maps argument names to values. A function in [args] becomes a
  /// registered callback identifier. A handle becomes its wire form.
  Future<Object?> invokeCapability(
    String capabilityId,
    Map<String, Object?> args,
  ) => request('invokeCapability', <Object?>[capabilityId, args]);

  /// Sends `cancelToken`. Returns true when the host cancelled the token.
  Future<bool> cancelToken(String tokenId) async =>
      await request('cancelToken', <Object?>[tokenId]) == true;

  /// Sends any JSON-RPC request and waits for the response.
  ///
  /// The call never times out. A capability such as `Aspire.Hosting/run`
  /// returns only when the application stops. The future fails with
  /// [AspireError].
  Future<Object?> request(String method, List<Object?> params) {
    if (_isClosed) {
      return Future<Object?>.error(
        AspireError(
          code: AspireErrorCodes.connectionClosed,
          message: 'The AppHost closed the connection.',
        ),
      );
    }

    final int id = _nextId++;
    final Completer<Object?> completer = Completer<Object?>();
    _pending[id] = completer;

    try {
      _send(<String, Object?>{
        'jsonrpc': '2.0',
        'id': id,
        'method': method,
        'params': _marshal(params),
      });
    } on Object catch (error) {
      _pending.remove(id);
      completer.completeError(
        AspireError(
          code: AspireErrorCodes.transportError,
          message: 'The AppHost connection failed: $error',
          data: error,
        ),
      );
    }

    return completer.future;
  }

  // ── Callbacks ─────────────────────────────────────────────────────────────

  /// Registers a function that the host can invoke.
  ///
  /// Returns the callback identifier. Pass the identifier, or the function
  /// itself, as a capability argument.
  String registerCallback(Function callback) {
    final String id =
        'callback_${++_callbackCounter}_${DateTime.now().millisecondsSinceEpoch}';
    _callbacks[id] = callback;
    return id;
  }

  /// Removes a callback. Returns true when the callback existed.
  bool unregisterCallback(String callbackId) =>
      _callbacks.remove(callbackId) != null;

  // ── Framing ───────────────────────────────────────────────────────────────

  void _send(Map<String, Object?> message) {
    final List<int> body = utf8.encode(jsonEncode(message));
    final BytesBuilder frame = BytesBuilder(copy: false)
      ..add(utf8.encode('Content-Length: ${body.length}\r\n\r\n'))
      ..add(body);
    _socket.add(frame.takeBytes());
  }

  void _onData(Uint8List chunk) {
    _buffer.addAll(chunk);
    while (_drainFrame()) {
      // Every complete frame in the buffer runs before the next read.
    }
  }

  /// Reads one frame out of the buffer. Returns false when the buffer holds no
  /// complete frame.
  bool _drainFrame() {
    final int separator = _indexOfSeparator();
    if (separator < 0) {
      return false;
    }

    final String headers = latin1.decode(_buffer.sublist(0, separator));
    final int? length = _contentLength(headers);
    if (length == null) {
      // The header block has no usable Content-Length. Drop it and continue
      // with the bytes that follow.
      _buffer.removeRange(0, separator + _headerSeparator.length);
      return true;
    }

    final int start = separator + _headerSeparator.length;
    if (_buffer.length - start < length) {
      return false;
    }

    final String body = utf8.decode(_buffer.sublist(start, start + length));
    _buffer.removeRange(0, start + length);
    _dispatch(body);
    return true;
  }

  int _indexOfSeparator() {
    for (
      int index = 0;
      index + _headerSeparator.length <= _buffer.length;
      index++
    ) {
      if (_buffer[index] == 13 &&
          _buffer[index + 1] == 10 &&
          _buffer[index + 2] == 13 &&
          _buffer[index + 3] == 10) {
        return index;
      }
    }
    return -1;
  }

  static int? _contentLength(String headers) {
    for (final String line in headers.split('\r\n')) {
      final int colon = line.indexOf(':');
      if (colon < 0) {
        continue;
      }
      if (line.substring(0, colon).trim().toLowerCase() != 'content-length') {
        continue;
      }
      final int? length = int.tryParse(line.substring(colon + 1).trim());
      if (length != null && length >= 0) {
        return length;
      }
    }
    return null;
  }

  // ── Dispatch ──────────────────────────────────────────────────────────────

  void _dispatch(String body) {
    final Object? decoded = jsonDecode(body);
    if (decoded is! Map<String, Object?>) {
      return;
    }

    final Object? method = decoded['method'];
    if (method is String) {
      _handleHostRequest(method, decoded);
      return;
    }

    final Object? id = decoded['id'];
    if (id is int) {
      _completePending(id, decoded);
    }
  }

  void _completePending(int id, Map<String, Object?> message) {
    final Completer<Object?>? completer = _pending.remove(id);
    if (completer == null || completer.isCompleted) {
      return;
    }

    final Object? error = message['error'];
    if (error is Map<String, Object?>) {
      completer.completeError(AspireError.fromJsonRpc(error));
      return;
    }

    final Object? result = message['result'];
    if (result is Map<String, Object?>) {
      final Object? atsError = result[r'$error'];
      if (atsError is Map<String, Object?>) {
        completer.completeError(AspireError.fromAts(atsError));
        return;
      }
    }

    completer.complete(AspireMarshal.decode(result));
  }

  void _handleHostRequest(String method, Map<String, Object?> message) {
    final Object? id = message['id'];
    if (method != 'invokeCallback') {
      _respondError(id, 'Unknown method: $method');
      return;
    }

    final Object? params = message['params'];
    final List<Object?> list = params is List ? params : const <Object?>[];
    final Object? callbackId = list.isEmpty ? null : list[0];
    final Object? args = list.length > 1 ? list[1] : null;

    final Function? callback = callbackId is String
        ? _callbacks[callbackId]
        : null;
    if (callback == null) {
      _respondError(id, 'Callback not found: $callbackId');
      return;
    }

    // The callback runs in a separate event-loop task. A callback is free to
    // await invokeCapability, which needs this read loop to stay free.
    unawaited(Future<void>(() => _runCallback(callback, args, id)));
  }

  Future<void> _runCallback(Function callback, Object? args, Object? id) async {
    Map<String, Object?>? response;
    try {
      final Object? result = await Function.apply(
        callback,
        _positionalArgs(args),
      );
      response = <String, Object?>{
        'jsonrpc': '2.0',
        'id': id,
        // Write-back protocol: a callback that returns null returns the
        // original arguments, so the host can detect DTO mutations.
        'result': result == null ? args : AspireMarshal.encode(result),
      };
    } on Object catch (error) {
      response = <String, Object?>{
        'jsonrpc': '2.0',
        'id': id,
        'error': <String, Object?>{
          'code': _callbackErrorCode,
          'message': '$error',
        },
      };
    }

    if (id != null && !_isClosed) {
      _send(response);
    }
  }

  /// The host serializes callback arguments with the positional keys `p0`,
  /// `p1`, ... A `$cancellationToken` entry repeats the identifier that one
  /// `pN` argument carries.
  static List<Object?> _positionalArgs(Object? args) {
    if (args == null) {
      return const <Object?>[];
    }
    if (args is! Map) {
      return <Object?>[AspireMarshal.decode(args)];
    }

    final Object? token = args[r'$cancellationToken'];
    final List<Object?> positional = <Object?>[];
    for (int index = 0; args.containsKey('p$index'); index++) {
      final Object? value = args['p$index'];
      positional.add(
        token is String && value == token
            ? CancellationToken(token)
            : AspireMarshal.decode(value),
      );
    }
    return positional;
  }

  void _respondError(Object? id, String message) {
    if (id == null || _isClosed) {
      return;
    }
    _send(<String, Object?>{
      'jsonrpc': '2.0',
      'id': id,
      'error': <String, Object?>{
        'code': _callbackErrorCode,
        'message': message,
      },
    });
  }

  // ── Marshalling ───────────────────────────────────────────────────────────

  /// Walks the arguments, registers every function as a callback, and converts
  /// the rest with [AspireMarshal.encode].
  Object? _marshal(Object? value) {
    if (value is Function) {
      return registerCallback(value);
    }
    if (value is Map) {
      return <String, Object?>{
        for (final MapEntry<Object?, Object?> entry in value.entries)
          '${entry.key}': _marshal(entry.value),
      };
    }
    if (value is Iterable) {
      return <Object?>[for (final Object? item in value) _marshal(item)];
    }
    return AspireMarshal.encode(value);
  }

  // ── Shutdown ──────────────────────────────────────────────────────────────

  void _onError(Object error) {
    _finish(
      AspireError(
        code: AspireErrorCodes.transportError,
        message: 'The AppHost connection failed: $error',
        data: error,
      ),
    );
  }

  void _onDone() {
    _finish(
      AspireError(
        code: AspireErrorCodes.connectionClosed,
        message: 'The AppHost closed the connection.',
      ),
    );
  }

  void _finish(AspireError error) {
    if (_isClosed) {
      return;
    }
    _isClosed = true;

    final List<Completer<Object?>> pending = _pending.values.toList(
      growable: false,
    );
    _pending.clear();
    for (final Completer<Object?> completer in pending) {
      if (!completer.isCompleted) {
        completer.completeError(error);
      }
    }

    if (!_closed.isCompleted) {
      _closed.complete();
    }
  }
}
