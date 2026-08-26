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
server listens on the ports that Aspire assigns. Serverpod builds the URLs that it sends to a client
from the public values.

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
- **The Jaspr preset has no publish path yet.** `AddJasprApp` runs `jaspr serve`. It does not build a
  publish artifact with `jaspr build`.

## Feedback & contributing

https://github.com/microsoft/aspire
