// A guest process for the environment tests.
//
// Dart cannot change the environment of the running process, so the two
// environment cases run in this child. `env_child_test.dart` starts it with
// REMOTE_APP_HOST_SOCKET_PATH, and with ASPIRE_REMOTE_APPHOST_TOKEN when the
// case needs authentication. The child prints one marker for each step.

import 'dart:io';

import '../../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/base.dart';
import '../../../../src/Aspire.Hosting.CodeGeneration.Dart/Resources/transport.dart';

Future<void> main() async {
  try {
    final AspireTransport transport = await AspireTransport.connect();
    stdout.writeln('socketPath:${transport.socketPath}');
    stdout.writeln(
      'default:${identical(AspireTransport.defaultInstance, transport)}',
    );
    stdout.writeln('ping:${await transport.ping()}');
    await transport.close();
    stdout.writeln('done');
  } on AspireError catch (error) {
    stdout.writeln('error:${error.code}:${error.message}');
    exitCode = 1;
  }
}
