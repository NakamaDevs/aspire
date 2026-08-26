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
  await api.withServerpodDatabase(asConnectionString(appdb));
  await api.withServerpodRedis(asConnectionString(cache));
  await api.withExternalHttpEndpoints();

  final site = await builder.addJasprApp('site', '../DartApps/jaspr_site');
  await site.withExternalHttpEndpoints();

  final worker = await builder.addDartApp('worker', '../DartApps/worker');
  await worker.withReference(api);
  await worker.waitFor(asResource(api));

  final app = await builder.build();
  await app.run();
}

/// Views a generated resource as a `ResourceWithConnectionString`.
///
/// The generated SDK gives every resource its own class, and each class extends
/// `AspireObject` directly. A Dart class therefore never satisfies an interface
/// parameter such as the `database` parameter of `withServerpodDatabase`. The two
/// helpers below rebuild the wrapper around the same handle and the same
/// transport, which is what the host expects on the wire.
ResourceWithConnectionString asConnectionString(AspireObject resource) =>
    ResourceWithConnectionString(resource.handle, resource.transport);

/// Views a generated resource as a `Resource`, which `waitFor` takes.
Resource asResource(AspireObject resource) =>
    Resource(resource.handle, resource.transport);
