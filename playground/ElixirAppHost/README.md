# ElixirAppHost playground

This playground runs an Elixir AppHost. The AppHost model lives in `apphost.exs`. The
Aspire CLI generates an Elixir SDK into `.aspire/modules/` and then runs the script with
`elixir`.

The applications come from [`../ElixirApps`](../ElixirApps). That directory also holds a C#
AppHost with the same model, so you can compare the two languages side by side.

## Model

```elixir
appdb =
  builder
  |> Builder.add_postgres!("db")
  |> PostgresServerResource.add_database!("appdb")

cache = Builder.add_redis!(builder, "cache")

builder
|> Builder.add_phoenix_app!("web", "../ElixirApps/phoenix_web")
|> tap(&PhoenixAppResource.with_ecto_database!(&1, appdb))
|> tap(&PhoenixAppResource.with_ecto_migrate!/1)
|> tap(&PhoenixAppResource.with_external_http_endpoints!/1)

builder
|> Builder.add_elixir_app!("worker", "../ElixirApps/worker")
|> tap(&ElixirAppResource.with_reference!(&1, cache))
|> tap(&ElixirAppResource.wait_for!(&1, cache))
```

`tap/2` is necessary today. Read "Known limits" below.

## Layout

| Path | Holds |
| --- | --- |
| `apphost.exs` | The AppHost model |
| `aspire.config.json` | The language, the feature flags, and the integration packages |
| `apphost.run.json` | The dashboard and OTLP endpoints |
| `.aspire/modules/` | The generated SDK. The CLI writes it. Do not edit it. |

## Prerequisites

- Elixir 1.19 and Erlang/OTP 28 on the PATH.
- A container runtime for the PostgreSQL and Redis containers.
- Network access on the first run, because `mix deps.get` reads from Hex.

## How to run

Elixir AppHost support is experimental. `aspire.config.json` turns the feature on for this
directory. To turn it on for every AppHost, run:

```bash
aspire config set features:experimentalPolyglot:elixir true --global
```

Then run the AppHost:

```bash
cd playground/ElixirAppHost
aspire run
```

To run the CLI from this repository instead of an installed CLI, follow
["Local Development Workflow"](../../docs/specs/polyglot-apphost.md#local-development-workflow):

```bash
export ASPIRE_REPO_ROOT="/path/to/aspire"
export PATH="$ASPIRE_REPO_ROOT/.dotnet:$PATH"
dotnet run --project "$ASPIRE_REPO_ROOT/src/Aspire.Cli/Aspire.Cli.csproj" -- run
```

`ASPIRE_REPO_ROOT` puts the CLI in development mode. The CLI then resolves the Aspire
packages through project references and regenerates the SDK on every run.

Stop the AppHost with `CTRL+C`, or with `aspire stop` from a second terminal.

## Watch mode

`aspire.config.json` sets `defaultWatchEnabled`. The CLI therefore starts the AppHost through
`.aspire/modules/watch.exs`. The script polls the modification time of every `*.ex` and
`*.exs` file below this directory. On a change it stops the AppHost and starts it again.

## Publish

```bash
aspire publish -o ./out
```

The model adds a Docker Compose compute environment, so `aspire publish` writes
`out/docker-compose.yaml` and one Dockerfile for each Elixir application.

## E2E checklist

The results come from a run on 2026-08-25 on macOS 26 (Apple Silicon), with Elixir 1.19.5 and
Erlang/OTP 28. The CLI came from `artifacts/bin/Aspire.Cli/Debug/net10.0/aspire` on branch
`feature/elixir-integration`, with `ASPIRE_REPO_ROOT` set.

| Step | Result | Evidence |
| --- | --- | --- |
| 1. Generate the SDK | Pass | `Generated 37 Elixir files in .../ElixirAppHost/.aspire/modules (37 changed)` |
| 2. Resources start | Pass | See the state table below. |
| 3. HTTP endpoints answer | Pass | See the curl output below. |
| 4. Worker reads Redis | Pass | See the worker log lines below. |
| 5. Telemetry export | Pass | No OTLP error appears in the `web` or `worker` logs. |
| 6. Watch restart | Pass | See the watch section below. |
| 7. Stop | Pass | `aspire stop` reported success. No `beam.smp` process of this playground remained. |
| 8. Publish | Pass | See the publish section below. |

### Step 2: resource states

`aspire describe --format json` returned:

```
appdb                      Running    exit=None
cache-bngzcdhd             Running    exit=None    tcp://localhost:64486, rediss://localhost:64485
db-usptvgdv                Running    exit=None    tcp://localhost:64487
web-ecto-migrate-nqxagbzn  Finished   exit=0
web-mix-deps-smgtdnge      Finished   exit=0
web-thbguckw               Running    exit=None    http://localhost:64488
worker-mix-deps-pauwkveq   Finished   exit=0
worker-yqnmcckt            Running    exit=None
```

`appdb` is a logical child of `db`. The migration step proves that the database exists.
`web-ecto-migrate` comes from `with_ecto_migrate!`. The `*-mix-deps` steps come from the
Elixir integration.

### Step 3: HTTP endpoints

Aspire allocated port 64488 for `web` in this run.

```
$ curl http://localhost:64488/api/hello
{"message":"hello from phoenix","version":1,"greetings":1}

$ curl -o /dev/null -w "%{http_code}" http://localhost:64488/health
200
```

### Step 4: worker counter

```
[worker] [info] worker counter worker:counter=38
[worker] [info] worker counter worker:counter=39
[worker] [info] worker counter worker:counter=40
[worker] [info] worker counter worker:counter=41
```

### Step 6: watch restart

The test added one line to the worker chain in `apphost.exs`:

```elixir
|> tap(&ElixirAppResource.with_environment!(&1, "DEMO", "1"))
```

The CLI log recorded the restart:

```
[2026-08-25 14:17:40.122] [FAIL] [AppHost] [aspire-watch] restarting: apphost.exs
```

The watcher process kept its PID. The AppHost process changed from 75854 to 87977. After the
restart, `aspire describe` showed the new environment variable:

```
worker-faygmybb Running DEMO= 1
```

The `[FAIL]` label is the CLI log category for the standard error stream of the AppHost. The
watcher writes its messages to standard error. The restart succeeded.

### Step 8: publish

```
$ aspire publish -o ./out
✅ 7/7 steps succeeded

$ find out -type f
out/.env
out/docker-compose.yaml
out/web.Dockerfile
out/web.Dockerfile.dockerignore
out/worker.Dockerfile
out/worker.Dockerfile.dockerignore
```

`out/web.Dockerfile` builds a release with `mix release 'phoenix_web'` on
`elixir:1.19.5-otp-28-slim`, and runs it on `erlang:28-slim` as a user that is not root. It
sets `PHX_SERVER=true`. `out/worker.Dockerfile` has the same shape and runs
`mix release 'worker'`. `out/docker-compose.yaml` holds the services `db`, `cache`, `web`,
and `worker`.

## Known limits

### A fluent function returns the base handle

Every fluent function in the generated SDK returns the **declared** handle type, not the
resource type of the receiver. Example:

```elixir
@spec with_ecto_database!(t(), ...) :: Aspire.Elixir.ElixirAppResource.t()
@spec with_reference!(t(), term(), keyword()) :: Aspire.ResourceWithEnvironment.t()
@spec wait_for!(t(), Aspire.Resource.t(), keyword()) :: Aspire.ResourceWithWaitSupport.t()
```

`Aspire.ResourceWithEnvironment`, `Aspire.ResourceWithEndpoints`, and
`Aspire.ResourceWithWaitSupport` are structs with no functions. A plain pipe therefore stops
after one step, and a second step raises `FunctionClauseError`.

The handle identity does not change, so `tap/2` is a correct workaround. It calls the function
and keeps the original struct in the pipe.

The TypeScript, Python, and Java generators do not have this problem. They read
`AtsCapabilityInfo.ReturnsBuilder` and return the receiver type. `AtsElixirCodeGenerator`
does not read that property. See NAK-516 for the report.

### Erlang TLS and the development certificate

The Erlang `:ssl` application refuses the self-signed Aspire development certificate. The
OTLP exporter then cannot reach the dashboard over HTTPS. `apphost.run.json` therefore
declares an `http` profile with `ASPIRE_ALLOW_UNSECURED_TRANSPORT`. The `ElixirApps` C#
AppHost uses the same profile for the same reason.

A guest AppHost cannot select a launch profile. `GuestAppHostProject.SupportsLaunchProfiles`
is `false`. The CLI reads the `https` profile when one exists, and the first profile
otherwise. This directory therefore declares one profile only.

### Windows named pipes

The Elixir transport uses a local `gen_tcp` socket. Windows named pipes are not supported
yet. See NAK-519.
