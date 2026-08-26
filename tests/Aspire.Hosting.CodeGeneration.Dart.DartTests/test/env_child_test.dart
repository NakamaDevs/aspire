// The transport reads two environment variables. Dart cannot set an
// environment variable in the running process, so each case starts
// `test/support/env_child.dart` with the variable and reads its markers.

import 'dart:io';

import 'package:test/test.dart';

import 'support/fake_host.dart';

const String socketPathVariable = 'REMOTE_APP_HOST_SOCKET_PATH';
const String authTokenVariable = 'ASPIRE_REMOTE_APPHOST_TOKEN';

void main() {
  test(
    'connect reads the socket path from the environment',
    () async {
      final FakeHost host = await FakeHost.start(
        handler: methodHandler(<String, Object?>{'ping': 'pong'}),
      );
      addTearDown(host.close);

      final ProcessResult result = await runChild(<String, String>{
        socketPathVariable: host.socketPath,
      });

      expect(result.exitCode, 0, reason: '${result.stdout}${result.stderr}');
      expect(result.stdout, contains('socketPath:${host.socketPath}'));
      expect(result.stdout, contains('default:true'));
      expect(result.stdout, contains('ping:pong'));

      final Map<String, Object?> request = await host.awaitRequest();
      expect(request['method'], 'ping');
      expect(request['params'], isEmpty);
    },
    timeout: const Timeout(Duration(minutes: 2)),
  );

  test(
    'connect authenticates when ASPIRE_REMOTE_APPHOST_TOKEN is set',
    () async {
      final FakeHost host = await FakeHost.start(
        handler: methodHandler(<String, Object?>{
          'authenticate': true,
          'ping': 'pong',
        }),
      );
      addTearDown(host.close);

      final ProcessResult result = await runChild(<String, String>{
        socketPathVariable: host.socketPath,
        authTokenVariable: 'secret-token',
      });

      expect(result.exitCode, 0, reason: '${result.stdout}${result.stderr}');
      expect(result.stdout, contains('ping:pong'));

      final Map<String, Object?> first = await host.awaitRequest();
      expect(first['method'], 'authenticate');
      expect(first['params'], <Object?>['secret-token']);

      final Map<String, Object?> second = await host.awaitRequest();
      expect(second['method'], 'ping');
    },
    timeout: const Timeout(Duration(minutes: 2)),
  );
}

/// Runs `test/support/env_child.dart` with the extra environment variables.
Future<ProcessResult> runChild(Map<String, String> environment) => Process.run(
  Platform.resolvedExecutable,
  <String>['run', '${Directory.current.path}/test/support/env_child.dart'],
  environment: environment,
  workingDirectory: Directory.current.path,
);
