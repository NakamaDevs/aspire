# Aspire Dart transport tests

These tests exercise the Dart resources that the code generator copies into a
guest project:

- `src/Aspire.Hosting.CodeGeneration.Dart/Resources/base.dart`
- `src/Aspire.Hosting.CodeGeneration.Dart/Resources/transport.dart`

The test files import those two files with a relative path, so the tests run
the same bytes that a user gets in `.aspire/modules/`. The resources use the
Dart SDK libraries only. This package declares the `test` runner as its one
development dependency.

## Commands

Run these commands in this directory.

```sh
dart pub get
dart test
dart analyze --fatal-infos
```

## Layout

| Path | Holds |
|---|---|
| `test/aspire_transport_test.dart` | framing, requests, callbacks, and lifecycle |
| `test/env_child_test.dart` | the two cases that need an environment variable |
| `test/support/fake_host.dart` | a fake AppHost on a temporary Unix socket |
| `test/support/env_child.dart` | the guest process that `env_child_test.dart` starts |

Dart cannot change the environment of the running process. The two cases that
read `REMOTE_APP_HOST_SOCKET_PATH` and `ASPIRE_REMOTE_APPHOST_TOKEN` start
`test/support/env_child.dart` with `Process.run` and read its output markers.

The suite fails when `REMOTE_APP_HOST_SOCKET_PATH` is set in your shell,
because one case proves that the transport reports a clear error when the
variable is absent. Unset it before you run the tests.
