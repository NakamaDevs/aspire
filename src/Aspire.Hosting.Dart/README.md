# Dart app hosting integration

Use this integration to model, configure, and orchestrate Dart applications in an Aspire solution.

## Getting started

### Prerequisites

The **Dart SDK** (`dart`) must be available on the PATH of the machine that runs the AppHost.

### Add the integration

From your AppHost directory, add the `Aspire.Hosting.Dart` integration with the Aspire CLI:

```bash
aspire add Aspire.Hosting.Dart
```

## Usage example

In the AppHost, add a Dart application resource:

**C#**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddDartApp("api", "../dart-api")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**TypeScript**

```typescript
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const api = await builder.addDartApp("api", "../dart-api")
    .withHttpEndpoint({ port: 8080, env: "PORT" })
    .withExternalHttpEndpoints();

await builder.build().run();
```

The method runs the application as `dart run bin/main.dart` from the directory that contains
`pubspec.yaml`. Pass a different entrypoint with the third parameter:

```csharp
builder.AddDartApp("api", "../dart-api", entrypoint: "bin/server.dart");
```

`dart run` gives every argument after the entrypoint to the program, so `.WithAppArgs(...)` needs no
separator. A second call replaces the arguments of the first call:

```csharp
builder.AddDartApp("api", "../dart-api")
    .WithAppArgs("--port", "8080");
// dart run bin/main.dart --port 8080
```

`.WithDartDefine(...)` adds a compile-time environment value that `String.fromEnvironment` reads. The
option comes before the entrypoint. The keys accumulate, and a second call with the same key replaces
the value:

```csharp
builder.AddDartApp("api", "../dart-api")
    .WithDartDefine("FLAVOR", "dev");
// dart run --define=FLAVOR=dev bin/main.dart
```

## Run mode

### Pub dependencies

`AddDartApp` adds a sibling resource with the name `{app}-pub-get` when the application directory
contains `pubspec.yaml`. The step runs `dart pub get`, and the application waits for it. The step
runs in run mode only and stays out of the manifest.

Call `.WithPubGet(install: false)` to keep the step but start it by hand from the dashboard. Call
`.WithPubGet()` again to turn the automatic run back on.

## Feedback & contributing

https://github.com/microsoft/aspire
