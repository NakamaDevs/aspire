defmodule PhoenixWebWeb.Router do
  use PhoenixWebWeb, :router

  pipeline :api do
    plug :accepts, ["json"]
  end

  scope "/api", PhoenixWebWeb do
    pipe_through :api

    get "/hello", HelloController, :hello
  end

  scope "/", PhoenixWebWeb do
    pipe_through :api

    get "/", HelloController, :index
    get "/health", HelloController, :health
  end
end
