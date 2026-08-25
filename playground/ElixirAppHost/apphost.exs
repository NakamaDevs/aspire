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

builder
|> Builder.add_phoenix_app!("web", "../ElixirApps/phoenix_web")
|> PhoenixAppResource.with_ecto_database!(appdb)
|> PhoenixAppResource.with_ecto_migrate!()
|> PhoenixAppResource.with_external_http_endpoints!()

builder
|> Builder.add_elixir_app!("worker", "../ElixirApps/worker")
|> ElixirAppResource.with_reference!(cache)
|> ElixirAppResource.wait_for!(cache)

builder
|> Aspire.build!()
|> Aspire.run!()
