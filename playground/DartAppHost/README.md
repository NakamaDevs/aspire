# DartAppHost playground

This playground runs a Dart AppHost. The AppHost model lives in `apphost.dart`. The Aspire CLI
generates a Dart SDK into `.aspire/modules/` and then runs the script with `dart run`.

The applications come from [`../DartApps`](../DartApps). That directory also holds a C# AppHost
with the same model, so you can compare the two languages side by side.

## Model

```dart
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
```

Every generated resource class implements the Dart class of each Aspire interface that its .NET
type implements, so a resource passes straight into `withServerpodDatabase(...)`,
`withReference(...)`, and `waitFor(...)`.

## Layout

| Path | Holds |
| --- | --- |
| `apphost.dart` | The AppHost model |
| `pubspec.yaml` | The package manifest. It declares no dependency. |
| `aspire.config.json` | The language, the feature flags, and the integration packages |
| `apphost.run.json` | The dashboard and OTLP endpoints |
| `.aspire/modules/` | The generated SDK. The CLI writes it. Do not edit it. |

## Prerequisites

- The Dart SDK on the PATH. The playground was verified with Dart 3.12.2.
- The `jaspr` command-line tool, because `AddJasprApp` starts `jaspr serve`.
- A container runtime for the PostgreSQL and the Redis container.
- Network access on the first run, because `dart pub get` reads from pub.dev.
- `../DartApps/serverpod_api/serverpod_api_server/config/passwords.yaml` must exist. See the
  "Known limits" section of the [DartApps README](../DartApps/README.md).

## How to run

Dart AppHost support is experimental. `aspire.config.json` turns the feature on for this
directory. To turn it on for every AppHost, run:

```bash
aspire config set features:experimentalPolyglot:dart true --global
```

Then run the AppHost:

```bash
cd playground/DartAppHost
aspire run
```

To run the CLI from this repository instead of an installed CLI, follow
["Local Development Workflow"](../../docs/specs/polyglot-apphost.md#local-development-workflow):

```bash
export ASPIRE_REPO_ROOT="/path/to/aspire"
export PATH="$ASPIRE_REPO_ROOT/.dotnet:$PATH"
export DOTNET_ROOT="$ASPIRE_REPO_ROOT/.dotnet"
"$ASPIRE_REPO_ROOT/artifacts/bin/Aspire.Cli/Debug/net10.0/aspire" run
```

Build that CLI first when it is older than `src/Aspire.Cli`:

```bash
"$ASPIRE_REPO_ROOT/.dotnet/dotnet" build "$ASPIRE_REPO_ROOT/src/Aspire.Cli/Aspire.Cli.csproj"
```

`ASPIRE_REPO_ROOT` puts the CLI in development mode. The CLI then resolves the Aspire packages
through project references and regenerates the SDK on every run.

Stop the AppHost with `CTRL+C`, or with `aspire stop` from a second terminal.

## Watch mode

`aspire.config.json` sets `defaultWatchEnabled`. The CLI therefore starts the AppHost through
`.aspire/modules/watch.dart`. The script polls the modification time of `apphost.dart`, of
`pubspec.yaml`, and of every `*.dart` file below this directory. On a change it stops the
AppHost and starts it again.

## Publish

```bash
aspire publish -o ./out
```

The model adds a Docker Compose compute environment, so `aspire publish` writes
`out/docker-compose.yaml` and one Dockerfile for each Dart application that has no authored
Dockerfile.

## E2E checklist

The results come from a run on 2026-08-26 on macOS 26.5.2 (Apple Silicon), with Dart 3.12.2,
Serverpod 3.4.12, and Jaspr 0.23.4. The CLI came from
`artifacts/bin/Aspire.Cli/Debug/net10.0/aspire` on branch `feature/dart-integration`, with
`ASPIRE_REPO_ROOT` set.

Steps 1 to 5, 7, and 8 come from one run after the generator started to emit the interface
classes. Step 6 comes from an earlier run on the same day. `watch.dart` did not change between
the two runs.

| Step | Result | Evidence |
| --- | --- | --- |
| 1. Generate the SDK | Pass | `Generated 37 Dart files in .../DartAppHost/.aspire/modules (37 changed)` |
| 2. The model analyzes | Pass | `dart analyze --fatal-infos apphost.dart` printed `No issues found!`. The model passes each resource directly, with no conversion helper. |
| 3. Resources start | Pass | See the state table below. |
| 4. HTTP endpoints answer | Pass | See the curl output below. |
| 5. Worker reads the API | Pass | `services__api__api__0=http://localhost:59508` on the worker resource. |
| 6. Watch restart | Pass | See the watch section below. |
| 7. Stop | Pass | `aspire stop` reported success in 1.5 seconds. No `dart`, `dartvm`, `dartaotruntime`, or `jaspr` process of this playground remained. |
| 8. Publish | Pass | The pipeline succeeded and `aspire publish` returned exit code 0 after 12.968 seconds. See the publish section. |

### Step 3: resource states

`aspire describe --format json` returned:

```
api-rczjmbff             Running   Healthy   http://localhost:59508 (api)
                                             http://localhost:59511 (insights)
                                             http://localhost:59507 (web)
api-pub-get-tzxzbzqk     Finished  exit=0
appdb                    Running   Healthy
cache-yyycgsvz           Running   Healthy   rediss://localhost:59509, tcp://localhost:59510
pg-tcenpznv              Running   Healthy   tcp://localhost:59506
site-wdyezggs            Running   Healthy   http://localhost:59505
site-pub-get-cxbkgtxx    Finished  exit=0
worker-xchkmdrm          Running   Healthy
worker-pub-get-kqkxrnrz  Finished  exit=0
```

`appdb` is a logical child of `pg`. The server starts with `--apply-migrations`, so a successful
start proves that the database exists. The `*-pub-get` steps come from the Dart integration.

### Step 4: HTTP endpoints

Aspire allocated port 59508 for the `api` endpoint of `api`, and port 59505 for `site`.

```
$ curl -o /dev/null -w "%{http_code}" http://localhost:59508/
200

$ curl -o /dev/null -w "%{http_code}" http://localhost:59505/
200
```

### Step 6: watch restart

The test added one line to the worker chain in `apphost.dart`:

```dart
await worker.withEnvironment('DEMO', '1');
```

The CLI log recorded the restart one second after the change:

```
[2026-08-26 15:54:24.281] [FAIL] [AppHost] [aspire-watch] restarting: apphost.dart
```

The `[FAIL]` label is the CLI log category for the standard error stream of the AppHost. The
watcher writes its messages to standard error. The restart succeeded. After the restart,
`aspire describe` showed the new environment variable on the new worker resource:

```
worker-rdkkqybh   Running   Healthy   DEMO=1
```

The `site` resource did not survive this restart. See "Known limits".

### Step 8: publish

```
$ time aspire publish -o ./out
✅ 7/7 steps succeeded
✅ Pipeline succeeded
aspire publish -o ./out  8.07s user 2.18s system 79% cpu 12.968 total
$ echo $?
0

$ find out -type f
out/.env
out/docker-compose.yaml
out/site.Dockerfile
out/site.Dockerfile.dockerignore
out/worker.Dockerfile
out/worker.Dockerfile.dockerignore
```

The command returns. The generated `run` method closes the transport when the AppHost stops, so
the Dart process exits and the CLI does not wait. An earlier run of the same model stayed alive
for more than 2 minutes after `Pipeline succeeded`, until the process was killed by hand.

`out/docker-compose.yaml` holds the services `compose-dashboard`, `pg`, `cache`, `api`, `site`,
`worker`, and `aspire`. The `api` service runs `--mode production --apply-migrations` and reads
`SERVERPOD_RUN_MODE=production`.

`out/site.Dockerfile` runs `jaspr build` on `dart:3.10.0` and serves `build/jaspr` from
`nginx:alpine`. `out/worker.Dockerfile` runs `dart compile exe` and copies the executable and the
Dart runtime into a `scratch` image. Aspire writes no `api.Dockerfile`, because the Serverpod
project ships its own Dockerfile and Aspire keeps an authored file.

The `out` directory is a run artifact. Delete it after the check.

## Known limits

### A watch restart can break the Jaspr site

`jaspr serve` opens an internal proxy server on a fixed port. Aspire allocates the `http` endpoint
only, and it passes that port with `-p`. On a watch restart, the new `jaspr serve` starts before
the old one releases the internal port, and it stops with exit code 1:

```
Error: SocketException: Failed to create server socket (OS Error: Address already in use,
errno = 48), address = 0.0.0.0, port = 5567
```

`aspire resource site start` then starts the resource again and it reaches Running. Use
`withJasprDevPorts` to choose the internal ports, but that does not remove the race, because both
processes then use the same chosen ports.

### The first Jaspr build takes minutes

`jaspr serve` runs `build_runner`, which compiles the whole dependency tree. The resource reaches
Running before the first page is ready, so a request can fail until the build ends.
