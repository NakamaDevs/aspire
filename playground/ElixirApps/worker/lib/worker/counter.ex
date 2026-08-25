defmodule Worker.Counter do
  @moduledoc """
  Increments a Redis key every two seconds and writes one trace span for each tick.
  """

  use GenServer

  require Logger
  require OpenTelemetry.Tracer, as: Tracer

  @interval 2_000
  @key "worker:counter"

  def start_link(opts), do: GenServer.start_link(__MODULE__, opts, name: __MODULE__)

  @impl true
  def init(_opts) do
    schedule()
    {:ok, %{}}
  end

  @impl true
  def handle_info(:tick, state) do
    Tracer.with_span "worker.tick" do
      case Redix.command(:cache, ["INCR", @key]) do
        {:ok, value} ->
          Tracer.set_attribute(:"worker.counter", value)
          Logger.info("worker counter #{@key}=#{value}")

        {:error, reason} ->
          Tracer.set_status(:error, inspect(reason))
          Logger.error("worker counter failed: #{inspect(reason)}")
      end
    end

    schedule()
    {:noreply, state}
  end

  defp schedule, do: Process.send_after(self(), :tick, @interval)
end
