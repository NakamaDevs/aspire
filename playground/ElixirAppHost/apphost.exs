# Aspire Elixir AppHost - Playground
# For more information, see: https://aspire.dev
#
# Elixir AppHost support is experimental. aspire.config.json turns the feature on
# for this directory. To turn it on for every AppHost, run:
#
#     aspire config set features:experimentalPolyglot:elixir true --global
#
# To run:
#
#     aspire run
#
# The applications live in ../ElixirApps. That directory also holds a C# AppHost
# that builds the same model.

Code.require_file(".aspire/modules/aspire.ex", __DIR__)

alias Aspire.DistributedApplicationBuilder, as: Builder
alias Aspire.Elixir.ElixirAppResource
alias Aspire.Elixir.PhoenixAppResource
alias Aspire.PostgreSQL.PostgresServerResource

builder = Aspire.create_builder!()

# The compute environment gives `aspire publish` a target. It writes a Docker Compose
# file and one Dockerfile for each Elixir application.
Builder.add_docker_compose_environment!(builder, "compose")

appdb =
  builder
  |> Builder.add_postgres!("db")
  |> PostgresServerResource.add_database!("appdb")

cache = Builder.add_redis!(builder, "cache")

# A fluent function returns the declared base handle, not the resource handle. The base
# handle modules hold no functions, so a plain pipe stops after one step. `tap/2` keeps
# the resource struct in the pipe. See "Known limits" in README.md.
builder
|> Builder.add_phoenix_app!("web", "../ElixirApps/phoenix_web")
|> tap(&PhoenixAppResource.with_ecto_database!(&1, appdb))
|> tap(&PhoenixAppResource.with_ecto_migrate!/1)
|> tap(&PhoenixAppResource.with_external_http_endpoints!/1)

builder
|> Builder.add_elixir_app!("worker", "../ElixirApps/worker")
|> tap(&ElixirAppResource.with_reference!(&1, cache))
|> tap(&ElixirAppResource.wait_for!(&1, cache))

builder
|> Aspire.build!()
|> Aspire.run!()
