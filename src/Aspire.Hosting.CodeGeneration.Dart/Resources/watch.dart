// File watcher for the Aspire Dart AppHost.
//
// This file is copied verbatim into `.aspire/modules/`. It uses the Dart SDK
// libraries only, and it loads none of the other SDK files. `aspire.dart` never
// imports it: the CLI runs it as the parent process of the AppHost when watch
// mode is on.
//
//     dart run .aspire/modules/watch.dart apphost.dart
//
// The watcher starts the AppHost with inherited stdio, then polls the
// modification time of `apphost.dart`, of `pubspec.yaml` and of every `*.dart`
// file below the AppHost directory. A change stops the child with SIGTERM, and
// with SIGKILL after five seconds, and starts it again.
//
// ## Options
//
//   * `--once` — do not restart. The watcher stops with the exit code of the
//     child. The tests use it to check the exit code.
//
// ## Behaviour
//
// The watcher follows `nodemon`. When the AppHost stops on its own, the watcher
// stays alive and waits for the next file change. `.dart_tool/`, `build/` and
// every directory whose name starts with a dot, such as `.aspire/`, are not
// watched.

import 'dart:async';
import 'dart:io';

/// The time between two snapshots of the watched files.
const Duration _pollInterval = Duration(milliseconds: 500);

/// The time the watcher waits for SIGTERM before it sends SIGKILL.
const Duration _stopTimeout = Duration(seconds: 5);

/// Directory names the watcher never descends into.
const List<String> _ignoredDirectories = <String>['.dart_tool', 'build'];

/// The manifest the watcher follows beside the AppHost file.
const String _manifestFileName = 'pubspec.yaml';

Future<void> main(List<String> arguments) async {
  final bool once = arguments.contains('--once');
  final List<String> rest = arguments
      .where((String argument) => argument != '--once')
      .toList();

  if (rest.isEmpty) {
    stderr.writeln('usage: dart run watch.dart <apphost.dart> [--once]');
    exit(2);
  }

  await _Watcher(
    appHostFile: rest.first,
    childArguments: rest.sublist(1),
    once: once,
  ).run();
}

class _Watcher {
  _Watcher({
    required this.appHostFile,
    required this.childArguments,
    required this.once,
  }) : appHostPath = File(appHostFile).absolute.path,
       root = File(appHostFile).absolute.parent.path;

  /// The AppHost path as the caller wrote it. The child gets the same text.
  final String appHostFile;

  /// The arguments that go to the AppHost after its own path.
  final List<String> childArguments;

  /// True when the watcher stops with the exit code of the first child.
  final bool once;

  /// The absolute AppHost path. The snapshot is keyed by absolute paths.
  final String appHostPath;

  /// The directory the watcher polls.
  final String root;

  Map<String, String> _snapshot = const <String, String>{};
  Process? _child;
  int? _childStatus;
  bool _reportedStop = false;

  Future<void> run() async {
    _installSignalHandlers();
    _snapshot = _takeSnapshot();
    await _startChild();

    while (true) {
      await Future<void>.delayed(_pollInterval);

      final int? status = _childStatus;
      if (status != null) {
        if (once) {
          exit(status);
        }
        if (!_reportedStop) {
          _reportedStop = true;
          stderr.writeln(
            '[aspire-watch] apphost stopped with status $status. '
            'Waiting for a file change.',
          );
        }
      }

      final Map<String, String> current = _takeSnapshot();
      final String? changed = _firstChange(_snapshot, current);
      _snapshot = current;

      if (changed == null) {
        continue;
      }

      stderr.writeln('[aspire-watch] restarting: ${_relative(changed)}');
      await _stopChild();
      await _startChild();
    }
  }

  // ── Shutdown ──────────────────────────────────────────────────────────────

  /// Stops the AppHost when the watcher itself is asked to stop.
  ///
  /// Without this the child outlives the watcher, because the CLI stops the
  /// watcher and never learns the process identifier of the AppHost.
  void _installSignalHandlers() {
    for (final ProcessSignal signal in <ProcessSignal>[
      ProcessSignal.sigterm,
      ProcessSignal.sigint,
    ]) {
      signal.watch().listen((ProcessSignal _) {
        unawaited(_shutdown());
      });
    }
  }

  Future<void> _shutdown() async {
    await _stopChild();
    exit(0);
  }

  // ── Child process ─────────────────────────────────────────────────────────

  Future<void> _startChild() async {
    _childStatus = null;
    _reportedStop = false;

    // Platform.resolvedExecutable is the `dart` binary that runs this script,
    // so the child uses the same SDK and no PATH lookup is necessary.
    final Process child = await Process.start(
      Platform.resolvedExecutable,
      <String>['run', appHostFile, ...childArguments],
      workingDirectory: root,
      mode: ProcessStartMode.inheritStdio,
    );

    _child = child;
    unawaited(
      child.exitCode.then((int code) {
        if (identical(_child, child)) {
          _childStatus = code;
        }
      }),
    );
  }

  Future<void> _stopChild() async {
    final Process? child = _child;
    _child = null;
    _childStatus = null;

    if (child == null) {
      return;
    }

    child.kill(ProcessSignal.sigterm);
    try {
      await child.exitCode.timeout(_stopTimeout);
    } on TimeoutException {
      child.kill(ProcessSignal.sigkill);
      await child.exitCode;
    }
  }

  // ── File snapshot ─────────────────────────────────────────────────────────

  Map<String, String> _takeSnapshot() {
    final Map<String, String> stamps = <String, String>{};
    _stamp(stamps, appHostPath);
    _stamp(stamps, '$root${Platform.pathSeparator}$_manifestFileName');
    _collect(Directory(root), stamps);
    return stamps;
  }

  void _collect(Directory directory, Map<String, String> stamps) {
    List<FileSystemEntity> entries;
    try {
      entries = directory.listSync(followLinks: false);
    } on FileSystemException {
      return;
    }

    for (final FileSystemEntity entry in entries) {
      final String name = _baseName(entry.path);

      if (entry is Directory) {
        if (name.startsWith('.') || _ignoredDirectories.contains(name)) {
          continue;
        }
        _collect(entry, stamps);
        continue;
      }

      if (entry is File && name.endsWith('.dart')) {
        _stamp(stamps, entry.path);
      }
    }
  }

  static void _stamp(Map<String, String> stamps, String path) {
    final File file = File(path);
    try {
      final FileStat stat = file.statSync();
      if (stat.type == FileSystemEntityType.notFound) {
        return;
      }
      stamps[file.absolute.path] =
          '${stat.modified.microsecondsSinceEpoch}:${stat.size}';
    } on FileSystemException {
      // A file that disappeared between the listing and the stat is a change
      // the next snapshot reports.
    }
  }

  /// Returns the first path that is new, changed or removed, or null.
  static String? _firstChange(
    Map<String, String> previous,
    Map<String, String> current,
  ) {
    for (final String path in current.keys.toList()..sort()) {
      if (previous[path] != current[path]) {
        return path;
      }
    }
    for (final String path in previous.keys.toList()..sort()) {
      if (!current.containsKey(path)) {
        return path;
      }
    }
    return null;
  }

  String _relative(String path) {
    final String prefix = '$root${Platform.pathSeparator}';
    return path.startsWith(prefix) ? path.substring(prefix.length) : path;
  }

  static String _baseName(String path) {
    final int index = path.lastIndexOf(Platform.pathSeparator);
    return index < 0 ? path : path.substring(index + 1);
  }
}
