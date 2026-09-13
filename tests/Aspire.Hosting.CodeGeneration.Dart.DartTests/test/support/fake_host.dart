// A fake AppHost server for the transport tests.
//
// The fake host listens on a temporary Unix domain socket, accepts one
// connection, and parses LSP framed JSON-RPC messages. Every parsed message
// goes into a queue that [FakeHost.awaitRequest] reads. An optional handler
// scripts the response.

import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:math';
import 'dart:typed_data';

/// Builds the response for one guest message. Return null to send nothing.
typedef FakeHandler =
    Map<String, Object?>? Function(Map<String, Object?> request);

/// Builds a handler that answers each method in [results] with a result.
FakeHandler methodHandler(Map<String, Object?> results) =>
    (Map<String, Object?> request) {
      final Object? method = request['method'];
      if (method is String && results.containsKey(method)) {
        return <String, Object?>{
          'jsonrpc': '2.0',
          'id': request['id'],
          'result': results[method],
        };
      }
      return null;
    };

int _socketCounter = 0;

// `dart test` runs each suite in its own isolate inside one process, so the
// process identifier does not separate two suites. The random prefix does.
final String _socketPrefix = Random().nextInt(0x7fffffff).toRadixString(36);

class FakeHost {
  FakeHost._(this._server, this.socketPath, this._handler);

  /// Starts a fake host on a new temporary socket.
  static Future<FakeHost> start({FakeHandler? handler}) async {
    final String path = _tempSocketPath();
    final File file = File(path);
    if (file.existsSync()) {
      file.deleteSync();
    }

    final ServerSocket server = await ServerSocket.bind(
      InternetAddress(path, type: InternetAddressType.unix),
      0,
    );
    final FakeHost host = FakeHost._(
      server,
      path,
      handler ?? (Map<String, Object?> request) => null,
    );
    host._subscription = server.listen(host._accept);
    return host;
  }

  /// The Unix socket path that the fake host listens on.
  final String socketPath;

  final ServerSocket _server;
  final FakeHandler _handler;
  final Completer<Socket> _accepted = Completer<Socket>();
  final List<int> _buffer = <int>[];
  final List<int> _received = <int>[];
  final List<Map<String, Object?>> _requests = <Map<String, Object?>>[];
  final List<Completer<Map<String, Object?>>> _waiters =
      <Completer<Map<String, Object?>>>[];

  StreamSubscription<Socket>? _subscription;
  StreamSubscription<Uint8List>? _clientSubscription;
  Socket? _client;

  /// Every byte that the guest sent, in order.
  List<int> get receivedBytes => List<int>.unmodifiable(_received);

  /// Frames a message with the LSP `Content-Length` header.
  static List<int> frame(Object message) {
    final List<int> body = utf8.encode(
      message is String ? message : jsonEncode(message),
    );
    return <int>[
      ...utf8.encode('Content-Length: ${body.length}\r\n\r\n'),
      ...body,
    ];
  }

  /// Encodes and sends one JSON-RPC message to the guest.
  Future<void> sendMessage(Map<String, Object?> message) =>
      sendRaw(frame(message));

  /// Sends raw bytes to the guest. Use it to script partial or joined frames.
  ///
  /// The guest can finish `connect` before the listener delivers the accepted
  /// socket. The send waits for that socket instead of failing.
  Future<void> sendRaw(List<int> bytes) async {
    // `close` destroys the accepted socket. This local holds the same socket.
    // ignore: close_sinks
    final Socket socket = await _ensureSocket();
    socket.add(bytes);
    await socket.flush();
  }

  /// Waits for the next message that the guest sent and returns it.
  Future<Map<String, Object?>> awaitRequest({
    Duration timeout = const Duration(seconds: 2),
  }) async {
    if (_requests.isNotEmpty) {
      return _requests.removeAt(0);
    }
    final Completer<Map<String, Object?>> waiter =
        Completer<Map<String, Object?>>();
    _waiters.add(waiter);
    return waiter.future.timeout(
      timeout,
      onTimeout: () {
        _waiters.remove(waiter);
        throw StateError(
          'no message from the guest after ${timeout.inMilliseconds}ms',
        );
      },
    );
  }

  /// Closes the accepted connection and the listening socket.
  Future<void> close() async {
    _client?.destroy();
    _client = null;
    await _clientSubscription?.cancel();
    _clientSubscription = null;
    await _subscription?.cancel();
    _subscription = null;
    await _server.close();
    final File file = File(socketPath);
    if (file.existsSync()) {
      file.deleteSync();
    }
  }

  Future<Socket> _ensureSocket() {
    final Socket? client = _client;
    if (client != null) {
      return Future<Socket>.value(client);
    }
    return _accepted.future.timeout(
      const Duration(seconds: 2),
      onTimeout: () =>
          throw StateError('FakeHost: no client connected within 2s'),
    );
  }

  void _accept(Socket socket) {
    _client = socket;
    _clientSubscription = socket.listen(
      _onData,
      cancelOnError: false,
      onError: (Object _) {},
    );
    if (!_accepted.isCompleted) {
      _accepted.complete(socket);
    }
  }

  void _onData(Uint8List chunk) {
    _received.addAll(chunk);
    _buffer.addAll(chunk);
    while (_drainFrame()) {
      // Every complete frame in the buffer runs before the next read.
    }
  }

  bool _drainFrame() {
    final int separator = _indexOfSeparator();
    if (separator < 0) {
      return false;
    }

    final String headers = latin1.decode(_buffer.sublist(0, separator));
    final int? length = _contentLength(headers);
    if (length == null) {
      _buffer.removeRange(0, separator + 4);
      return true;
    }

    final int start = separator + 4;
    if (_buffer.length - start < length) {
      return false;
    }

    final String body = utf8.decode(_buffer.sublist(start, start + length));
    _buffer.removeRange(0, start + length);
    _handleFrame(body);
    return true;
  }

  int _indexOfSeparator() {
    for (int index = 0; index + 4 <= _buffer.length; index++) {
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
      if (colon >= 0 &&
          line.substring(0, colon).trim().toLowerCase() == 'content-length') {
        return int.tryParse(line.substring(colon + 1).trim());
      }
    }
    return null;
  }

  void _handleFrame(String body) {
    final Object? decoded = jsonDecode(body);
    if (decoded is! Map<String, Object?>) {
      return;
    }

    if (_waiters.isNotEmpty) {
      _waiters.removeAt(0).complete(decoded);
    } else {
      _requests.add(decoded);
    }

    final Map<String, Object?>? response = _handler(decoded);
    if (response != null) {
      unawaited(sendRaw(frame(response)));
    }
  }

  static String _tempSocketPath() {
    // macOS limits a Unix socket path to 104 bytes. Keep it short.
    return '/tmp/adt${_socketPrefix}_${++_socketCounter}.sock';
  }
}
