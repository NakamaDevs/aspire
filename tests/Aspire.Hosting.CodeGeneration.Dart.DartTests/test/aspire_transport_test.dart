import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:test/test.dart';

import '../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/base.dart';
import '../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/transport.dart';
import 'support/fake_host.dart';

void main() {
  // ── Framing ────────────────────────────────────────────────────────────────

  test('frames a request with Content-Length byte count', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    final Future<String> pending = transport.ping();
    final Map<String, Object?> request = await host.awaitRequest();
    expect(request['method'], 'ping');

    final _Frame frame = parseFrame(host.receivedBytes);
    expect(frame.length, frame.body.length);

    final Map<String, Object?> decoded =
        jsonDecode(utf8.decode(frame.body)) as Map<String, Object?>;
    expect(decoded['jsonrpc'], '2.0');
    expect(decoded['method'], 'ping');
    expect(decoded['params'], isEmpty);
    expect(decoded['id'], isA<int>());

    await host.sendMessage(<String, Object?>{
      'jsonrpc': '2.0',
      'id': request['id'],
      'result': 'pong',
    });
    expect(await pending, 'pong');
  });

  test('parses two frames in one packet', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    final Future<Object?> first = transport.invokeCapability(
      'one',
      <String, Object?>{},
    );
    final Map<String, Object?> firstRequest = await host.awaitRequest();

    final Future<Object?> second = transport.invokeCapability(
      'two',
      <String, Object?>{},
    );
    final Map<String, Object?> secondRequest = await host.awaitRequest();

    await host.sendRaw(<int>[
      ...FakeHost.frame(<String, Object?>{
        'jsonrpc': '2.0',
        'id': firstRequest['id'],
        'result': 'first',
      }),
      ...FakeHost.frame(<String, Object?>{
        'jsonrpc': '2.0',
        'id': secondRequest['id'],
        'result': 'second',
      }),
    ]);

    expect(await first, 'first');
    expect(await second, 'second');
  });

  test('parses a frame split across packets', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    final Future<Object?> pending = transport.invokeCapability(
      'one',
      <String, Object?>{},
    );
    final Map<String, Object?> request = await host.awaitRequest();

    bool completed = false;
    unawaited(pending.then((Object? _) => completed = true));

    final List<int> frame = FakeHost.frame(<String, Object?>{
      'jsonrpc': '2.0',
      'id': request['id'],
      'result': 'split',
    });

    await host.sendRaw(frame.sublist(0, 12));
    await settle();
    expect(completed, isFalse);

    await host.sendRaw(frame.sublist(12, 32));
    await settle();
    expect(completed, isFalse);

    await host.sendRaw(frame.sublist(32));
    expect(await pending, 'split');
  });

  test('multibyte UTF-8 body length is bytes not graphemes', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    const String text = 'héllo → 世界';
    final Future<Object?> pending = transport.invokeCapability(
      'echo',
      <String, Object?>{'text': text},
    );
    final Map<String, Object?> request = await host.awaitRequest();

    final _Frame frame = parseFrame(host.receivedBytes);
    expect(frame.length, frame.body.length);

    final String body = utf8.decode(frame.body);
    expect(frame.length, greaterThan(body.length));

    final Map<String, Object?> decoded =
        jsonDecode(body) as Map<String, Object?>;
    expect(decoded['params'], <Object?>[
      'echo',
      <String, Object?>{'text': text},
    ]);

    await host.sendMessage(<String, Object?>{
      'jsonrpc': '2.0',
      'id': request['id'],
      'result': text,
    });
    expect(await pending, text);
  });

  // ── Requests ───────────────────────────────────────────────────────────────

  test('ping returns pong', () async {
    final FakeHost host = await startHost(
      handler: methodHandler(<String, Object?>{'ping': 'pong'}),
    );
    final AspireTransport transport = await startTransport(host);

    expect(await transport.ping(), 'pong');

    final Map<String, Object?> request = await host.awaitRequest();
    expect(request['method'], 'ping');
    expect(request['params'], isEmpty);
  });

  test('invokeCapability returns a handle', () async {
    const String redisType =
        'Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource';
    const String builderType =
        'Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder';

    final FakeHost host = await startHost(
      handler: methodHandler(<String, Object?>{
        'invokeCapability': <String, Object?>{
          r'$handle': '2',
          r'$type': redisType,
        },
      }),
    );
    final AspireTransport transport = await startTransport(host);

    final Object? result = await transport.invokeCapability(
      'Aspire.Hosting.Redis/addRedis',
      <String, Object?>{
        'builder': const AspireHandle('1', builderType),
        'name': 'cache',
      },
    );

    expect(result, isA<AspireHandle>());
    final AspireHandle handle = result! as AspireHandle;
    expect(handle.id, '2');
    expect(handle.type, redisType);

    final Map<String, Object?> request = await host.awaitRequest();
    expect(request['method'], 'invokeCapability');
    expect(request['params'], <Object?>[
      'Aspire.Hosting.Redis/addRedis',
      <String, Object?>{
        'builder': <String, Object?>{r'$handle': '1', r'$type': builderType},
        'name': 'cache',
      },
    ]);
  });

  test(
    'invokeCapability error maps to AspireError with code and data',
    () async {
      final Map<String, Object?> atsError = <String, Object?>{
        'code': 'CAPABILITY_NOT_FOUND',
        'message': 'Unknown capability: Contoso.Aspire/bar',
        'capability': 'Contoso.Aspire/bar',
        'details': <String, Object?>{'parameter': 'name'},
      };

      final FakeHost host = await startHost(
        handler: methodHandler(<String, Object?>{
          'invokeCapability': <String, Object?>{r'$error': atsError},
        }),
      );
      final AspireTransport transport = await startTransport(host);

      await expectLater(
        transport.invokeCapability('Contoso.Aspire/bar', <String, Object?>{}),
        throwsA(
          isA<AspireError>()
              .having(
                (AspireError error) => error.code,
                'code',
                'CAPABILITY_NOT_FOUND',
              )
              .having(
                (AspireError error) => error.message,
                'message',
                'Unknown capability: Contoso.Aspire/bar',
              )
              .having(
                (AspireError error) => error.capability,
                'capability',
                'Contoso.Aspire/bar',
              )
              .having((AspireError error) => error.data, 'data', atsError),
        ),
      );
    },
  );

  test('concurrent requests correlate by id', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    final List<Future<Object?>> pending = <Future<Object?>>[
      for (int index = 1; index <= 5; index++)
        transport.invokeCapability('cap$index', <String, Object?>{}),
    ];

    final List<Map<String, Object?>> requests = <Map<String, Object?>>[
      for (int index = 0; index < 5; index++) await host.awaitRequest(),
    ];

    final List<int> ids = requests
        .map((Map<String, Object?> request) => request['id']! as int)
        .toList();
    expect(ids, orderedEquals(<int>[...ids]..sort()));

    // Answer in reverse order to prove correlation is by id, not by arrival.
    for (final Map<String, Object?> request in requests.reversed) {
      final List<Object?> params = request['params']! as List<Object?>;
      final String index = (params[0]! as String).substring('cap'.length);
      await host.sendMessage(<String, Object?>{
        'jsonrpc': '2.0',
        'id': request['id'],
        'result': 'result$index',
      });
    }

    expect(await Future.wait(pending), <Object?>[
      'result1',
      'result2',
      'result3',
      'result4',
      'result5',
    ]);
  });

  test('cancelToken sends cancelToken', () async {
    final FakeHost host = await startHost(
      handler: methodHandler(<String, Object?>{'cancelToken': true}),
    );
    final AspireTransport transport = await startTransport(host);

    expect(await transport.cancelToken('ct_abc'), isTrue);

    final Map<String, Object?> request = await host.awaitRequest();
    expect(request['method'], 'cancelToken');
    expect(request['params'], <Object?>['ct_abc']);
  });

  test('CancellationToken.cancel sends cancelToken', () async {
    final FakeHost host = await startHost(
      handler: methodHandler(<String, Object?>{'cancelToken': true}),
    );
    final AspireTransport transport = await startTransport(host);

    final CancellationToken token = CancellationToken.create();
    expect(await token.cancel(transport), isTrue);

    final Map<String, Object?> request = await host.awaitRequest();
    expect(request['method'], 'cancelToken');
    expect(request['params'], <Object?>[token.id]);
  });

  // ── Callbacks ──────────────────────────────────────────────────────────────

  test(
    'host invokeCallback runs registered callback and returns result',
    () async {
      final FakeHost host = await startHost();
      final AspireTransport transport = await startTransport(host);

      final String callbackId = transport.registerCallback(
        (String value) => 'got:$value',
      );
      expect(callbackId, startsWith('callback_1_'));

      await invokeCallback(host, 100, callbackId, <String, Object?>{
        'p0': 'hello',
      });

      final Map<String, Object?> response = await host.awaitRequest();
      expect(response['id'], 100);
      expect(response['result'], 'got:hello');
    },
  );

  test('callback error is returned as JSON-RPC error', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    final String callbackId = transport.registerCallback(
      (String _) => throw StateError('boom'),
    );
    await invokeCallback(host, 101, callbackId, <String, Object?>{
      'p0': 'hello',
    });

    final Map<String, Object?> response = await host.awaitRequest();
    expect(response['id'], 101);
    expect(response.containsKey('result'), isFalse);
    final Map<String, Object?> error =
        response['error']! as Map<String, Object?>;
    expect(error['message'], contains('boom'));
    expect(error['code'], -32000);
  });

  test('callback that invokes a capability does not deadlock', () async {
    final FakeHost host = await startHost(
      handler: methodHandler(<String, Object?>{
        'invokeCapability': <String, Object?>{r'$handle': '7', r'$type': 'T'},
      }),
    );
    final AspireTransport transport = await startTransport(host);

    final String callbackId = transport.registerCallback((Object? _) async {
      final Object? built = await transport.invokeCapability(
        'Aspire.Hosting/build',
        <String, Object?>{},
      );
      return (built! as AspireHandle).id;
    });

    await invokeCallback(host, 102, callbackId, <String, Object?>{
      'p0': <String, Object?>{r'$handle': '5', r'$type': 'Context'},
    });

    final Map<String, Object?> nested = await host.awaitRequest();
    expect(nested['method'], 'invokeCapability');

    final Map<String, Object?> response = await host.awaitRequest();
    expect(response['id'], 102);
    expect(response['result'], '7');
  });

  test(
    r'callback receives a cancellation token for $cancellationToken',
    () async {
      final FakeHost host = await startHost();
      final AspireTransport transport = await startTransport(host);

      final List<Object?> seen = <Object?>[];
      final String callbackId = transport.registerCallback((
        Object? context,
        Object? token,
      ) {
        seen
          ..add(context)
          ..add(token);
        return 'done';
      });

      await invokeCallback(host, 103, callbackId, <String, Object?>{
        'p0': <String, Object?>{r'$handle': '5', r'$type': 'Context'},
        'p1': 'ct_1',
        r'$cancellationToken': 'ct_1',
      });

      final Map<String, Object?> response = await host.awaitRequest();
      expect(response['id'], 103);
      expect(response['result'], 'done');

      expect(seen[0], const AspireHandle('5', 'Context'));
      expect(seen[1], const CancellationToken('ct_1'));
    },
  );

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  test('socket close fails pending requests and completes onClose', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    final Future<Object?> pending = transport.invokeCapability(
      'slow',
      <String, Object?>{},
    );
    // Attach the expectation before the close, so the failure never reaches
    // the zone as an unhandled asynchronous error.
    final Future<void> failure = expectLater(
      pending,
      throwsA(
        isA<AspireError>().having(
          (AspireError error) => error.code,
          'code',
          'CONNECTION_CLOSED',
        ),
      ),
    );
    await host.awaitRequest();

    await host.close();

    await failure;
    await transport.onClose;
    expect(transport.isConnected, isFalse);
  });

  test('missing REMOTE_APP_HOST_SOCKET_PATH returns a clear error', () async {
    expect(
      Platform.environment.containsKey(
        AspireTransport.socketPathEnvironmentVariable,
      ),
      isFalse,
      reason:
          'unset ${AspireTransport.socketPathEnvironmentVariable} before you run the test suite',
    );

    await expectLater(
      AspireTransport.connect(),
      throwsA(
        isA<AspireError>()
            .having(
              (AspireError error) => error.code,
              'code',
              'MISSING_SOCKET_PATH',
            )
            .having(
              (AspireError error) => error.message,
              'message',
              contains(AspireTransport.socketPathEnvironmentVariable),
            ),
      ),
    );
  });
}

// ── Helpers ──────────────────────────────────────────────────────────────────

/// Starts a fake host and closes it when the test ends.
Future<FakeHost> startHost({FakeHandler? handler}) async {
  final FakeHost host = await FakeHost.start(handler: handler);
  addTearDown(host.close);
  return host;
}

/// Connects a transport to [host] and closes it when the test ends.
Future<AspireTransport> startTransport(FakeHost host) async {
  final AspireTransport transport = await AspireTransport.connect(
    socketPath: host.socketPath,
  );
  addTearDown(transport.close);
  return transport;
}

/// Sends an `invokeCallback` request from the host to the guest.
Future<void> invokeCallback(
  FakeHost host,
  int id,
  String callbackId,
  Map<String, Object?> args,
) => host.sendMessage(<String, Object?>{
  'jsonrpc': '2.0',
  'id': id,
  'method': 'invokeCallback',
  'params': <Object?>[callbackId, args],
});

/// Waits long enough for a pending socket read to run.
Future<void> settle() => Future<void>.delayed(const Duration(milliseconds: 50));

/// One LSP frame: the declared length and the body bytes.
class _Frame {
  const _Frame(this.length, this.body);

  final int length;
  final List<int> body;
}

/// Reads the first frame out of raw bytes.
_Frame parseFrame(List<int> raw) {
  final String text = latin1.decode(raw);
  final int separator = text.indexOf('\r\n\r\n');
  expect(separator, greaterThan(0), reason: 'the bytes hold no frame header');

  final String headers = text.substring(0, separator);
  expect(headers, startsWith('Content-Length: '));

  final int length = int.parse(
    headers.substring('Content-Length: '.length).trim(),
  );
  final int start = separator + 4;
  return _Frame(length, raw.sublist(start, start + length));
}
