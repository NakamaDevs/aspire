defmodule Aspire.TransportTest do
  use ExUnit.Case, async: false

  alias Aspire.CancellationToken
  alias Aspire.FakeHost
  alias Aspire.Handle
  alias Aspire.Transport

  @socket_env "REMOTE_APP_HOST_SOCKET_PATH"
  @token_env "ASPIRE_REMOTE_APPHOST_TOKEN"

  # ── Framing ───────────────────────────────────────────────────────────────

  test "frames a request with Content-Length byte count" do
    host = start_host()
    transport = start_transport(host)

    task = Task.async(fn -> Transport.ping(transport) end)
    assert %{"method" => "ping"} = FakeHost.await_request()

    raw = FakeHost.received_bytes(host)
    assert {length, body} = parse_frame(raw)
    assert length == byte_size(body)
    assert %{"jsonrpc" => "2.0", "method" => "ping", "params" => []} = JSON.decode!(body)
    assert is_integer(JSON.decode!(body)["id"])

    FakeHost.send_message(host, %{"jsonrpc" => "2.0", "id" => 1, "result" => "pong"})
    assert {:ok, "pong"} = Task.await(task)
  end

  test "parses two frames in one packet" do
    host = start_host()
    transport = start_transport(host)

    first = Task.async(fn -> Transport.invoke_capability(transport, "one", %{}) end)
    assert %{"id" => first_id} = FakeHost.await_request()

    second = Task.async(fn -> Transport.invoke_capability(transport, "two", %{}) end)
    assert %{"id" => second_id} = FakeHost.await_request()

    packet =
      FakeHost.frame(%{"jsonrpc" => "2.0", "id" => first_id, "result" => "first"}) <>
        FakeHost.frame(%{"jsonrpc" => "2.0", "id" => second_id, "result" => "second"})

    FakeHost.send_raw(host, packet)

    assert {:ok, "first"} = Task.await(first)
    assert {:ok, "second"} = Task.await(second)
  end

  test "parses a frame split across packets" do
    host = start_host()
    transport = start_transport(host)

    task = Task.async(fn -> Transport.invoke_capability(transport, "one", %{}) end)
    assert %{"id" => id} = FakeHost.await_request()

    frame = FakeHost.frame(%{"jsonrpc" => "2.0", "id" => id, "result" => "split"})
    <<head::binary-size(12), middle::binary-size(20), tail::binary>> = frame

    FakeHost.send_raw(host, head)
    refute Task.yield(task, 50)

    FakeHost.send_raw(host, middle)
    refute Task.yield(task, 50)

    FakeHost.send_raw(host, tail)
    assert {:ok, "split"} = Task.await(task)
  end

  test "multibyte UTF-8 body length is bytes not graphemes" do
    host = start_host()
    transport = start_transport(host)

    text = "héllo → 世界"
    task = Task.async(fn -> Transport.invoke_capability(transport, "echo", %{"text" => text}) end)
    assert %{"id" => id} = FakeHost.await_request()

    raw = FakeHost.received_bytes(host)
    assert {length, body} = parse_frame(raw)
    assert length == byte_size(body)
    assert length > String.length(body)
    assert %{"params" => ["echo", %{"text" => ^text}]} = JSON.decode!(body)

    FakeHost.send_message(host, %{"jsonrpc" => "2.0", "id" => id, "result" => text})
    assert {:ok, ^text} = Task.await(task)
  end

  # ── Requests ──────────────────────────────────────────────────────────────

  test "ping returns pong" do
    host = start_host(handler: method_handler(%{"ping" => "pong"}))
    transport = start_transport(host)

    assert {:ok, "pong"} = Transport.ping(transport)
    assert %{"method" => "ping", "params" => []} = FakeHost.await_request()
  end

  test "invoke_capability returns a handle" do
    result = %{
      "$handle" => "2",
      "$type" => "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource"
    }

    host = start_host(handler: method_handler(%{"invokeCapability" => result}))
    transport = start_transport(host)

    builder = %Handle{
      id: "1",
      type: "Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder"
    }

    assert {:ok, %Handle{id: "2", type: type}} =
             Transport.invoke_capability(transport, "Aspire.Hosting.Redis/addRedis", %{
               "builder" => builder,
               "name" => "cache"
             })

    assert type == "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource"

    assert %{"method" => "invokeCapability", "params" => params} = FakeHost.await_request()

    assert params == [
             "Aspire.Hosting.Redis/addRedis",
             %{
               "builder" => %{
                 "$handle" => "1",
                 "$type" => "Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder"
               },
               "name" => "cache"
             }
           ]
  end

  test "invoke_capability error maps to Aspire.Error with code and data" do
    error = %{
      "$error" => %{
        "code" => "CAPABILITY_NOT_FOUND",
        "message" => "Unknown capability: Contoso.Aspire/bar",
        "capability" => "Contoso.Aspire/bar",
        "details" => %{"parameter" => "name"}
      }
    }

    host = start_host(handler: method_handler(%{"invokeCapability" => error}))
    transport = start_transport(host)

    assert {:error, %Aspire.Error{} = returned} =
             Transport.invoke_capability(transport, "Contoso.Aspire/bar", %{})

    assert returned.code == "CAPABILITY_NOT_FOUND"
    assert returned.message == "Unknown capability: Contoso.Aspire/bar"
    assert returned.capability == "Contoso.Aspire/bar"
    assert returned.data == error["$error"]
    assert Exception.message(returned) == "Unknown capability: Contoso.Aspire/bar"
  end

  test "concurrent requests correlate by id" do
    host = start_host()
    transport = start_transport(host)

    tasks =
      for index <- 1..5 do
        Task.async(fn -> Transport.invoke_capability(transport, "cap#{index}", %{}) end)
      end

    requests = for _ <- 1..5, do: FakeHost.await_request()
    assert Enum.map(requests, & &1["id"]) == Enum.sort(Enum.map(requests, & &1["id"]))

    # Answer in reverse order to prove correlation is by id, not by arrival.
    for request <- Enum.reverse(requests) do
      ["cap" <> index, _args] = request["params"]

      FakeHost.send_message(host, %{
        "jsonrpc" => "2.0",
        "id" => request["id"],
        "result" => "result#{index}"
      })
    end

    assert Enum.map(tasks, &Task.await/1) ==
             for(index <- 1..5, do: {:ok, "result#{index}"})
  end

  test "cancel_token sends cancelToken" do
    host = start_host(handler: method_handler(%{"cancelToken" => true}))
    transport = start_transport(host)

    assert {:ok, true} = Transport.cancel_token(transport, "ct_abc")

    assert %{"method" => "cancelToken", "params" => ["ct_abc"]} = FakeHost.await_request()
  end

  test "Aspire.CancellationToken.cancel sends cancelToken" do
    host = start_host(handler: method_handler(%{"cancelToken" => true}))
    transport = start_transport(host)

    token = CancellationToken.new()
    assert {:ok, true} = CancellationToken.cancel(token, transport)

    assert %{"method" => "cancelToken", "params" => [id]} = FakeHost.await_request()
    assert id == token.id
  end

  # ── Callbacks ─────────────────────────────────────────────────────────────

  test "host invokeCallback runs registered callback and returns result" do
    host = start_host()
    transport = start_transport(host)

    callback_id = Transport.register_callback(transport, fn value -> "got:" <> value end)
    assert is_binary(callback_id)

    invoke_callback(host, 100, callback_id, %{"p0" => "hello"})

    assert_receive {:fake_host, :request, %{"id" => 100, "result" => "got:hello"}}, 1000
  end

  test "callback error is returned as JSON-RPC error" do
    host = start_host()
    transport = start_transport(host)

    callback_id = Transport.register_callback(transport, fn _ -> raise "boom" end)
    invoke_callback(host, 101, callback_id, %{"p0" => "hello"})

    assert_receive {:fake_host, :request, %{"id" => 101, "error" => error}}, 1000
    assert error["message"] =~ "boom"
    refute Map.has_key?(error, "result")
  end

  test "callback that invokes a capability does not deadlock" do
    handler = fn
      %{"method" => "invokeCapability"} -> {:result, %{"$handle" => "7", "$type" => "T"}}
      _ -> :noreply
    end

    host = start_host(handler: handler)
    transport = start_transport(host)

    callback_id =
      Transport.register_callback(transport, fn _context ->
        {:ok, %Handle{id: id}} =
          Transport.invoke_capability(transport, "Aspire.Hosting/build", %{})

        id
      end)

    invoke_callback(host, 102, callback_id, %{"p0" => %{"$handle" => "5", "$type" => "Context"}})

    assert_receive {:fake_host, :request, %{"method" => "invokeCapability"}}, 1000
    assert_receive {:fake_host, :request, %{"id" => 102, "result" => "7"}}, 1000
  end

  test "callback receives a cancellation token for $cancellationToken" do
    host = start_host()
    transport = start_transport(host)

    parent = self()

    callback_id =
      Transport.register_callback(transport, fn context, token ->
        send(parent, {:callback_args, context, token})
        "done"
      end)

    invoke_callback(host, 103, callback_id, %{
      "p0" => %{"$handle" => "5", "$type" => "Context"},
      "p1" => "ct_1",
      "$cancellationToken" => "ct_1"
    })

    assert_receive {:callback_args, %Handle{id: "5"}, %CancellationToken{id: "ct_1"}}, 1000
    assert_receive {:fake_host, :request, %{"id" => 103, "result" => "done"}}, 1000
  end

  # ── Lifecycle ─────────────────────────────────────────────────────────────

  test "socket close stops the transport" do
    host = start_host()
    transport = start_transport(host)

    reference = Process.monitor(transport)
    pending = Task.async(fn -> Transport.invoke_capability(transport, "slow", %{}) end)
    FakeHost.await_request()

    FakeHost.close(host)

    assert {:error, %Aspire.Error{code: "CONNECTION_CLOSED"}} = Task.await(pending)
    assert_receive {:DOWN, ^reference, :process, ^transport, :normal}, 1000
  end

  test "missing REMOTE_APP_HOST_SOCKET_PATH returns a clear error" do
    previous = System.get_env(@socket_env)
    System.delete_env(@socket_env)
    on_exit(fn -> if previous, do: System.put_env(@socket_env, previous) end)

    assert {:error, %Aspire.Error{} = error} = Transport.start_link()
    assert error.code == "MISSING_SOCKET_PATH"
    assert error.message =~ @socket_env
  end

  test "connect reads the socket path from the environment" do
    host = start_host(handler: method_handler(%{"ping" => "pong"}))
    previous = System.get_env(@socket_env)
    System.put_env(@socket_env, FakeHost.socket_path(host))

    on_exit(fn ->
      if previous, do: System.put_env(@socket_env, previous), else: System.delete_env(@socket_env)
    end)

    assert {:ok, transport} = Transport.connect()
    on_exit(fn -> stop_transport(transport) end)

    assert {:ok, "pong"} = Transport.ping()
  end

  test "connect authenticates when ASPIRE_REMOTE_APPHOST_TOKEN is set" do
    host = start_host(handler: method_handler(%{"authenticate" => true}))
    previous = System.get_env(@token_env)
    System.put_env(@token_env, "secret-token")

    on_exit(fn ->
      if previous, do: System.put_env(@token_env, previous), else: System.delete_env(@token_env)
    end)

    transport = start_transport(host)
    assert is_pid(transport)

    assert %{"method" => "authenticate", "params" => ["secret-token"]} = FakeHost.await_request()
  end

  # ── Helpers ───────────────────────────────────────────────────────────────

  defp start_host(opts \\ []) do
    {:ok, host} = FakeHost.start_link(opts)
    path = FakeHost.socket_path(host)
    on_exit(fn -> File.rm(path) end)
    host
  end

  defp start_transport(host, opts \\ []) do
    path = FakeHost.socket_path(host)
    {:ok, transport} = Transport.start_link(Keyword.put(opts, :socket_path, path))
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

  defp invoke_callback(host, id, callback_id, args) do
    FakeHost.send_message(host, %{
      "jsonrpc" => "2.0",
      "id" => id,
      "method" => "invokeCallback",
      "params" => [callback_id, args]
    })
  end

  defp parse_frame("Content-Length: " <> rest) do
    [length, body] = :binary.split(rest, "\r\n\r\n")
    {String.to_integer(length), body}
  end
end
