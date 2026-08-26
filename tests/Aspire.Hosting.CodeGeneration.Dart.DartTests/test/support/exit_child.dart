// A guest process for the exit test.
//
// The transport listens on the socket, and that subscription keeps the Dart
// event loop alive. A script that connects and then returns from `main` never
// exits. This child closes the transport, so the loop ends and the process
// exits. The generated `run` method does the same after the AppHost stops.
//
// `exit_child_test.dart` starts the child with REMOTE_APP_HOST_SOCKET_PATH.

import 'dart:io';

import '../../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/transport.dart';

Future<void> main() async {
  final AspireTransport transport = await AspireTransport.connect();
  stdout.writeln('ping:${await transport.ping()}');

  await transport.close();

  // close is idempotent, so a second call changes nothing and throws nothing.
  await transport.close();

  stdout.writeln('closed:${transport.isConnected}');
  stdout.writeln('done');
}
