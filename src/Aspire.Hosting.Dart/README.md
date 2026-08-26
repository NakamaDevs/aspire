# Dart app hosting integration

Use this integration to model, configure, and orchestrate Dart applications in an Aspire solution.

`AddDartApp` is the first-class path for **any** Dart program: a `shelf` server, `dart_frog`,
`conduit`, a Flutter web build, a `build_runner` site, or a command-line tool. `AddServerpodApp` and
`AddJasprApp` are thin presets on top of it. A preset only sets defaults, so every generic method
keeps working on it.

## Getting started

### Prerequisites

The **Dart SDK** (`dart`) must be available on the PATH of the machine that runs the AppHost.

A framework tool must also be on the PATH when you use it. `AddJasprApp` needs `jaspr`; install it
with `dart pub global activate jaspr_cli`. `WithRunCommand` adds a required command for the tool that
you name.

### Add the integration

From your AppHost directory, add the `Aspire.Hosting.Dart` integration with the Aspire CLI:

```bash
aspire add Aspire.Hosting.Dart
```

## Any Dart application

`AddDartApp` runs the application as `dart run bin/main.dart` from the directory that contains
`pubspec.yaml`.

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// A shelf server that reads the port from the PORT environment variable.
var api = builder.AddDartApp("api", "../dart-api")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const api = await builder.addDartApp("api", "../dart-api")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();

await builder.build().run();
```

Pass a different entrypoint with the third parameter, or with `.WithEntrypoint(...)` later:

```csharp
builder.AddDartApp("api", "../dart-api", entrypoint: "bin/server.dart");

builder.AddDartApp("api", "../dart-api")
    .WithEntrypoint("bin/server.dart");
```

### Arguments

`dart run` reads its own options before the file name and gives every argument after the file name to
the program. The integration keeps that separation:

| Method | Position | Use for |
|---|---|---|
| `.WithDartRunArgs(...)` | before the entrypoint | An option of the Dart runtime, for example `--enable-asserts` or `--observe`. |
| `.WithDartDefine(key, value)` | before the entrypoint | A compile-time value that `String.fromEnvironment` reads. |
| `.WithAppArgs(...)` | after the entrypoint | An argument of your program. |

```csharp
builder.AddDartApp("api", "../dart-api")
    .WithDartRunArgs("--enable-asserts")
    .WithDartDefine("FLAVOR", "dev")
    .WithAppArgs("--port", "8080");
// dart run --define=FLAVOR=dev --enable-asserts bin/main.dart --port 8080
```

A second call to `.WithDartRunArgs(...)` or `.WithAppArgs(...)` replaces the arguments of the first
call. `.WithDartDefine(...)` accumulates the keys, and a second call with the same key replaces its
value.

### A different command

Some Dart web frameworks ship their own command-line tool. `.WithRunCommand(...)` replaces the
complete command line and adds a required command for the tool:

```csharp
var api = builder.AddDartApp("api", "../api")
    .WithHttpEndpoint(env: "PORT");

api.WithRunCommand(
    "dart_frog", "dev", "--port", api.GetEndpoint("http").Property(EndpointProperty.TargetPort));
// dart_frog dev --port 8080
```

```csharp
var web = builder.AddDartApp("web", "../flutter-web")
    .WithHttpEndpoint(env: "PORT");

web.WithRunCommand(
    "flutter", "run", "-d", "web-server",
    "--web-port", web.GetEndpoint("http").Property(EndpointProperty.TargetPort));
```

The arguments of `.WithAppArgs(...)` still come after the arguments of `.WithRunCommand(...)`.

The options of `.WithDartDefine(...)` and `.WithDartRunArgs(...)` belong to `dart run`. Aspire writes
them directly after a leading `run` argument, and only when the command is `dart`:

```csharp
builder.AddDartApp("api", "../dart-api")
    .WithDartDefine("FLAVOR", "dev")
    .WithRunCommand("dart", "run", "bin/server.dart");
// dart run --define=FLAVOR=dev bin/server.dart
```

Every other command line drops those options, because no other position and no other tool accepts
them.

## Presets

A preset calls `AddDartApp` and then sets defaults for one framework. It adds no behavior that you
cannot add by hand, and every generic method above keeps working on it.

### Serverpod

`AddServerpodApp` adds a Serverpod server. The second parameter is the server directory of the
Serverpod project, which has the name `{project}_server`.

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("pg").AddDatabase("serverpod");
var cache = builder.AddRedis("cache");

builder.AddServerpodApp("api", "../myapp_server")
    .WithServerpodDatabase(db)
    .WithServerpodRedis(cache)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const db = await builder.addPostgres("pg").addDatabase("serverpod");
const cache = await builder.addRedis("cache");

await builder.addServerpodApp("api", "../myapp_server")
    .withServerpodDatabase(db)
    .withServerpodRedis(cache)
    .withExternalHttpEndpoints();

await builder.build().run();
```

The preset adds exactly this to `AddDartApp`:

1. The entrypoint `bin/main.dart`.
2. The options `--mode <mode>` and `--apply-migrations` after the entrypoint.
3. Three HTTP endpoints:

   | Endpoint | Target port | Port variable |
   |---|---|---|
   | `api` | 8080 | `SERVERPOD_API_SERVER_PORT` |
   | `insights` | 8081 | `SERVERPOD_INSIGHTS_SERVER_PORT` |
   | `web` | 8082 | `SERVERPOD_WEB_SERVER_PORT` |

4. `SERVERPOD_RUN_MODE`, and `SERVERPOD_<X>_SERVER_PUBLIC_HOST`, `_PUBLIC_PORT`, and `_PUBLIC_SCHEME`
   for each endpoint.
5. In publish mode, `SERVERPOD_PASSWORD_serviceSecret` from a generated secret parameter with the
   name `{name}-service-secret`.

A Serverpod environment variable wins over the value in the configuration file below `config`, so the
server listens on the ports that the table above names. Serverpod builds the URLs that it sends to a
client from the public values.

The `insights` endpoint carries the service protocol of the Serverpod tools, so `.WithReference(api)`
gives another resource the `api` and `web` endpoints only.

`.WithServerpodDatabase(db)` sets `SERVERPOD_DATABASE_HOST`, `_PORT`, `_NAME`, `_USER`, and
`SERVERPOD_PASSWORD_database`. It also adds a reference to the database and makes the server wait for
it.

`.WithServerpodRedis(cache)` sets `SERVERPOD_REDIS_ENABLED` to `true`, and it sets
`SERVERPOD_REDIS_HOST`, `SERVERPOD_REDIS_PORT`, and `SERVERPOD_PASSWORD_redis`.

Both methods take any resource with a connection string, and both read the values from its connection
properties. The comparison ignores letter case:

| Method | Properties |
|---|---|
| `.WithServerpodDatabase(...)` | `Host`, `Port`, `DatabaseName`, `Username`, `Password` |
| `.WithServerpodRedis(...)` | `Host`, `Port`, `Password` |

A PostgreSQL database from `AddPostgres(...).AddDatabase(...)` and a Redis cache from
`AddRedis(...)` expose those properties. So does any other resource that exposes them, which keeps
the two methods free of a dependency on the PostgreSQL and Redis packages.

A method sets an environment variable only when the resource has the property, so a resource without
a user or a password still works. `Host` and `Port` are necessary. A resource without them throws an
`InvalidOperationException` that names the resource and the missing property.

Serverpod itself supports PostgreSQL and Redis only, so the values must point to those two servers.

`.WithServerpodMode("staging")` changes the run mode. The mode selects `config/{mode}.yaml`, and it
becomes the value of the `--mode` option and of `SERVERPOD_RUN_MODE`. The default is `development` in
run mode and `production` in publish mode.

`.WithApplyMigrations(false)` removes the `--apply-migrations` option. Use it when a separate step
applies the migrations.

In run mode the preset does not set `SERVERPOD_PASSWORD_serviceSecret`, because
`config/passwords.yaml` holds the development value.

### Jaspr

`AddJasprApp` adds a Jaspr web application.

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddJasprApp("web", "../jaspr-web")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

await builder.addJasprApp("web", "../jaspr-web")
    .withExternalHttpEndpoints();

await builder.build().run();
```

The preset adds exactly this to `AddDartApp`:

1. The command `jaspr` instead of `dart`, and a required command for it.
2. The arguments `serve -p <port>`, where the port comes from the HTTP endpoint.
3. One HTTP endpoint with the variable `PORT`.
4. The rendering mode from the `jaspr` block of `pubspec.yaml`.
5. A mark that says the process reloads itself, so Aspire keeps its own restart off.

`jaspr serve` also binds a web port and a proxy port. The Jaspr defaults are 5467 and 5567. Two Jaspr
applications in one distributed application collide on those defaults, so give each application its
own pair:

```csharp
builder.AddJasprApp("docs", "../jaspr-docs")
    .WithJasprDevPorts(webPort: 5468, proxyPort: 5568);
```

`AddJasprApp` reads the rendering mode from `pubspec.yaml`:

```yaml
jaspr:
  mode: server
```

The mode tells the preset which Jaspr build to use: `jaspr serve` for a local run, and `jaspr build`
for a publish artifact. This integration runs `jaspr serve` today. The publish path is not part of it
yet. When the file holds no mode, the mode is `static`, which is the Jaspr default. Call
`.WithJasprMode(JasprMode.Server)` when the AppHost must hold a different value.

`JasprMode` belongs to the Jaspr preset only. It does not appear on `DartAppResource`, so a generic
Dart application never sees it.

## Run mode

### Pub dependencies

`AddDartApp` adds a sibling resource with the name `{app}-pub-get` when the application directory
contains `pubspec.yaml`. The step runs `dart pub get`, and the application waits for it. The step
runs in run mode only and stays out of the manifest. Every preset inherits the step.

Call `.WithPubGet(install: false)` to keep the step but start it by hand from the dashboard. Call
`.WithPubGet()` again to turn the automatic run back on.

### Live reload

`dart run` does not reload code, so a change needs a restart. `AddDartApp` and `AddServerpodApp`
therefore watch the source files and restart the resource. `AddJasprApp` does not, because
`jaspr serve` holds its own watcher.

Aspire looks at:

| Path | Accepts |
|---|---|
| `lib/`, with its subdirectories | `.dart` and `.yaml` files |
| `bin/`, with its subdirectories | `.dart` and `.yaml` files |
| `pubspec.yaml` in the application directory | the file itself |

Aspire ignores `build/` and every directory whose name starts with a period, which covers
`.dart_tool/` and `.git/`. An editor and a code generator write many files at one time, so Aspire
waits 500 milliseconds after the last change and then runs the restart command once.

Turn the restart off for a development server that reloads itself:

```csharp
builder.AddDartApp("api", "../api")
    .WithRunCommand("dart_frog", "dev")
    .WithLiveReload(false);
```

The method does nothing in publish mode, because the image holds a compiled executable and no source.

### Debugging

`AddDartApp` and `AddServerpodApp` make the resource debuggable in run mode. Aspire sends a `dart`
launch configuration to the IDE, and the
[Dart-Code](https://marketplace.visualstudio.com/items?itemName=Dart-Code.dart-code) extension starts
the session. An IDE that cannot start a `dart` launch configuration makes Aspire run a plain process.

The configuration carries the entrypoint, the working directory, the options of `dart run`, and the
arguments of the program. The resource command line does not change, so the dashboard shows the same
command line in a debug session and in a plain run.

Debugging follows the command:

| Command | Debuggable |
|---|---|
| `dart run <entrypoint>`, the default | yes |
| `.WithRunCommand("dart", "run", "bin/worker.dart")` | yes |
| `.WithRunCommand("dart_frog", "dev")`, or any other tool | no |
| `AddJasprApp`, which runs `jaspr serve` | no |

Only the Dart SDK has a contract with the Dart-Code debug adapter, so every other tool runs as a
plain process.

To attach a profiler or Dart DevTools instead, start the Dart VM service:

```csharp
builder.AddDartApp("api", "../dart-api")
    .WithVmService(8181);
// dart run --enable-vm-service=8181 bin/main.dart
```

Aspire does not allocate that port, so give each application its own value. Call `.WithVmService()`
without a port to let the VM select a free one.

## Publish mode

`aspire publish` turns the executable into a container image. Aspire generates the Dockerfile, unless
the application directory already contains a `Dockerfile`. That file is the contract of the
repository, so Aspire never replaces it. A Serverpod project ships one.

Both shapes below share one build stage. It starts from the official `dart` image, copies
`pubspec.*`, runs `dart pub get`, copies the rest of the source, and runs `dart pub get --offline`.
The manifest is copied first, so the dependency layer stays in the cache across source edits.

### The executable shape, which is the default

The build stage runs `dart compile exe` on the entrypoint. The runtime stage is `scratch` with the
`/runtime` directory of the Dart image, so the image holds no SDK, no source, and no shell.

```dockerfile
FROM docker.io/library/dart:3.9.4 AS build
WORKDIR /app
COPY pubspec.* ./
RUN dart pub get
COPY . .
RUN dart pub get --offline
RUN dart compile exe bin/main.dart -o /app/bin/server

FROM scratch
COPY --from=build /runtime/ /
WORKDIR /app
COPY --from=build /app/bin/server /app/bin/
CMD ["/app/bin/server"]
```

Use `.WithPublishEntrypoint("bin/production.dart")` when the image must compile a file other than the
run entrypoint.

### The static-site shape

A Dart web framework can build a directory of files that a web server sends to the browser. No Dart
process is left, so the runtime stage is `nginx:alpine`.

```csharp
builder.AddDartApp("web", "../flutter-web")
    .WithStaticSiteBuild(
        "flutter",
        ["build", "web", "--release"],
        "build/web",
        buildImage: "ghcr.io/cirruslabs/flutter:stable");
```

```dockerfile
FROM ghcr.io/cirruslabs/flutter:stable AS build
WORKDIR /app
COPY pubspec.* ./
RUN dart pub get
COPY . .
RUN dart pub get --offline
RUN flutter build web --release

FROM docker.io/library/nginx:alpine
COPY --from=build /app/build/web /usr/share/nginx/html
EXPOSE 80
```

The default build image holds the Dart SDK only, so name an image that holds `flutter` with the
`buildImage` parameter, as the example above does. For a command that comes from a pub package, use
`.WithStaticSiteTool("<package>")` instead. The build stage then runs
`dart pub global activate <package>` and puts `/root/.pub-cache/bin` on the search path.

`WithDockerfileBaseImage(buildImage: ...)` wins over the `buildImage` parameter, because it is the
general override of both stage images.

Nginx binds port 80, so `WithStaticSiteBuild` sets the target port of the `http` endpoint to 80 in
publish mode, and it creates that endpoint when the resource has none. It changes nothing in run mode.

Pass `spaFallback: true` when the browser owns the routes. Nginx then sends `index.html` for every
path that names no file:

```csharp
builder.AddDartApp("web", "../flutter-web")
    .WithStaticSiteBuild(
        "flutter",
        ["build", "web"],
        "build/web",
        spaFallback: true,
        buildImage: "ghcr.io/cirruslabs/flutter:stable");
```

### What each preset chooses

| Preset | Shape | Image contents |
|---|---|---|
| `AddDartApp` | executable | `dart compile exe` on the run entrypoint, or on `WithPublishEntrypoint`. |
| `AddServerpodApp` | executable | `dart compile exe bin/main.dart`, plus `--mode <mode>` and `--apply-migrations` in `CMD` and `SERVERPOD_RUN_MODE` in `ENV`. |
| `AddJasprApp`, mode `static` or `client` | static site | `jaspr build` from `jaspr_cli`, output `build/jaspr`, served by Nginx. |
| `AddJasprApp`, mode `server` | executable | `jaspr build` from `jaspr_cli`. The image gets the complete `build/jaspr` directory and runs `/app/app`. |

The Serverpod values follow `WithServerpodMode` and `WithApplyMigrations`, because Aspire reads both
when it generates the Dockerfile. The default mode in publish mode is `production`.

`jaspr build` writes `build/jaspr` in every mode and takes no output option. In server mode the
directory holds the compiled server `app` and the browser bundle below `web`. The server reads that
bundle from the directory that holds the executable, so the image receives the complete directory.
`WithJasprMode` replaces the shape of the earlier mode.

### Base images

`WithDockerfileBaseImage(buildImage: ..., runtimeImage: ...)` replaces either image. When the runtime
image is not `scratch`, Aspire leaves out the `/runtime` copy, because a full base image already holds
a C library and a loader. It still copies the executable and sets `CMD`.

The default build image tag comes from the detected Dart version: `.tool-versions` first, then the
`environment: sdk:` lower bound in `pubspec.yaml`, then `3.12.2`. A `pubspec.yaml` lower bound is a
constraint and not a released SDK version, so the tag can name an image that does not exist. Pin the
toolchain in `.tool-versions`, or name the image with `WithDockerfileBaseImage`.

### `.dockerignore`

Aspire writes a `.dockerignore` next to the generated Dockerfile, unless the application directory
already contains one. The rules keep `.dart_tool`, `build`, `node_modules`, and the files of a
checkout out of the build context. Both are rebuilt inside the image, so neither must reach the
daemon.

## OpenTelemetry

Every Dart resource gets the OpenTelemetry environment variables of the Aspire dashboard. The
application reads them and sends its telemetry to the dashboard.

| Variable | Holds |
|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | The OTLP endpoint of the dashboard. |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` or `http/protobuf`. |
| `OTEL_EXPORTER_OTLP_HEADERS` | The API key of the dashboard, as `key=value` pairs. |
| `OTEL_SERVICE_NAME` | The name of the resource. |
| `OTEL_RESOURCE_ATTRIBUTES` | The instance attributes of the resource. |

The [`opentelemetry`](https://pub.dev/packages/opentelemetry) package sends OTLP over HTTP, so tell
Aspire to give the application the HTTP endpoint of the dashboard:

```csharp
builder.AddDartApp("api", "../dart-api")
    .WithOtlpExporter(OtlpProtocol.HttpProtobuf);
```

That overload needs `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` in the AppHost configuration.

Read the two variables in the Dart application:

```dart
import 'dart:io';

import 'package:opentelemetry/api.dart' as api;
import 'package:opentelemetry/sdk.dart' as sdk;

void configureTelemetry() {
  final endpoint = Platform.environment['OTEL_EXPORTER_OTLP_ENDPOINT'];
  if (endpoint == null) {
    return;
  }

  final headers = <String, String>{};
  final rawHeaders = Platform.environment['OTEL_EXPORTER_OTLP_HEADERS'];
  if (rawHeaders != null && rawHeaders.isNotEmpty) {
    for (final pair in rawHeaders.split(',')) {
      final separator = pair.indexOf('=');
      if (separator > 0) {
        headers[pair.substring(0, separator).trim()] =
            Uri.decodeComponent(pair.substring(separator + 1).trim());
      }
    }
  }

  final exporter = sdk.CollectorExporter(
    Uri.parse('$endpoint/v1/traces'),
    headers: headers,
  );

  api.registerGlobalTracerProvider(
    sdk.TracerProviderBase(processors: [sdk.BatchSpanProcessor(exporter)]),
  );
}
```

Confirm the constructor of `CollectorExporter` against the version of the package that the project
uses. The API of the Dart OpenTelemetry package is not stable.

## Certificate trust

In run mode Aspire builds a certificate bundle and gives the path to the application in two
variables:

- `SSL_CERT_FILE` — the trust set of the Dart runtime.
- `OTEL_EXPORTER_OTLP_CERTIFICATE` — the trust set of an OTLP exporter.

The default scope is `CertificateTrustScope.System`, so the bundle holds the system authorities and
the custom authorities together. The Dart runtime replaces its complete trust set with the file, so
Aspire does not set `SSL_CERT_FILE` for an `Append` bundle. The `{app}-pub-get` step does not receive
the variables.

## Known limits

- **`SSL_CERT_FILE` works on Linux only.** The Dart runtime on macOS reads the trust set from the
  system keychain, and on Windows from the certificate store. The variable does not change the trust
  set on those two systems. To trust the Aspire development certificate there, add the certificate to
  the keychain or to the store, or give the client its own `SecurityContext`.
- **The Dart OpenTelemetry package sends OTLP over HTTP.** Aspire gives a resource the gRPC endpoint
  of the dashboard by default. Use `.WithOtlpExporter(OtlpProtocol.HttpProtobuf)` for a Dart
  application, and configure `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`.
- **The published image runs as root.** A `scratch` image holds no account database, so the image
  cannot name a different user. The image also holds no shell and no package manager.
- **The Dart version of the default build image comes from a constraint.** A `pubspec.yaml` lower
  bound is not always a released SDK version. Pin the version in `.tool-versions`, or name the image
  with `WithDockerfileBaseImage`.
- **A pub workspace does not publish with a generated Dockerfile.** The build context is the
  application directory alone, so `dart pub get` in the image finds no workspace root and stops with
  `found no workspace root including it in parent directories`. Author a Dockerfile for a package
  that carries `resolution: workspace`. A Serverpod project is such a workspace, and
  `serverpod create` already writes a Dockerfile that Aspire keeps.
- **A Serverpod server binds the fixed target ports 8080, 8081, and 8082.** The ports match the
  Serverpod documentation and the generated client, so Aspire does not allocate them. Two Serverpod
  servers in one AppHost therefore collide in run mode. Give the second server different target
  ports with `.WithEndpoint(...)`.
- **The Jaspr server image reads its port from the application.** In run mode `jaspr serve` binds the
  port that Aspire gives it with `-p`. The published image has no such option, so the Jaspr server
  must read `PORT` itself.

## Additional documentation

- https://aspire.dev/integrations/gallery/
- [Aspire documentation](https://aspire.dev/)
- [Dart documentation](https://dart.dev/)
- [Serverpod documentation](https://docs.serverpod.dev/)
- [Jaspr documentation](https://docs.jaspr.site/)

## Feedback & contributing

https://github.com/microsoft/aspire
