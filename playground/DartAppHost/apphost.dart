// Aspire Dart AppHost - Playground
// For more information, see: https://aspire.dev
//
// Dart AppHost support is experimental. aspire.config.json turns the feature on
// for this directory. To turn it on for every AppHost, run:
//
//     aspire config set features:experimentalPolyglot:dart true --global
//
// To run:
//
//     aspire run
//
// The applications live in ../DartApps. That directory also holds a C# AppHost
// that builds the same model.

import '.aspire/modules/aspire.dart';

Future<void> main(List<String> args) async {
  final builder = await createBuilder(args);

  // The compute environment gives `aspire publish` a target. It writes a Docker
  // Compose file and one Dockerfile for each Dart application.
  await builder.addDockerComposeEnvironment('compose');

  final pg = await builder.addPostgres('pg');
  final appdb = await pg.addDatabase('appdb');
  final cache = await builder.addRedis('cache');

  final api = await builder.addServerpodApp(
      'api', '../DartApps/serverpod_api/serverpod_api_server');
  await api.withServerpodDatabase(appdb);
  await api.withServerpodRedis(cache);
  await api.withExternalHttpEndpoints();

  final site = await builder.addJasprApp('site', '../DartApps/jaspr_site');
  await site.withExternalHttpEndpoints();

  final worker = await builder.addDartApp('worker', '../DartApps/worker');
  await worker.withReference(api);
  await worker.waitFor(api);

  final app = await builder.build();
  await app.run();
}
