# Dart integration: local development loop

This page gives the commands for work on `Aspire.Hosting.Dart` and
`Aspire.Hosting.CodeGeneration.Dart`.

## Prerequisites

- Run `./restore.sh` once. It installs the .NET SDK from `global.json` into
  `.dotnet`. Use `.dotnet/dotnet` for every command below.
- Install the Dart SDK 3.12 or later. The generated SDK needs Dart 3.8 or
  later, which is the constraint in the scaffolded `pubspec.yaml`.
- Docker (OrbStack on macOS) for the container resources in the playground.

## Build and test

Hosting integration:

```sh
.dotnet/dotnet build tests/Aspire.Hosting.Dart.Tests/Aspire.Hosting.Dart.Tests.csproj
.dotnet/dotnet test --project tests/Aspire.Hosting.Dart.Tests/Aspire.Hosting.Dart.Tests.csproj --no-launch-profile -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

Code generator and language support:

```sh
.dotnet/dotnet test --project tests/Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.Dart.Tests.csproj --no-launch-profile -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

That project holds the round-trip tests as well. `DartRoundTripTests` generates
the SDK from the real `Aspire.Hosting` assembly, starts a RemoteHost JSON-RPC
server in the test process on a temporary Unix socket, and runs an
`apphost.dart` script against it with `dart run`. The class carries
`[RequiresTools(["dart"])]`, so the tests are skipped when `dart` is not on the
PATH:

```sh
.dotnet/dotnet test --project tests/Aspire.Hosting.CodeGeneration.Dart.Tests/Aspire.Hosting.CodeGeneration.Dart.Tests.csproj --no-launch-profile -- --filter-class "*.DartRoundTripTests"
```

Dart guest runtime (transport, marshalling, watch):

```sh
cd tests/Aspire.Hosting.CodeGeneration.Dart.DartTests
dart pub get
dart test
dart analyze --fatal-infos
```

Those tests import the four resource files with a relative path, so they run
the same bytes that a user gets in `.aspire/modules/`.

CLI registration:

```sh
.dotnet/dotnet test --project tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj --no-launch-profile -- --filter-method "*Dart*" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

Test filters go after `--`. Do not use the VSTest `--filter` option; it hangs
with Microsoft.Testing.Platform.

## Public API baseline

The build regenerates `src/<Package>/api/<Package>.cs` only when the compiler
runs:

```sh
.dotnet/dotnet build src/Aspire.Hosting.Dart/Aspire.Hosting.Dart.csproj -p:GenAPIGenerateReferenceAssemblySource=true --no-incremental
```

That command also rewrites `src/Aspire.Hosting/api/Aspire.Hosting.cs`. Revert
that file with `git checkout -- src/Aspire.Hosting/api/Aspire.Hosting.cs`
before you commit.

`src/Aspire.Hosting.Dart/api/Aspire.Hosting.Dart.ats.txt` is the second
baseline. It lists the capabilities that the assembly exports, so a change to
an `[AspireExport]` attribute shows up in the diff.

## Snapshots

Verify writes `*.received.*` next to the `Snapshots` directory of a test
project. Review the content, then rename the file to `*.verified.*`. The Dart
snapshot of the generated SDK is
`tests/Aspire.Hosting.CodeGeneration.Dart.Tests/Snapshots/AtsGeneratedAspire.verified.dart`.

## Wire names

The host marshals a data object with the camelCase naming policy of
`System.Text.Json`. The generated Dart code therefore reads and writes the
camelCase name, and not the .NET property name. `AtsDartCodeGenerator.ToWireName`
holds that rule. A callback that receives a data object returns the changed
object, and the host copies the properties back under the same names.

## Analyzer rule ASPIREEXPORT009

A generic builder method that takes another `IResourceBuilder<T>` parameter
needs an explicit export id:

```csharp
[AspireExport("withDartServerpodDatabase", MethodName = "withServerpodDatabase")]
```

Every other public builder method uses the bare `[AspireExport]` attribute.

## Local release for mise

`eng/scripts/mise-local-release.sh` builds a release from this checkout and
links it into mise:

```sh
eng/scripts/mise-local-release.sh --suffix dart.20260826
```

The script runs `./localhive.sh -c Release --native-aot --archive` into
`~/.aspire/local-releases/aspire-<version>`, writes the identity sidecar
`bin/.aspire-install.json` with `channel`, `version`, and `packages`, and runs
`mise link aspire@<version> <layout>`. The `packages` field points the CLI at
`hives/local/packages`, so every `Aspire.Hosting.*` package resolves from the
build. A project pins the release with `mise use aspire@<version>` and sets
`channel` and `sdk.version` in `aspire.config.json`.

`--link-only` rewrites the sidecar and links an existing layout without a
build. `--no-link` builds the layout only. The `.tar.gz` next to the layout is
the portable archive for another machine with the same RID.

## Feature flag

Dart support is experimental. Enable it before you scaffold:

```sh
aspire config set features:experimentalPolyglot:dart true --global
```
