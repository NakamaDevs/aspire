# Elixir integration: local development loop

This page gives the commands for work on `Aspire.Hosting.Elixir` and
`Aspire.Hosting.CodeGeneration.Elixir`. The parity matrix in
[elixir-parity.md](specs/elixir-parity.md) defines done.

## Prerequisites

- Run `./restore.sh` once. It installs the .NET SDK from `global.json` into
  `.dotnet`. Use `.dotnet/dotnet` for every command below.
- Install Elixir 1.19 and Erlang/OTP 28. `mise` reads `.tool-versions` in
  the Elixir sample directories.
- Docker (OrbStack on macOS) for the Postgres and Redis playground containers.

## Solution filter

Open `Aspire-Elixir.slnf` in the IDE. It loads the two Elixir packages, their
test projects, the CLI, the TypeScript generator, and the shared test
utilities.

## Build and test

Hosting integration:

```sh
.dotnet/dotnet build tests/Aspire.Hosting.Elixir.Tests/Aspire.Hosting.Elixir.Tests.csproj
.dotnet/dotnet test --project tests/Aspire.Hosting.Elixir.Tests/Aspire.Hosting.Elixir.Tests.csproj --no-launch-profile -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

Code generator and language support:

```sh
.dotnet/dotnet test --project tests/Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.Elixir.Tests.csproj --no-launch-profile -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

Elixir guest runtime (transport, marshalling, watch):

```sh
cd tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests
mix compile --warnings-as-errors
mix format --check-formatted
mix test
```

CLI registration:

```sh
.dotnet/dotnet test --project tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj --no-launch-profile -- --filter-method "*Elixir*" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

Test filters go after `--`. Do not use the VSTest `--filter` option; it hangs
with Microsoft.Testing.Platform.

## Public API baseline

The build regenerates `src/<Package>/api/<Package>.cs` only when the compiler
runs:

```sh
.dotnet/dotnet build src/Aspire.Hosting.Elixir/Aspire.Hosting.Elixir.csproj -p:GenAPIGenerateReferenceAssemblySource=true --no-incremental
```

That command also rewrites `src/Aspire.Hosting/api/Aspire.Hosting.cs`. Revert
that file with `git checkout -- src/Aspire.Hosting/api/Aspire.Hosting.cs`
before you commit.

## Snapshots

Verify writes `*.received.txt` next to the `Snapshots` directory of a test
project. Review the content, then rename the file to `*.verified.txt`.

## Analyzer rule ASPIREEXPORT009

A generic builder method that takes another `IResourceBuilder<T>` parameter
needs an explicit export id:

```csharp
[AspireExport("withElixirEctoDatabase", MethodName = "withEctoDatabase")]
```

Every other public builder method uses the bare `[AspireExport]` attribute.

## Local release for mise

`eng/scripts/mise-local-release.sh` builds a release from this checkout and
links it into mise:

```sh
eng/scripts/mise-local-release.sh --suffix elixir.20260825
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
