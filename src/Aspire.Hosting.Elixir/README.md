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

## Feedback & contributing

https://github.com/microsoft/aspire
