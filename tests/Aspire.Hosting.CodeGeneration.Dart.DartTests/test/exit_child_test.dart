// The transport listens on the socket, and that subscription keeps the Dart
// event loop alive. A guest script therefore exits only after it closes the
// transport. Dart cannot set an environment variable in the running process, so
// the case starts `test/support/exit_child.dart` with the socket path.

import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:test/test.dart';

import 'support/fake_host.dart';

const String socketPathVariable = 'REMOTE_APP_HOST_SOCKET_PATH';

/// The time the child gets to exit after it printed its last marker.
const Duration exitTimeout = Duration(seconds: 5);

void main() {
  test('the process exits after close', () async {
    final FakeHost host = await FakeHost.start(
      handler: methodHandler(<String, Object?>{'ping': 'pong'}),
    );
    addTearDown(host.close);

    final Process child = await Process.start(
      Platform.resolvedExecutable,
      <String>['run', '${Directory.current.path}/test/support/exit_child.dart'],
      environment: <String, String>{socketPathVariable: host.socketPath},
      workingDirectory: Directory.current.path,
    );

    final List<String> lines = <String>[];
    final Completer<void> lastMarker = Completer<void>();
    final StreamSubscription<String> collector = child.stdout
        .transform(utf8.decoder)
        .transform(const LineSplitter())
        .listen(
          (String line) {
            lines.add(line);
            if (line == 'done' && !lastMarker.isCompleted) {
              lastMarker.complete();
            }
          },
          onDone: () {
            if (!lastMarker.isCompleted) {
              lastMarker.complete();
            }
          },
        );
    addTearDown(collector.cancel);

    final Future<String> errors = child.stderr.transform(utf8.decoder).join();

    // `dart run` compiles the script first, so the work itself gets a long
    // timeout. Only the exit after the last marker has to be fast.
    await lastMarker.future.timeout(const Duration(minutes: 2));

    final int code = await child.exitCode.timeout(
      exitTimeout,
      onTimeout: () {
        child.kill(ProcessSignal.sigkill);
        return -1;
      },
    );

    final String errorText = await errors;

    expect(
      code,
      0,
      reason:
          'The child did not exit within ${exitTimeout.inSeconds} seconds '
          'after it closed the transport.\n${lines.join('\n')}\n$errorText',
    );
    expect(lines, contains('ping:pong'));
    expect(lines, contains('closed:false'));
    expect(lines, contains('done'));
  }, timeout: const Timeout(Duration(minutes: 2)));
}
