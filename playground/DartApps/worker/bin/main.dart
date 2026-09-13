// A plain Dart console application. `AddDartApp` starts it with `dart run bin/main.dart`.
//
// The application reads the address of the Serverpod API from the service discovery
// variable that `WithReference(api)` injects, and it polls the root path every two
// seconds.

import 'dart:async';
import 'dart:io';

/// The service discovery variable of the `api` resource and its `api` endpoint.
///
/// Aspire writes one variable for each endpoint, in the form
/// `services__<resource>__<endpoint>__<index>`.
const String apiVariable = 'services__api__api__0';

/// The time between two polls.
const Duration pollInterval = Duration(seconds: 2);

Future<void> main(List<String> args) async {
  final baseUrl = Platform.environment[apiVariable];

  if (baseUrl == null || baseUrl.isEmpty) {
    stderr.writeln('worker: $apiVariable is not set. Add WithReference(api) to the model.');
    exitCode = 1;
    return;
  }

  final demo = Platform.environment['DEMO'];
  stdout.writeln('worker: polls $baseUrl every ${pollInterval.inSeconds} s (DEMO=$demo)');

  final client = HttpClient();
  final target = Uri.parse(baseUrl);
  var counter = 0;

  // A stop signal must close the client, so the process exits without an open socket.
  final done = Completer<void>();
  ProcessSignal.sigterm.watch().listen((_) {
    if (!done.isCompleted) {
      done.complete();
    }
  });
  ProcessSignal.sigint.watch().listen((_) {
    if (!done.isCompleted) {
      done.complete();
    }
  });

  final timer = Timer.periodic(pollInterval, (_) async {
    counter++;

    try {
      final request = await client.getUrl(target);
      final response = await request.close();
      await response.drain<void>();
      stdout.writeln('worker: poll $counter api status=${response.statusCode}');
    } on Exception catch (error) {
      stdout.writeln('worker: poll $counter api error=$error');
    }
  });

  await done.future;

  timer.cancel();
  client.close(force: true);
  stdout.writeln('worker: stopped after $counter polls');
}
