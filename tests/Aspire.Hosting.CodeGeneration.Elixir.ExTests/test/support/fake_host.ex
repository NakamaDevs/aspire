defmodule Aspire.FakeHost do
  @moduledoc """
  A fake AppHost server for the transport tests.

  The fake host listens on a temporary Unix domain socket, accepts one
  connection, and parses LSP framed JSON-RPC messages. Each parsed request goes
  to the owner process as `{:fake_host, :request, message}`. An optional
  `:handler` function scripts the response.
  """

  use GenServer

  @type response ::
          {:result, term()}
          | {:error, term()}
          | {:message, map()}
          | :noreply

  # ── Public API ────────────────────────────────────────────────────────────

  def start_link(opts \\ []) do
    GenServer.start_link(__MODULE__, {self(), opts})
  end

  @doc "Returns the Unix socket path the fake host listens on."
  def socket_path(host), do: GenServer.call(host, :socket_path)

  @doc "Encodes and sends one JSON-RPC message to the guest."
  def send_message(host, message), do: GenServer.call(host, {:send_raw, frame(message)})

  @doc "Sends raw bytes to the guest. Use it to script partial or joined frames."
  def send_raw(host, bytes), do: GenServer.call(host, {:send_raw, bytes})

  @doc "Returns every byte the guest sent, in order."
  def received_bytes(host), do: GenServer.call(host, :received_bytes)

  @doc "Closes the accepted connection."
  def close(host), do: GenServer.call(host, :close)

  @doc "Frames a message with the LSP `Content-Length` header."
  def frame(message) when is_map(message), do: frame(JSON.encode!(message))

  def frame(body) when is_binary(body) do
    "Content-Length: #{byte_size(body)}\r\n\r\n" <> body
  end

  @doc "Waits for the next request the guest sent and returns it."
  def await_request(timeout \\ 1000) do
    receive do
      {:fake_host, :request, message} -> message
    after
      timeout -> raise "no request from the guest after #{timeout}ms"
    end
  end

  # ── GenServer ─────────────────────────────────────────────────────────────

  @impl true
  def init({owner, opts}) do
    path = Keyword.get_lazy(opts, :socket_path, &temp_socket_path/0)
    File.rm(path)

    {:ok, listen} =
      :gen_tcp.listen(0, [
        :binary,
        {:ifaddr, {:local, path}},
        active: false,
        packet: :raw,
        reuseaddr: true,
        backlog: 1
      ])

    server = self()

    {:ok, _acceptor} =
      Task.start_link(fn ->
        case :gen_tcp.accept(listen) do
          {:ok, socket} ->
            :ok = :gen_tcp.controlling_process(socket, server)
            send(server, {:accepted, socket})

          {:error, _reason} ->
            :ok
        end
      end)

    state = %{
      owner: owner,
      handler: Keyword.get(opts, :handler, fn _ -> :noreply end),
      path: path,
      listen: listen,
      socket: nil,
      buffer: "",
      received: ""
    }

    {:ok, state}
  end

  @impl true
  def handle_call(:socket_path, _from, state), do: {:reply, state.path, state}

  def handle_call(:received_bytes, _from, state), do: {:reply, state.received, state}

  def handle_call({:send_raw, bytes}, _from, state) do
    :ok = :gen_tcp.send(state.socket, bytes)
    {:reply, :ok, state}
  end

  def handle_call(:close, _from, state) do
    if state.socket, do: :gen_tcp.close(state.socket)
    :gen_tcp.close(state.listen)
    File.rm(state.path)
    {:reply, :ok, %{state | socket: nil}}
  end

  @impl true
  def handle_info({:accepted, socket}, state) do
    :ok = :inet.setopts(socket, active: :once)
    {:noreply, %{state | socket: socket}}
  end

  def handle_info({:tcp, socket, data}, state) do
    {frames, buffer} = split_frames(state.buffer <> data, [])
    state = %{state | buffer: buffer, received: state.received <> data}
    state = Enum.reduce(frames, state, &handle_frame/2)
    :ok = :inet.setopts(socket, active: :once)
    {:noreply, state}
  end

  def handle_info({:tcp_closed, _socket}, state) do
    send(state.owner, {:fake_host, :closed})
    {:noreply, %{state | socket: nil}}
  end

  def handle_info(_message, state), do: {:noreply, state}

  @impl true
  def terminate(_reason, state) do
    if state.socket, do: :gen_tcp.close(state.socket)
    :gen_tcp.close(state.listen)
    File.rm(state.path)
    :ok
  end

  # ── Internals ─────────────────────────────────────────────────────────────

  defp handle_frame(body, state) do
    message = JSON.decode!(body)
    send(state.owner, {:fake_host, :request, message})

    case state.handler.(message) do
      {:result, value} ->
        reply(state, %{"jsonrpc" => "2.0", "id" => message["id"], "result" => value})

      {:error, value} ->
        reply(state, %{"jsonrpc" => "2.0", "id" => message["id"], "error" => value})

      {:message, custom} ->
        reply(state, custom)

      :noreply ->
        state
    end
  end

  defp reply(state, message) do
    :ok = :gen_tcp.send(state.socket, frame(message))
    state
  end

  defp split_frames(buffer, acc) do
    case :binary.split(buffer, "\r\n\r\n") do
      [_incomplete] ->
        {Enum.reverse(acc), buffer}

      [headers, rest] ->
        length = content_length(headers)

        if byte_size(rest) >= length do
          <<body::binary-size(length), remaining::binary>> = rest
          split_frames(remaining, [body | acc])
        else
          {Enum.reverse(acc), buffer}
        end
    end
  end

  defp content_length(headers) do
    headers
    |> String.split("\r\n")
    |> Enum.find_value(fn line ->
      case String.split(line, ":", parts: 2) do
        [name, value] ->
          if String.downcase(String.trim(name)) == "content-length" do
            String.to_integer(String.trim(value))
          end

        _ ->
          nil
      end
    end)
  end

  defp temp_socket_path do
    unique = System.unique_integer([:positive])
    base = System.tmp_dir!()
    path = Path.join(base, "ah#{unique}.sock")

    # macOS limits a Unix socket path to 104 bytes.
    if byte_size(path) < 100, do: path, else: Path.join("/tmp", "ah#{unique}.sock")
  end
end
