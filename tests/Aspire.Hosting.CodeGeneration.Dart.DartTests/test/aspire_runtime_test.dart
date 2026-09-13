// Tests for `aspire_runtime.dart`, the helper file the code generator copies
// into a guest project.
//
// The tests import the resource with a relative path, so they run the same bytes
// that a user gets in `.aspire/modules/`.

import 'dart:async';

import 'package:test/test.dart';

import '../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/aspire_runtime.dart';
import '../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/base.dart';
import '../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/transport.dart';
import 'support/fake_host.dart';

/// Stands in for a generated context class. The generator emits the same shape:
/// a class that extends [AspireObject] and takes the handle and the transport.
class _FakeContext extends AspireObject {
  const _FakeContext(super.handle, super.transport);
}

void main() {
  // ── Reference expressions ──────────────────────────────────────────────────

  test('ref builds a reference expression capability call', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    const AspireHandle endpoint = AspireHandle('e1', 'Aspire.Hosting/Endpoint');

    final ReferenceExpression expression = ref(<Object?>[
      'http://',
      endpoint,
      '/health',
    ]);

    expect(expression.format, 'http://{0}/health');
    expect(expression.valueProviders, hasLength(1));

    // The expression travels as {"$expr": {"format": ..., "valueProviders": ...}}.
    // The fake host never answers, so the call fails when the test closes the
    // transport. The test reads the request bytes, not the reply.
    unawaited(
      transport
          .invokeCapability('Aspire.Hosting/withUrl', <String, Object?>{
            'url': expression,
          })
          .catchError((Object _) => null),
    );

    final Map<String, Object?> request = await host.awaitRequest();
    final List<Object?> params = request['params']! as List<Object?>;
    final Map<String, Object?> args = params[1]! as Map<String, Object?>;
    expect(args['url'], <String, Object?>{
      r'$expr': <String, Object?>{
        'format': 'http://{0}/health',
        'valueProviders': <Object?>[
          <String, Object?>{
            r'$handle': 'e1',
            r'$type': 'Aspire.Hosting/Endpoint',
          },
        ],
      },
    });
  });

  test('a host reference expression resolves through getValueAsync', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    const AspireHandle handle = AspireHandle(
      'r1',
      'Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression',
    );
    final ReferenceExpression expression = ReferenceExpression(
      handle,
      transport,
    );

    final Future<String?> pending = expression.getValueAsync();
    final Map<String, Object?> request = await host.awaitRequest();
    final List<Object?> params = request['params']! as List<Object?>;

    expect(params[0], ReferenceExpression.getValueCapability);
    expect(params[1], <String, Object?>{
      'context': <String, Object?>{r'$handle': 'r1', r'$type': handle.type},
    });

    await host.sendMessage(<String, Object?>{
      'jsonrpc': '2.0',
      'id': request['id'],
      'result': 'http://localhost:5000/health',
    });
    expect(await pending, 'http://localhost:5000/health');
  });

  test('a guest reference expression cannot resolve on the host', () async {
    final ReferenceExpression expression = ref(<Object?>['plain']);

    await expectLater(
      expression.getValueAsync(),
      throwsA(
        isA<AspireError>().having(
          (AspireError error) => error.code,
          'code',
          AspireErrorCodes.invalidArgument,
        ),
      ),
    );
  });

  // ── Callbacks ──────────────────────────────────────────────────────────────

  test('typed callback decodes handle argument', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    _FakeContext? received;

    // The generator emits this wrapper: it decodes the raw argument into the
    // context class before the function of the caller runs.
    final String callbackId = transport.registerCallback((Object? a0) async {
      final _FakeContext p0 = _FakeContext(
        AspireRuntime.requireHandle(a0, 'Aspire.Tests/withCallback'),
        transport,
      );
      received = p0;
      return null;
    });

    await invokeCallback(host, 200, callbackId, <String, Object?>{
      'p0': <String, Object?>{
        r'$handle': 'c1',
        r'$type': 'Aspire.Tests/Context',
      },
    });

    final Map<String, Object?> response = await host.awaitRequest();
    expect(response['id'], 200);

    expect(received, isNotNull);
    expect(received!.handle.id, 'c1');
    expect(received!.handle.type, 'Aspire.Tests/Context');

    // The wrapper returned null, so the transport echoes the arguments the host
    // sent. That is the write-back protocol a data object callback relies on.
    expect(response['result'], <String, Object?>{
      'p0': <String, Object?>{
        r'$handle': 'c1',
        r'$type': 'Aspire.Tests/Context',
      },
    });
  });

  test('asCallback keeps a guest function and drops a host identifier', () {
    void handler(Object? _) {}

    expect(AspireRuntime.asCallback<Function>(handler), same(handler));
    expect(AspireRuntime.asCallback<Function>('callback_1_2'), isNull);
    expect(AspireRuntime.asCallback<Function>(null), isNull);
  });

  // ── Union guard ────────────────────────────────────────────────────────────

  test('requireUnion names every accepted type', () {
    expect(
      AspireRuntime.requireUnion(
        'redis',
        (Object? value) => value is String,
        const <String>['String', 'Resource'],
        'Aspire.Tests/withUnion',
        'dependency',
      ),
      'redis',
    );

    expect(
      () => AspireRuntime.requireUnion(
        7,
        (Object? value) => value is String,
        const <String>['String', 'Resource'],
        'Aspire.Tests/withUnion',
        'dependency',
      ),
      throwsA(
        isA<ArgumentError>().having(
          (ArgumentError error) => error.message,
          'message',
          allOf(
            contains('String, Resource'),
            contains('Aspire.Tests/withUnion'),
          ),
        ),
      ),
    );
  });

  // ── Collections ────────────────────────────────────────────────────────────

  test('AspireList decodes every element with its decoder', () async {
    final FakeHost host = await startHost();
    final AspireTransport transport = await startTransport(host);

    const AspireHandle handle = AspireHandle('l1', 'Aspire.Hosting/List');
    final AspireList<String?> list = AspireList<String?>(
      handle,
      transport,
      (Object? item) => AspireRuntime.asString(item),
    );

    final Future<List<String?>> pending = list.toList();
    final Map<String, Object?> request = await host.awaitRequest();
    final List<Object?> params = request['params']! as List<Object?>;

    expect(params[0], 'Aspire.Hosting/List.toArray');
    expect(params[1], <String, Object?>{
      'list': <String, Object?>{
        r'$handle': 'l1',
        r'$type': 'Aspire.Hosting/List',
      },
    });

    await host.sendMessage(<String, Object?>{
      'jsonrpc': '2.0',
      'id': request['id'],
      'result': <Object?>['one', 'two'],
    });
    expect(await pending, <String?>['one', 'two']);
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
