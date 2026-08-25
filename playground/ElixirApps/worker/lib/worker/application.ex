defmodule Worker.Application do
  @moduledoc false

  use Application

  @impl true
  def start(_type, _args) do
    children = [
      {Redix, Keyword.put(Worker.redix_options(), :name, :cache)},
      Worker.Counter
    ]

    Supervisor.start_link(children, strategy: :one_for_one, name: Worker.Supervisor)
  end
end
