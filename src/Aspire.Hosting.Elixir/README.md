# Elixir app hosting integration

Use this integration to model, configure, and orchestrate Elixir applications in an Aspire solution.

## Getting started

### Prerequisites

**Elixir** (`elixir`) and the **Mix** build tool (`mix`) must be available on the PATH of the machine that runs the AppHost.

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
The resource sets `MIX_ENV` to `dev` in run mode and to `prod` in publish mode.

Pass extra application arguments with `.WithAppArgs(...)`. The arguments go after the `--` separator:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithAppArgs("--port", "4000");
// mix run --no-halt -- --port 4000
```

### Fetch dependencies with `WithMixDeps`

`.WithMixDeps()` adds a setup resource named `{app}-mix-deps` that runs `mix deps.get`.
The application waits for the step to complete. The step runs only in run mode and stays out of the manifest.

`AddElixirApp` calls this method automatically when the application directory contains `mix.exs`.
Call `.WithMixDeps(install: false)` to keep the step but start it by hand from the dashboard:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithMixDeps(install: false);
```

### Compile with `WithMixCompile`

`.WithMixCompile()` adds a setup resource named `{app}-mix-compile` that runs `mix compile`.
The compile step waits for the dependency step when both exist, and the application waits for the compile step:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithMixDeps()
    .WithMixCompile();
```

### Select the Mix environment with `WithMixEnv`

`.WithMixEnv("test")` sets `MIX_ENV`. The value replaces the default, which is `dev` in run mode and `prod` in publish mode:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithMixEnv("test");
```

### Run a different task with `WithMixTask`

`.WithMixTask("phx.server")` replaces the default `run --no-halt` arguments. Arguments from `.WithAppArgs(...)` stay after the `--` separator:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithMixTask("phx.server");
// mix phx.server
```

### Set Erlang virtual machine flags with `WithErlFlags` and `WithElixirErlOptions`

`.WithErlFlags("+S 4:4")` sets `ERL_FLAGS`. `.WithElixirErlOptions("+K true")` sets `ELIXIR_ERL_OPTIONS`:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithErlFlags("+S 4:4")
    .WithElixirErlOptions("+K true");
```

### Name the node with `WithNodeName`

`.WithNodeName("api")` adds `--sname api` and `--cookie <value>` to `ELIXIR_ERL_OPTIONS`, after any options from `.WithElixirErlOptions(...)`.
The method also sets `RELEASE_NODE` and `RELEASE_COOKIE`, which a Mix release reads.
When you give no cookie, the method creates a secret parameter with the name `{app}-cookie`:

```csharp
var cookie = builder.AddParameter("cookie", secret: true);

builder.AddElixirApp("api", "../elixir-api")
    .WithNodeName("api", cookie);
```

## Phoenix

`AddPhoenixApp` adds a Phoenix web application. The method runs the application as `mix phx.server`
and adds one HTTP endpoint:

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddPhoenixApp("web", "../phoenix-web")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const web = await builder.addPhoenixApp("web", "../phoenix-web")
    .withExternalHttpEndpoints();

await builder.build().run();
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

Configure the endpoint in `config/runtime.exs` to read the values:

```elixir
config :my_app, MyAppWeb.Endpoint,
  url: [host: System.get_env("PHX_HOST") || "localhost", port: 443, scheme: "https"],
  http: [ip: {0, 0, 0, 0}, port: String.to_integer(System.get_env("PORT") || "4000")],
  secret_key_base: System.get_env("SECRET_KEY_BASE")
```

`AddPhoenixApp` accepts every method that `AddElixirApp` accepts. `.WithMixTask(...)` replaces
`phx.server` with a different task.

## Databases and Ecto

`.WithEctoDatabase(db)` sets `DATABASE_URL` from a database resource. The method also adds a
reference to the database and makes the application wait for it:

```csharp
var db = builder.AddPostgres("pg").AddDatabase("appdb");

builder.AddElixirApp("api", "../elixir-api")
    .WithEctoDatabase(db);
```

Ecto accepts the `ecto://`, `postgres://`, and `postgresql://` URI forms. The method uses the `Uri`
connection property of the database when the database has one. PostgreSQL, for example, gives
`postgresql://user:password@host:port/database`. If the database has no `Uri` property, the method
uses the connection string of the database. In that case, confirm that Ecto accepts the format.

Read the value in `config/runtime.exs`:

```elixir
config :my_app, MyApp.Repo, url: System.get_env("DATABASE_URL")
```

`.WithReference(resource)` works for every other resource. Aspire writes
`ConnectionStrings__{name}` for a connection-string resource and `services__{name}__{binding}__0`
for an endpoint.

### Migrate the database with `WithEctoMigrate`

`.WithEctoMigrate()` adds a setup resource named `{app}-ecto-migrate` that runs `mix ecto.migrate`.
The step gets the same `DATABASE_URL` value as the application. It waits for the database and for
the Mix setup steps. The application waits for the migration to complete:

```csharp
var db = builder.AddPostgres("pg").AddDatabase("appdb");

builder.AddElixirApp("api", "../elixir-api")
    .WithEctoDatabase(db)
    .WithEctoMigrate();
```

The step runs only in run mode and stays out of the manifest. A Mix release has no Mix tasks, so
publish output cannot run `mix ecto.migrate`. Add a release module and run it from the release
script instead:

```elixir
defmodule MyApp.Release do
  @app :my_app

  def migrate do
    load_app()

    for repo <- Application.fetch_env!(@app, :ecto_repos) do
      {:ok, _, _} = Ecto.Migrator.with_repo(repo, &Ecto.Migrator.run(&1, :up, all: true))
    end
  end

  defp load_app, do: Application.load(@app)
end
```

```bash
bin/my_app eval "MyApp.Release.migrate"
```

## Publishing

`aspire publish` turns an Elixir or Phoenix resource into a container image. Aspire writes a
Dockerfile with two stages. The build stage compiles the project and runs `mix release`. The runtime
stage carries only the release directory, so the image holds no compiler, no source, and no Hex
cache. The image runs as the user `app`, which is not root.

The generated Dockerfile has this shape:

```dockerfile
FROM docker.io/library/elixir:1.19.5-otp-28-slim AS build
RUN apt-get update -y && apt-get install -y build-essential git ca-certificates && apt-get clean && rm -f /var/lib/apt/lists/*_*
WORKDIR /app
RUN mix local.hex --force && mix local.rebar --force
ENV MIX_ENV=prod
COPY mix.exs ./
COPY mix.lock ./
COPY config config
RUN mix deps.get --only prod && mix deps.compile
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
CMD ["/app/bin/my_app", "start"]
```

Aspire also writes `<name>.Dockerfile.dockerignore`. The rules keep `_build`, `deps`, `.elixir_ls`,
`node_modules`, and `.git` out of the build context. A `.dockerignore` in the application directory
replaces these rules.

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

The runtime image is the exact base of the build image, because the official `elixir` image is built
from `erlang:<otp-major>-slim`. That match matters: a Mix release carries the Erlang runtime system
that the build stage compiled, and that runtime system must find the same C library at run time. The
image also already holds OpenSSL, ncurses, and the C++ runtime.

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

1. The name that `WithReleaseName` gives.
2. The `app:` key in `mix.exs`, for example `app: :my_app`.
3. The resource name with each hyphen replaced by an underscore, because a release name is an Erlang
   atom.

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithReleaseName("api_release");
```

### Phoenix assets

A Phoenix application that has an `assets` directory gets one more build step. Aspire adds
`RUN mix assets.deploy` after `mix compile` and before `mix release`. The step builds and digests the
static files that the release serves.

If `assets/package.json` exists, Aspire also installs Node.js and npm in the build stage and runs
`npm ci --prefix assets` before `mix assets.deploy`. Commit `assets/package-lock.json`, because
`npm ci` reads it.

The runtime stage of a Phoenix image sets `PHX_SERVER=true`, so the release starts the endpoint. The
`PORT`, `PHX_HOST`, and `SECRET_KEY_BASE` values arrive as environment variables at run time. The
secret never enters an image layer.

Add `assets.deploy` to the aliases in `mix.exs`:

```elixir
defp aliases do
  [
    "assets.deploy": ["tailwind my_app --minify", "esbuild my_app --minify", "phx.digest"]
  ]
end
```

`WithPhoenixHealthCheck` adds an HTTP health check. The default path is `/health`. The application
must serve the path:

```csharp
builder.AddPhoenixApp("web", "../phoenix-web")
    .WithPhoenixHealthCheck();
```

### An authored Dockerfile

Aspire writes no Dockerfile when the application directory holds one. The `Dockerfile` of the
repository stays the contract, and `aspire publish` builds the image from it.

## Prebuilt releases

`AddMixRelease` adds a Mix release that a different build step already produced. Elixir and Mix do
not have to be on the PATH:

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddMixRelease("api", "../elixir-api/_build/prod/rel/my_app")
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

await builder.addMixRelease("api", "../elixir-api/_build/prod/rel/my_app")
    .withHttpEndpoint({ env: "PORT" });

await builder.build().run();
```

The resource starts the release with `bin/<release-name> start`. On Windows it starts
`bin\<release-name>.bat`. The release name defaults to the name of the release directory. Pass the
third parameter to give a different name:

```csharp
builder.AddMixRelease("api", "../build/output", "my_app");
```

A release carries its compiled dependencies, so this resource adds no Mix setup steps. There is no
`mix deps.get` sibling and no `mix compile` sibling.

In publish mode the resource becomes a container image with one stage. The image copies the release
directory and runs it as the user `app`:

```dockerfile
FROM docker.io/library/erlang:28-slim
RUN apt-get update -y && apt-get install -y ca-certificates locales && apt-get clean && rm -f /var/lib/apt/lists/*_*
RUN sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen && locale-gen
ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8
RUN groupadd --system --gid 999 app && useradd --system --gid 999 --uid 999 --no-create-home app
WORKDIR /app
COPY --chown=app:app . ./
USER app
CMD ["/app/bin/my_app", "start"]
```

Build the release on the same operating system as the runtime image. A release holds the Erlang
runtime system in a compiled form, so a release that a Debian machine produced does not start on
Alpine. Use `WithDockerfileBaseImage` to select a different runtime image.

To name the node, set `RELEASE_NODE` and `RELEASE_COOKIE`, which every Mix release reads:

```csharp
builder.AddMixRelease("api", "../elixir-api/_build/prod/rel/my_app")
    .WithEnvironment("RELEASE_NODE", "api@127.0.0.1")
    .WithEnvironment("RELEASE_COOKIE", cookie);
```

`WithNodeName` is for `AddElixirApp` and `AddPhoenixApp` only.

## OpenTelemetry

Every Elixir and Phoenix resource exports telemetry to the Aspire dashboard. Aspire sets
`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`, and
`OTEL_RESOURCE_ATTRIBUTES`. The Mix setup steps get no telemetry variables, because they produce
no telemetry.

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
:opentelemetry_bandit.setup()
OpentelemetryPhoenix.setup(adapter: :bandit)
OpentelemetryEcto.setup([:my_app, :repo])
```

### Known limits

The Erlang OpenTelemetry metrics and logs OTLP exporters have not shipped. The dashboard shows
traces and console logs only. Metrics from an Elixir resource do not reach the dashboard.

### Certificate trust

The resource sets `OTEL_EXPORTER_OTLP_CERTIFICATE` and `SSL_CERT_FILE` to the Aspire certificate
bundle. The default certificate trust scope is `System`, because the Erlang `:ssl` application
replaces its complete trust set with the file that `SSL_CERT_FILE` names. It cannot add to the
trust set. The `System` scope makes Aspire put the system authorities and the custom authorities in
one bundle.

With the `Append` scope the bundle holds the custom authorities only, so the resource sets
`OTEL_EXPORTER_OTLP_CERTIFICATE` but not `SSL_CERT_FILE`. Aspire applies custom certificate trust
in run mode only.

## Feedback & contributing

https://github.com/microsoft/aspire
