# ElixirApps playground

This playground runs a Phoenix web application and a plain Elixir OTP application under Aspire.
It exercises `AddPhoenixApp`, `AddElixirApp`, `WithEctoDatabase`, `WithEctoMigrate`, and
`WithReference`.

The AppHost model is:

```csharp
var db = builder.AddPostgres("pg").AddDatabase("appdb");
var cache = builder.AddRedis("cache");

builder.AddPhoenixApp("web", "../phoenix_web")
       .WithEctoDatabase(db)
       .WithEctoMigrate()
       .WithExternalHttpEndpoints();

builder.AddElixirApp("worker", "../worker")
       .WithReference(cache)
       .WaitFor(cache);
```

## Layout

| Path | Holds |
| --- | --- |
| `ElixirApps.AppHost/` | The C# AppHost |
| `phoenix_web/` | A Phoenix 1.8 JSON API with an Ecto repository |
| `worker/` | A plain Mix OTP application that increments a Redis key |

## Prerequisites

- Elixir 1.19.5 and Erlang/OTP 28 on the PATH. Both applications hold a `.tool-versions` file.
- A container runtime for the PostgreSQL and Redis containers.
- Network access on the first run, because `mix deps.get` reads from Hex.

## How to run

```bash
cd ElixirApps.AppHost
dotnet run
```

The log prints the dashboard URL and the login token. Open that URL.

## What to look for in the dashboard

- Resources `pg`, `appdb`, `cache`, `web`, and `worker` reach the Running state.
- The setup siblings `web-mix-deps`, `worker-mix-deps`, and `web-ecto-migrate` reach Finished
  with exit code 0. `web-mix-compile` does not exist, because the AppHost does not call
  `WithMixCompile`.
- The `web` endpoint answers `GET /api/hello` with JSON and `GET /health` with status 200.
- The console log of `worker` shows one counter line every two seconds.
- The Traces page shows `GET /api/hello` spans from `web`, nested
  `phoenix_web.repo.query:greetings` spans from Ecto, and `worker.tick` spans from `worker`.

## Notes on the Elixir code

- `config/runtime.exs` in `phoenix_web` reads `DATABASE_URL` in every environment. A generated
  Phoenix application reads it in `:prod` only.
- `config/dev.exs` reads the HTTP port from `PORT`, which Aspire allocates.
- The controller counts the rows of the `greetings` table, so each request uses the database.
- The migration creates the table and inserts one row.
- `worker` parses `ConnectionStrings__cache`. Aspire gives the value
  `host:port,password=<value>,ssl=true`.

## E2E checklist

The results come from a run on 2026-08-25 on macOS 15 (Apple Silicon), with Elixir 1.19.5 and
Erlang/OTP 28.

| Step | Result | Evidence |
| --- | --- | --- |
| 1. Build the AppHost | Pass | `dotnet build` printed `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| 2. Resources start | Pass | See the state table below. |
| 3. HTTP endpoints answer | Pass | See the curl output below. |
| 4. Live reload | Pass | The controller changed at 07:48:54. The endpoint answered `"version":2` at 07:48:59, with no restart. |
| 5. Worker counter | Pass | See the worker log lines below. |
| 6. Traces arrive | Pass | The dashboard telemetry API returned spans from both applications. |
| 7. Stop the AppHost | Pass | After the stop, no `beam.smp` process of this playground remained. The containers stopped. |
| 8. Publish smoke | Pass | `dotnet run -- --publisher manifest --output-path ./out/aspire-manifest.json` exits 0 with `web.Dockerfile`, `worker.Dockerfile`, their `.dockerignore` files, and two `container.v1` entries in the manifest. Build stage `docker.io/library/elixir:1.19.5-otp-28-slim`, runtime `docker.io/library/erlang:28-slim`. Verified on commit `6669aac12`. |

### Step 2: resource states

The states come from the DCP API server of the run:

```
aspire-dashboard-uyaxnkpf | Running
worker-nybjqjps           | Running
web-ztbdfxgj              | Running
web-ecto-migrate-fbpdcxve | Finished | exit=0
worker-mix-deps-ngtacwun  | Finished | exit=0
web-mix-deps-pnbxbstq     | Finished | exit=0
pg-xejpkeer               | Running
cache-kabrmnkx            | Running
```

`appdb` is a logical child of `pg`. The PostgreSQL integration creates the database. The
migration step proves that the database exists.

### Step 3: HTTP endpoints

Aspire allocated port 57687 for `web` in this run.

```
$ curl http://localhost:57687/api/hello
{"message":"hello from phoenix","version":1,"greetings":1}

$ curl -o /dev/null -w "%{http_code}" http://localhost:57687/health
200
```

### Step 5: worker counter

```
[info] worker counter worker:counter=6
[info] worker counter worker:counter=7
[info] worker counter worker:counter=8
[info] worker counter worker:counter=9
[info] worker counter worker:counter=10
[info] worker counter worker:counter=11
```

### Step 6: traces

`GET /api/telemetry/spans` on the dashboard returned these spans:

```
('web',    'opentelemetry_bandit', 'GET /api/hello')                     -> 3
('web',    'opentelemetry_bandit', 'GET /health')                        -> 1
('web',    'opentelemetry_ecto',   'phoenix_web.repo.query')             -> 4
('web',    'opentelemetry_ecto',   'phoenix_web.repo.query:greetings')   -> 3
('web',    'opentelemetry_ecto',   'phoenix_web.repo.query:schema_migrations') -> 4
('worker', 'worker',               'worker.tick')                        -> 22
```

One `worker.tick` span:

```json
{"name": "worker.tick", "kind": 1, "attributes": [{"key": "worker.counter", "value": {"stringValue": "1"}}]}
```

The API needs the key from the `DASHBOARD__API__PRIMARYAPIKEY` environment variable of the
dashboard process, in the `X-API-Key` header.

### Step 8: publish

Publish support landed in commit `6669aac12` (NAK-498). The publish smoke step above records the verified result.

```bash
dotnet run -- --publisher manifest --output-path ./out/aspire-manifest.json
```

and confirm that `out/web.Dockerfile` and `out/worker.Dockerfile` exist.

## Known limits

- The Erlang `:ssl` application refuses a self-signed leaf certificate, even when the trust
  bundle holds the same certificate. Aspire signs the Redis TLS certificate this way. The worker
  therefore gives Redix a `verify_fun` that accepts a self-signed peer when the bundle at
  `SSL_CERT_FILE` holds the same bytes. See `worker/lib/worker.ex`.
- The OpenTelemetry Bandit instrumentation is the Elixir module `OpentelemetryBandit`. The call
  `:opentelemetry_bandit.setup()` does not compile to a real function.
- The Erlang OpenTelemetry metrics and logs OTLP exporters have not shipped. The dashboard shows
  traces and console logs only.
