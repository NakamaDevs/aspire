# DartApps playground

This playground runs three Dart applications under Aspire. It exercises `AddServerpodApp`,
`AddJasprApp`, `AddDartApp`, `WithServerpodDatabase`, `WithServerpodRedis`, and `WithReference`.

The AppHost model is:

```csharp
var db = builder.AddPostgres("pg").AddDatabase("appdb");
var cache = builder.AddRedis("cache");

var api = builder.AddServerpodApp("api", "../serverpod_api/serverpod_api_server")
                 .WithServerpodDatabase(db)
                 .WithServerpodRedis(cache)
                 .WithExternalHttpEndpoints();

builder.AddJasprApp("site", "../jaspr_site")
       .WithExternalHttpEndpoints();

builder.AddDartApp("worker", "../worker")
       .WithReference(api)
       .WaitFor(api);
```


## Before the first run: Serverpod passwords

`serverpod create` writes `serverpod_api/serverpod_api_server/config/passwords.yaml` with
generated secrets. The file is not committed. Copy the example and keep the placeholder values
for development, or run `serverpod create` again to generate new ones:

```sh
cp serverpod_api/serverpod_api_server/config/passwords.yaml.example \
   serverpod_api/serverpod_api_server/config/passwords.yaml
```

Aspire supplies `SERVERPOD_PASSWORD_database` and `SERVERPOD_PASSWORD_redis` from the Postgres
and Redis resources; the other keys in the file (service secret, auth peppers) come from this
file in development.

A run on 2026-08-26 confirmed this path. With the placeholder values from the example file, `api`
reached Running, `GET /` on its API endpoint returned 200, and `worker` logged
`poll N api status=200`. Serverpod does not check the content of these values in development.

## Layout

| Path | Holds |
| --- | --- |
| `DartApps.AppHost/` | The C# AppHost |
| `serverpod_api/serverpod_api_server/` | A Serverpod 3.4 server with a PostgreSQL database and a Redis cache |
| `jaspr_site/` | A Jaspr static site with multi-page routing |
| `worker/` | A plain Dart console application that polls the Serverpod API |

## Prerequisites

- The Dart SDK on the PATH. The playground was verified with Dart 3.12.2.
- The `serverpod` and `jaspr` command-line tools for a new scaffold. A run of this playground
  needs `jaspr` only, because `AddJasprApp` starts `jaspr serve`.
- A container runtime for the PostgreSQL and the Redis container.
- Network access on the first run, because `dart pub get` reads from pub.dev.
- `serverpod_api_server/config/passwords.yaml` must exist. See
  "Before the first run: Serverpod passwords" above.

## How to run

```bash
cd DartApps.AppHost
dotnet run
```

The log prints the dashboard URL and the login token. Open that URL.

## What to look for in the dashboard

- Resources `pg`, `appdb`, `cache`, `api`, `site`, and `worker` reach the Running state.
- The setup siblings `api-pub-get`, `site-pub-get`, and `worker-pub-get` reach Finished with
  exit code 0.
- The `api` resource holds three endpoints. `AddServerpodApp` adds `api` (port 8080),
  `insights` (8081), and `web` (8082). `GET /` on the `api` endpoint answers status 200.
- The `site` endpoint answers `GET /` with an HTML page.
- The console log of `worker` shows one poll line every two seconds.

## Notes on the Dart code

- `serverpod create` writes a pub workspace with a server, a client, and a Flutter package. This
  playground keeps the server only. `serverpod_api_server/pubspec.yaml` therefore drops
  `resolution: workspace`, so `dart pub get` works in the server directory alone.
- `WithServerpodDatabase` and `WithServerpodRedis` set the `SERVERPOD_DATABASE_*`,
  `SERVERPOD_REDIS_*`, and `SERVERPOD_PASSWORD_*` variables. They win over
  `config/development.yaml`, so the server reaches the containers that Aspire started, not the
  ports of the Serverpod Docker Compose file. That file is not part of this playground.
- `serverpod_api_server/Dockerfile` comes from the Serverpod template. This playground changed the
  build context from the workspace root to the server directory, because the package is
  standalone now. Aspire keeps an authored Dockerfile, so `aspire publish` uses this file.
- The Jaspr template pins `build_web_compilers: ^4.8.10`, and that release needs Dart 3.13 or
  later. `jaspr_site/pubspec.yaml` relaxes the constraint to `^4.8.0`, which still accepts every
  later 4.x release.
- `worker/bin/main.dart` uses `dart:io` only. It reads `services__api__api__0`, which
  `WithReference(api)` injects, and polls the root path of the Serverpod API every two seconds.

## E2E checklist

The results come from a run on 2026-08-26 on macOS 26.5.2 (Apple Silicon), with Dart 3.12.2,
Serverpod 3.4.12, and Jaspr 0.23.4, on branch `feature/dart-integration`.

| Step | Result | Evidence |
| --- | --- | --- |
| 1. Build the AppHost | Pass | `dotnet build` printed `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| 2. Resources start | Pass | See the state table below. |
| 3. Serverpod API answers | Pass | `GET http://localhost:65170/` returned status 200. |
| 4. Jaspr site answers | Pass | `GET http://localhost:65186/` returned status 200 and a body that starts with `<!DOCTYPE html>`. |
| 5. Worker polls the API | Pass | See the worker log lines below. |
| 6. Jaspr live reload | Pass | The page changed at 15:48:22 UTC. The new text was served at 15:48:27 UTC. The `jaspr serve` process kept PID 46767, so Aspire did not restart the resource. |
| 7. Dart live reload | Pass | `worker/bin/main.dart` changed at 15:48:39 UTC. The worker process changed from PID 47239 to PID 53457 and printed its first new line at 15:48:50 UTC. |
| 8. Stop the AppHost | Pass | After `SIGINT`, no `dart`, `dartvm`, `dartaotruntime`, or `jaspr` process of this playground remained. The `pg` and `cache` containers were removed. |
| 9. Publish smoke | Pass | See the publish section below. |

### Step 2: resource states

The states come from the DCP API server of the run:

```
site-fzmvbygq           Running    exit=None
api-njxxaeyr            Running    exit=None
worker-vamcknsf         Running    exit=None
worker-pub-get-hbtezkyt Finished   exit=0
site-pub-get-uutednbf   Finished   exit=0
api-pub-get-tvgtfdqf    Finished   exit=0
aspire-dashboard-xrqprsga Running  exit=None
cache-yzznrvey          Running    exit=None
pg-hvmjbsvq             Running    exit=None
```

`appdb` is a logical child of `pg`. The PostgreSQL integration creates the database. The server
starts with `--apply-migrations`, so a successful start proves that the database exists.

### Step 3 and step 4: HTTP endpoints

Aspire allocated port 65170 for the `api` endpoint of `api`, and port 65186 for `site`.

```
$ curl -o /dev/null -w "%{http_code}" http://localhost:65170/
200

$ curl -o /dev/null -w "%{http_code}" http://localhost:65186/
200

$ curl -s http://localhost:65186/ | head -c 40
<!DOCTYPE html>
<html>
  <head>
```

The environment of the Serverpod process showed the wiring:

```
SERVERPOD_DATABASE_HOST=localhost
SERVERPOD_DATABASE_PORT=65172
SERVERPOD_DATABASE_NAME=appdb
SERVERPOD_DATABASE_USER=postgres
SERVERPOD_REDIS_ENABLED=true
SERVERPOD_REDIS_HOST=localhost
SERVERPOD_REDIS_PORT=65166
SERVERPOD_API_SERVER_PORT=8080
SERVERPOD_INSIGHTS_SERVER_PORT=8081
SERVERPOD_WEB_SERVER_PORT=8082
```

### Step 5: worker polls

```
worker: polls http://localhost:65170 every 2 s (DEMO=null)
worker: poll 16 api status=200
worker: poll 17 api status=200
worker: poll 18 api status=200
worker: poll 19 api status=200
```

The environment of the worker process held the service discovery variables:

```
services__api__api__0=http://localhost:65170
services__api__web__0=http://localhost:65171
```

`services__api__insights__0` does not exist, because `AddServerpodApp` marks the Insights
endpoint with `ExcludeReferenceEndpoint`.

### Step 6: Jaspr live reload

The test changed the text of `jaspr_site/lib/pages/home.dart`. `jaspr serve` holds its own file
watcher, so `AddJasprApp` adds a `JasprSelfReloadsAnnotation` and Aspire keeps its own restart
off. The new text reached the browser five seconds later, with no change of the process.

### Step 7: Dart live reload

The test changed the log text in `worker/bin/main.dart`. `dart run` does not reload code, so
`AddDartApp` turns the Aspire restart on. Aspire replaced the process:

```
worker pid before: 47239
worker pid after:  53457
2026-08-26T15:48:50.486Z worker: polls http://localhost:65170 every 2 s (DEMO=null)
2026-08-26T15:48:52.543Z worker: restarted poll 1 api status=200
```

### Step 9: publish

```bash
dotnet run -- --publisher manifest --output-path ./out/aspire-manifest.json
```

The command exits 0 and writes:

```
out/aspire-manifest.json
out/site.Dockerfile
out/site.Dockerfile.dockerignore
out/worker.Dockerfile
out/worker.Dockerfile.dockerignore
```

The manifest holds three `container.v1` entries:

```
api    container.v1   dockerfile=../../serverpod_api/serverpod_api_server/Dockerfile
site   container.v1   dockerfile=site.Dockerfile
worker container.v1   dockerfile=worker.Dockerfile
```

`api` uses the Serverpod-authored Dockerfile, so Aspire writes no Dockerfile for it.
`site.Dockerfile` runs `jaspr build` on `dart:3.10.0` and serves `build/jaspr` from
`nginx:alpine`. `worker.Dockerfile` runs `dart compile exe` and copies the executable and the
Dart runtime into a `scratch` image.

The `out` directory is a run artifact. Delete it after the check.

## Known limits

- The first `site` start takes minutes. `jaspr serve` runs `build_runner`, which compiles the
  whole dependency tree. The resource reaches Running before the first page is ready, so a request
  can fail until the build ends.
- The Jaspr template pins a `build_web_compilers` release that needs Dart 3.13 or later. See the
  note above.
