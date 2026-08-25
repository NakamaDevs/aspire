defmodule PhoenixWebWeb.HelloController do
  use PhoenixWebWeb, :controller

  import Ecto.Query, only: [from: 2]

  alias PhoenixWeb.Repo

  # The count query exercises the Ecto path, so the response proves that the
  # database connection from DATABASE_URL works.
  def hello(conn, _params) do
    greetings = Repo.one(from(g in "greetings", select: count(g.id)))

    json(conn, %{message: "hello from phoenix", version: 1, greetings: greetings})
  end

  def health(conn, _params) do
    send_resp(conn, 200, "ok")
  end
end
