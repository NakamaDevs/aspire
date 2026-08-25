defmodule Aspire.ReferenceExpressionTest do
  use ExUnit.Case, async: false

  alias Aspire.FakeHost
  alias Aspire.Handle
  alias Aspire.ReferenceExpression
  alias Aspire.Transport

  test "Aspire.ref builds a reference expression capability call" do
    host = start_host(handler: method_handler(%{"invokeCapability" => "ok"}))
    transport = start_transport(host)

    endpoint = %Handle{
      id: "7",
      type: "Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference"
    }

    # `Aspire.ref/1` lives in the generated entry file. It calls from_parts/1, so the test uses
    # the runtime function that the generated one delegates to.
    expression = ReferenceExpression.from_parts(["http://", endpoint, "/health"])

    assert expression.format == "http://{0}/health"
    assert expression.value_providers == [endpoint]

    assert {:ok, "ok"} =
             Transport.invoke_capability(transport, "Aspire.Hosting/withEnvironment", %{
               "name" => "HEALTH_URL",
               "value" => expression
             })

    assert %{"method" => "invokeCapability", "params" => [_capability, args]} =
             FakeHost.await_request()

    assert args == %{
             "name" => "HEALTH_URL",
             "value" => %{
               "$expr" => %{
                 "format" => "http://{0}/health",
                 "valueProviders" => [
                   %{
                     "$handle" => "7",
                     "$type" => "Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference"
                   }
                 ]
               }
             }
           }
  end

  test "a literal only expression sends no value providers" do
    assert ReferenceExpression.to_wire(ReferenceExpression.from_parts(["plain"])) ==
             %{"$expr" => %{"format" => "plain"}}
  end

  test "a part that is not a string, a number or a handle raises" do
    assert_raise ArgumentError, ~r/a reference expression part/, fn ->
      ReferenceExpression.to_wire(ReferenceExpression.from_parts([%{other: 1}]))
    end
  end

  test "get_value_async resolves an expression the host returned" do
    host = start_host(handler: method_handler(%{"invokeCapability" => "http://localhost:8080"}))
    transport = start_transport(host)

    expression = %ReferenceExpression{
      handle: %Handle{
        id: "3",
        type: "Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression"
      },
      transport: transport
    }

    assert {:ok, "http://localhost:8080"} = ReferenceExpression.get_value_async(expression)

    assert %{"params" => ["Aspire.Hosting.ApplicationModel/getValueAsync", args]} =
             FakeHost.await_request()

    assert args == %{
             "context" => %{
               "$handle" => "3",
               "$type" => "Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression"
             }
           }
  end

  test "get_value_async needs an expression the host returned" do
    assert {:error, %Aspire.Error{code: "INVALID_ARGUMENT"}} =
             ReferenceExpression.get_value_async(ReferenceExpression.from_parts(["local"]))
  end

  # ── Helpers ───────────────────────────────────────────────────────────────

  defp start_host(opts) do
    {:ok, host} = FakeHost.start_link(opts)
    path = FakeHost.socket_path(host)
    on_exit(fn -> File.rm(path) end)
    host
  end

  defp start_transport(host) do
    path = FakeHost.socket_path(host)
    {:ok, transport} = Transport.start_link(socket_path: path)
    on_exit(fn -> stop_transport(transport) end)
    transport
  end

  defp stop_transport(transport) do
    if Process.alive?(transport), do: GenServer.stop(transport, :normal)
  catch
    :exit, _ -> :ok
  end

  defp method_handler(results) do
    fn message ->
      case Map.fetch(results, message["method"]) do
        {:ok, value} -> {:result, value}
        :error -> :noreply
      end
    end
  end
end
