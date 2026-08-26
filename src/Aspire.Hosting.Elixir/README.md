# Elixir app hosting integration

Use this integration to model, configure, and orchestrate Elixir applications in an Aspire solution.

## Getting started

### Prerequisites

**Elixir** (`elixir`) and the **Mix** build tool (`mix`) must be available on the PATH of the machine
that runs the AppHost. `aspire publish` also needs a container runtime.

`AddMixRelease` needs neither Elixir nor Mix, because the release carries its own runtime system.

### Add the integration

From your AppHost directory, add the `Aspire.Hosting.Elixir` integration with the Aspire CLI:

```bash
aspire add Aspire.Hosting.Elixir
```

## Usage example

In the AppHost, add an Elixir application resource:

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddElixirApp("api", "../elixir-api")
    .WithHttpEndpoint(port: 4000)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const api = await builder.addElixirApp("api", "../elixir-api")
    .withHttpEndpoint({ port: 4000 })
    .withExternalHttpEndpoints();

await builder.build().run();
```

The method runs the application as `mix run --no-halt` from the directory that contains `mix.exs`.
Pass extra application arguments with `.WithAppArgs(...)`. Mix passes everything after the `--`
separator to the application:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithAppArgs("--port", "4000");
// mix run --no-halt -- --port 4000
```

`.WithMixTask("phx.server")` replaces `run --no-halt` with a different task. Arguments from
`.WithAppArgs(...)` stay after the `--` separator.

## Run mode

### Mix setup steps

Aspire adds a sibling resource for each Mix setup step. Every step runs in run mode only and stays
out of the manifest. Each step uses the Mix environment and the working directory of the application.

| Method | Sibling resource | Command |
| --- | --- | --- |
| `WithMixDeps()` | `{app}-mix-deps` | `mix deps.get` |
| `WithMixCompile()` | `{app}-mix-compile` | `mix compile` |
| `WithEctoMigrate()` | `{app}-ecto-migrate` | `mix ecto.migrate` |

`AddElixirApp` and `AddPhoenixApp` call `WithMixDeps` automatically when the application directory
holds `mix.exs`. The steps run in order: `deps.get`, then `compile`, then `ecto.migrate`. The
application waits for the last step.

Call `.WithMixDeps(install: false)` to keep the step but start it by hand from the dashboard. A later
`.WithMixDeps()` turns the automatic run back on.

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithMixDeps()
    .WithMixCompile();
```

### The Mix environment

The resource sets `MIX_ENV` to `dev` in run mode and to `prod` in publish mode. `.WithMixEnv("test")`
replaces that value. The setup steps and the generated Dockerfile use the same value, so the build
and the start always agree:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithMixEnv("test");
```

### Erlang virtual machine flags

`.WithErlFlags("+S 4:4")` sets `ERL_FLAGS`. `.WithElixirErlOptions("+K true")` sets
`ELIXIR_ERL_OPTIONS`, which the `elixir` command puts on the `erl` command line:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithErlFlags("+S 4:4")
    .WithElixirErlOptions("+K true");
```

### Live reload

`mix run` does not reload code, so a change to a source file has no effect until the application
restarts. `AddElixirApp` therefore adds live reload by default in run mode. Aspire looks at the `lib`
and `config` directories of the application, accepts the `.ex`, `.exs`, `.heex`, and `.eex`
extensions, and ignores `_build`, `deps`, and `.elixir_ls`. A compiler writes many files at one time,
so Aspire waits 500 milliseconds after the last change and then restarts the resource one time. The
resource log shows the reason:

```text
Restarting api: /work/elixir-api/lib/api.ex changed
```

`AddPhoenixApp` does not add live reload, because the Phoenix code reloader does the same work
without a restart. Add or remove the restart with `.WithLiveReload()` and `.WithLiveReload(false)`.

Live reload applies in run mode only. A published image holds a Mix release and no source.

### Debugging

`AddElixirApp` and `AddPhoenixApp` both let an IDE start the resource under a debugger in run mode,
so no extra call is necessary.

Aspire sends the IDE a launch configuration of the type `elixir`. The fields follow the ElixirLS
`mix_task` debug adapter: the project directory, the Mix task, the task arguments, the Mix
environment, and the working directory. That adapter runs `mix <task> <taskArgs>` itself, so Aspire
does not give it the `mix` command.

Install [ElixirLS](https://marketplace.visualstudio.com/items?itemName=JakeBecker.elixir-ls) in VS
Code before you start a debug session. An IDE that cannot start an `elixir` launch configuration
makes Aspire start the resource as a plain process instead, so the application still runs. The
command line of the resource does not change in a debug session.

## Phoenix

`AddPhoenixApp` runs the application as `mix phx.server` and adds one HTTP endpoint:

**C#**

```csharp
var web = builder.AddPhoenixApp("web", "../phoenix-web")
    .WithExternalHttpEndpoints();
```

**TypeScript**

```typescript
const web = await builder.addPhoenixApp("web", "../phoenix-web")
    .withExternalHttpEndpoints();
```

The resource sets these environment variables:

| Variable | Value |
| --- | --- |
| `PORT` | The target port of the HTTP endpoint |
| `PHX_SERVER` | `true`, which tells a Mix release to start the endpoint |
| `PHX_HOST` | The host of the HTTP endpoint |
| `SECRET_KEY_BASE` | A generated secret parameter, in publish mode only |

The secret parameter has the name `{app}-secret-key-base` and 64 characters, which is the minimum
length that Phoenix accepts. In run mode the method does not set `SECRET_KEY_BASE`, because
`config/dev.exs` holds the development value.

Read the values in `config/runtime.exs`:

```elixir
config :my_app, MyAppWeb.Endpoint,
  url: [host: System.get_env("PHX_HOST") || "localhost", port: 443, scheme: "https"],
  http: [ip: {0, 0, 0, 0}, port: String.to_integer(System.get_env("PORT") || "4000")],
  secret_key_base: System.get_env("SECRET_KEY_BASE")
```

`AddPhoenixApp` accepts every method that `AddElixirApp` accepts. Use the framework method
`.WithHttpHealthCheck("/health")` to add an HTTP health check. The application must serve the path.

## Databases and Ecto

`.WithEctoDatabase(db)` sets `DATABASE_URL` from a database resource. The method also adds a
reference to the database and makes the application wait for it:

**C#**

```csharp
var db = builder.AddPostgres("pg").AddDatabase("appdb");

builder.AddElixirApp("api", "../elixir-api")
    .WithEctoDatabase(db)
    .WithEctoMigrate();
```

**TypeScript**

```typescript
const db = await builder.addPostgres("pg").addDatabase("appdb");

const api = await builder.addElixirApp("api", "../elixir-api");
await api.withEctoDatabase(db);
await api.withEctoMigrate();
```

Ecto accepts the `ecto://`, `postgres://`, and `postgresql://` URI forms. The method uses the `Uri`
connection property of the database when the database has one. PostgreSQL, for example, gives
`postgresql://user:password@host:port/database`. If the database has no `Uri` property, the method
uses the connection string of the database. In that case, confirm that Ecto accepts the format.

Read the value in `config/runtime.exs`:

```elixir
config :my_app, MyApp.Repo, url: System.get_env("DATABASE_URL")
```

`.WithReference(resource)` works for every other resource. Aspire writes `ConnectionStrings__{name}`
for a connection-string resource and `services__{name}__{binding}__0` for an endpoint.

A Mix release has no Mix tasks, so publish output cannot run `mix ecto.migrate`. Add a release module
and run it from the release script instead:

```elixir
defmodule MyApp.Release do
  @app :my_app

  def migrate do
    Application.load(@app)

    for repo <- Application.fetch_env!(@app, :ecto_repos) do
      {:ok, _, _} = Ecto.Migrator.with_repo(repo, &Ecto.Migrator.run(&1, :up, all: true))
    end
  end
end
```

```bash
bin/my_app eval "MyApp.Release.migrate"
```

## Distributed Erlang

`.WithNodeName("api")` starts the application as a named node, so other nodes can connect to it. The
method adds `-sname api` and `-setcookie <value>` to `ELIXIR_ERL_OPTIONS`, after any options from
`.WithElixirErlOptions(...)`. It also sets `RELEASE_NODE` and `RELEASE_COOKIE`, which a Mix release
reads. Without a cookie parameter the method creates a secret parameter named `{app}-cookie`:

**C#**

```csharp
var cookie = builder.AddParameter("cookie", secret: true);

builder.AddElixirApp("api", "../elixir-api")
    .WithNodeName("api", cookie);
```

**TypeScript**

```typescript
const cookie = await builder.addParameter("cookie", { secret: true });

const api = await builder.addElixirApp("api", "../elixir-api");
await api.withNodeName("api", cookie);
```

`mix run` reads the cookie from the command line only, so the value appears in the process list of
the machine while the application runs. A Mix release reads `RELEASE_COOKIE` from the environment
instead, so `AddMixRelease` does not expose the value that way. `WithNodeName` is for `AddElixirApp`
and `AddPhoenixApp` only.

## Publish mode

`aspire publish` turns an Elixir or Phoenix resource into a container image. Aspire writes a
Dockerfile with two stages. The build stage compiles the project and runs `mix release`. The runtime
stage carries only the release directory, so the image holds no compiler, no source, and no Hex
cache. The image runs as the user `app`, which is not root.

```dockerfile
FROM docker.io/library/elixir:1.19.5-otp-28-slim AS build
RUN apt-get update -y && apt-get install -y build-essential git ca-certificates && apt-get clean && rm -f /var/lib/apt/lists/*_*
WORKDIR /app
RUN mix local.hex --force && mix local.rebar --force
ENV MIX_ENV=prod
COPY mix.exs ./
COPY mix.lock ./
COPY config config
RUN mix deps.get --only 'prod' && mix deps.compile
COPY . .
RUN mix compile
RUN mix release 'my_app'

FROM docker.io/library/erlang:28-slim
RUN apt-get update -y && apt-get install -y ca-certificates locales && apt-get clean && rm -f /var/lib/apt/lists/*_*
RUN sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen && locale-gen
ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8
RUN groupadd --system --gid 999 app && useradd --system --gid 999 --uid 999 --no-create-home app
WORKDIR /app
COPY --from=build --chown=app:app /app/_build/prod/rel/my_app ./
ENV MIX_ENV=prod
USER app
ENTRYPOINT ["/app/bin/my_app"]
CMD ["start"]
```

Aspire also writes `<name>.Dockerfile.dockerignore`. The rules keep `_build`, `deps`, `.elixir_ls`,
`node_modules`, and `.git` out of the build context. A `.dockerignore` in the application directory
replaces these rules.

Aspire writes no Dockerfile when the application directory holds one. That `Dockerfile` stays the
contract of the repository, and `aspire publish` builds the image from it.

### Base images

The build image comes from the Elixir and OTP versions that Aspire detects. Aspire reads
`.tool-versions` first, then the `elixir:` requirement in `mix.exs`. Without those files it uses
Elixir 1.19.5 and OTP 28.4.1.

| Stage | Default image |
| --- | --- |
| Build | `docker.io/library/elixir:<elixir>-otp-<otp-major>-slim` |
| Runtime | `docker.io/library/erlang:<otp-major>-slim` |

The official `elixir` image is the only published Elixir image with a stable tag. Every
`hexpm/elixir` tag carries a Debian snapshot date and an exact OTP patch version from the Hex build
matrix. Neither value can be read from `.tool-versions` or `mix.exs`, so a tag built from the
detected versions would not exist.

The official `elixir` image is built from `erlang:<otp-major>-slim`, so the two stages share a base.
That match matters: a Mix release carries the Erlang runtime system that the build stage compiled,
and that runtime system must find the same C library at run time.

Use `WithDockerfileBaseImage` to select different images. Aspire recognizes an Alpine image and uses
`apk` in place of `apt-get`:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithDockerfileBaseImage(
        buildImage: "hexpm/elixir:1.18.4-erlang-27.3.4-debian-bookworm-20250908-slim",
        runtimeImage: "debian:bookworm-slim");
```

Give both images the same operating system. A release that a Debian build stage produced does not
start on Alpine, and the reverse is also true.

### The release name

`mix release` needs a release name, and the name selects the launcher script. Aspire reads the name
in this order:

1. The name that `.WithReleaseName("api_release")` gives.
2. The `app:` key in `mix.exs`, for example `app: :my_app`.
3. The resource name with each hyphen replaced by an underscore, because a release name is an Erlang
   atom.

Use `WithReleaseName` when `mix.exs` declares more than one release. The method changes publish
output only.

### Phoenix assets

A Phoenix application that has an `assets` directory gets one more build step. Aspire adds
`RUN mix assets.deploy` after `mix compile` and before `mix release`. Add the alias to `mix.exs`:

```elixir
defp aliases do
  [
    "assets.deploy": ["tailwind my_app --minify", "esbuild my_app --minify", "phx.digest"]
  ]
end
```

If `assets/package.json` exists, Aspire also installs Node.js and npm in the build stage and runs
`npm ci --prefix assets` before `mix assets.deploy`. Commit `assets/package-lock.json`, because
`npm ci` reads it.

The runtime stage of a Phoenix image sets `PHX_SERVER=true`. The `PORT`, `PHX_HOST`, and
`SECRET_KEY_BASE` values arrive as environment variables at run time, so the secret never enters an
image layer.

## Prebuilt releases

`AddMixRelease` adds a Mix release that a different build step already produced. The resource starts
the release with `bin/<releaseName> start`, and on Windows with `bin\<releaseName>.bat`. A release
carries its compiled dependencies, so the method adds no Mix setup steps:

**C#**

```csharp
builder.AddMixRelease("api", "../elixir-api/_build/prod/rel/my_app")
    .WithHttpEndpoint(env: "PORT");
```

**TypeScript**

```typescript
const api = await builder.addMixRelease("api", "../elixir-api/_build/prod/rel/my_app")
    .withHttpEndpoint({ env: "PORT" });
```

The default release name is the name of the release directory. Pass a third argument when the two are
different. In publish mode the resource becomes a container image with one stage that copies the
release directory.

Set `RELEASE_NODE` and `RELEASE_COOKIE` with `WithEnvironment` when the release must join a cluster.

## OpenTelemetry

Every Elixir, Phoenix, and Mix release resource exports telemetry to the Aspire dashboard. Aspire
sets `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`, and
`OTEL_RESOURCE_ATTRIBUTES`. The Mix setup steps get no telemetry variables, because they produce no
telemetry.

Add the Hex dependencies to `mix.exs`:

```elixir
defp deps do
  [
    {:opentelemetry, "~> 1.5"},
    {:opentelemetry_api, "~> 1.4"},
    {:opentelemetry_exporter, "~> 1.8"},
    {:opentelemetry_phoenix, "~> 2.0"},
    {:opentelemetry_ecto, "~> 1.2"},
    {:opentelemetry_bandit, "~> 0.2"}
  ]
end
```

Select the OTLP exporter in `config/runtime.exs`:

```elixir
config :opentelemetry, traces_exporter: :otlp
```

The exporter reads `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`,
`OTEL_EXPORTER_OTLP_HEADERS`, and `OTEL_EXPORTER_OTLP_CERTIFICATE` from the environment, so no
endpoint belongs in the configuration file.

Attach the instrumentation when the application starts, in `application.ex`:

```elixir
OpentelemetryBandit.setup()
OpentelemetryPhoenix.setup(adapter: :bandit)
OpentelemetryEcto.setup([:my_app, :repo])
```

## Known limits

**No metrics and no OTLP logs.** The Erlang OpenTelemetry metrics and logs OTLP exporters have not
shipped. The dashboard shows traces and console logs from an Elixir resource. Metrics do not reach
the dashboard.

**Certificate trust replaces the trust set.** The resource sets `OTEL_EXPORTER_OTLP_CERTIFICATE` and
`SSL_CERT_FILE` to the Aspire certificate bundle, and the default certificate trust scope is
`System`. The Erlang `:ssl` application replaces its complete trust set with the file that
`SSL_CERT_FILE` names, and it cannot add to that set. The `System` scope therefore makes Aspire put
the system authorities and the custom authorities in one bundle. With the `Append` scope the bundle
holds the custom authorities only, so the resource sets `OTEL_EXPORTER_OTLP_CERTIFICATE` but not
`SSL_CERT_FILE`. Aspire applies custom certificate trust in run mode only.

**Erlang `:ssl` refuses a self-signed certificate.** Aspire signs the certificate of a local resource
with itself. The Erlang `:ssl` application refuses a self-signed leaf certificate, even when the
trust bundle holds the same certificate. A client such as Redix must read `SSL_CERT_FILE` itself and
supply a `verify_fun` that accepts a certificate that the bundle holds. The
[playground worker](https://github.com/microsoft/aspire/blob/main/playground/ElixirApps/worker/lib/worker.ex)
shows the pattern.

**`mix run` shows the Erlang cookie in the process list.** See
[Distributed Erlang](#distributed-erlang).

## Additional documentation

- https://aspire.dev/integrations/gallery/
- [Aspire documentation](https://aspire.dev/)
- [Elixir installation](https://elixir-lang.org/install.html)
- [Mix releases](https://hexdocs.pm/mix/Mix.Tasks.Release.html)
- [OpenTelemetry Erlang and Elixir](https://opentelemetry.io/docs/languages/erlang/)

## Feedback & contributing

https://github.com/microsoft/aspire
