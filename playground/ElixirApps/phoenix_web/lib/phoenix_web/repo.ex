defmodule PhoenixWeb.Repo do
  use Ecto.Repo,
    otp_app: :phoenix_web,
    adapter: Ecto.Adapters.Postgres
end
