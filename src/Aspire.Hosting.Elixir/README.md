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
In run mode the resource sets `MIX_ENV` to `dev`.

Pass extra application arguments with `.WithAppArgs(...)`. The arguments go after the `--` separator:

```csharp
builder.AddElixirApp("api", "../elixir-api")
    .WithAppArgs("--port", "4000");
// mix run --no-halt -- --port 4000
```

## Feedback & contributing

https://github.com/microsoft/aspire
