// Tests for `watch.dart`, the AppHost file watcher.
//
// Each test starts a real `dart` process, so the timeouts stay short and every
// process is stopped in `addTearDown`.

import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:test/test.dart';

/// The watcher the code generator copies into `.aspire/modules/`. `dart test`
/// runs from the package directory, so the path is relative to it.
final String watchScript = File(
  '../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/watch.dart',
).absolute.path;

const Duration timeout = Duration(seconds: 10);

void main() {
  test('watch restarts child when apphost.dart mtime changes', () async {
    final Directory directory = temporaryDirectory();
    write(
      directory,
      'pubspec.yaml',
      'name: watch_app\npublish_to: none\n\nenvironment:\n  sdk: ^3.8.0\n',
    );
    write(directory, 'apphost.dart', '''
import 'dart:io';

Future<void> main(List<String> args) async {
  stdout.writeln('APPHOST-STARTED');
  await stdout.flush();
  await Future<void>.delayed(const Duration(seconds: 60));
}
''');

    final _Watcher watcher = await startWatch(directory, <String>[
      'apphost.dart',
    ]);

    await watcher.awaitOutput('APPHOST-STARTED', 1);

    // The watcher compares modification times, so the stamp has to be clearly
    // newer than the snapshot it already took.
    File(
      '${directory.path}/apphost.dart',
    ).setLastModifiedSync(DateTime.now().add(const Duration(seconds: 2)));

    final String output = await watcher.awaitOutput('APPHOST-STARTED', 2);
    expect(output, contains('[aspire-watch] restarting: apphost.dart'));
  });

  test('watch forwards child exit code with --once', () async {
    final Directory directory = temporaryDirectory();
    write(
      directory,
      'pubspec.yaml',
      'name: watch_app\npublish_to: none\n\nenvironment:\n  sdk: ^3.8.0\n',
    );
    write(directory, 'apphost.dart', '''
import 'dart:io';

void main() {
  stdout.writeln('APPHOST-FAILED');
  exit(3);
}
''');

    final _Watcher watcher = await startWatch(directory, <String>[
      'apphost.dart',
      '--once',
    ]);

    final int status = await watcher.process.exitCode.timeout(timeout);
    expect(status, 3);
    expect(watcher.output, contains('APPHOST-FAILED'));
  });
}

// ── Helpers ──────────────────────────────────────────────────────────────────

/// A running watcher and everything it wrote so far.
class _Watcher {
  _Watcher(this.process);

  final Process process;
  final StringBuffer _output = StringBuffer();
  final List<void Function()> _listeners = <void Function()>[];

  String get output => _output.toString();

  void collect(Stream<List<int>> stream) {
    stream.transform(utf8.decoder).listen((String chunk) {
      _output.write(chunk);
      for (final void Function() listener in _listeners.toList()) {
        listener();
      }
    });
  }

  /// Waits until [marker] appeared [wanted] times, then returns everything the
  /// watcher wrote.
  Future<String> awaitOutput(String marker, int wanted) {
    final Completer<String> completer = Completer<String>();

    void check() {
      if (!completer.isCompleted && count(output, marker) >= wanted) {
        completer.complete(output);
      }
    }

    _listeners.add(check);
    check();

    return completer.future.timeout(
      timeout,
      onTimeout: () => throw StateError(
        'no "$marker" (x$wanted) from the watcher in '
        '${timeout.inMilliseconds}ms: $output',
      ),
    );
  }
}

Future<_Watcher> startWatch(Directory directory, List<String> arguments) async {
  final Process process = await Process.start(
    Platform.resolvedExecutable,
    <String>['run', watchScript, ...arguments],
    workingDirectory: directory.path,
  );

  final _Watcher watcher = _Watcher(process);
  watcher.collect(process.stdout);
  watcher.collect(process.stderr);

  // The watcher stops its own child on SIGTERM, so the AppHost never outlives
  // the test.
  addTearDown(() async {
    process.kill(ProcessSignal.sigterm);
    await process.exitCode.timeout(
      timeout,
      onTimeout: () {
        process.kill(ProcessSignal.sigkill);
        return -1;
      },
    );
  });

  return watcher;
}

int count(String text, String marker) => text.split(marker).length - 1;

Directory temporaryDirectory() {
  final Directory directory = Directory.systemTemp.createTempSync(
    'aspire_watch_',
  );
  addTearDown(() {
    if (directory.existsSync()) {
      directory.deleteSync(recursive: true);
    }
  });
  return directory;
}

void write(Directory directory, String name, String content) {
  final File file = File('${directory.path}/$name');
  file.parent.createSync(recursive: true);
  file.writeAsStringSync(content);
}
