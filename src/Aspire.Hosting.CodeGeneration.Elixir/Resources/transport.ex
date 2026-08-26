# JSON-RPC transport for the Aspire Elixir SDK.
#
# This file is copied verbatim into `.aspire/modules/` and loaded with
# `Code.require_file/1`. It has no Hex dependency. Load `base.ex` first.

defmodule Aspire.Transport do
  @moduledoc """
  A JSON-RPC 2.0 client for the Aspire AppHost.

  The transport connects to the Unix domain socket in
  `REMOTE_APP_HOST_SOCKET_PATH` and speaks the LSP frame format:

      Content-Length: <byte count>\\r\\n\\r\\n<utf8 json>

  The process owns the socket. It never blocks on a reply: `handle_call/3`
  stores the caller in a pending map and `GenServer.reply/2` answers when the
  response frame arrives. Host callbacks run in a separate task, so a callback
  can invoke a capability without a deadlock.

  ## Guest to host methods

  | Method | Function |
  |---|---|
  | `ping` | `ping/1` |
  | `invokeCapability` | `invoke_capability/3` |
  | `cancelToken` | `cancel_token/2` |
  | `authenticate` | sent by `start_link/1` when `ASPIRE_REMOTE_APPHOST_TOKEN` is set |

  ## Host to guest methods

  | Method | Behaviour |
  |---|---|
  | `invokeCallback` | runs the function that `register_callback/2` stored |
  """

  use GenServer

  alias Aspire.CancellationToken
  alias Aspire.Error
  alias Aspire.Handle
  alias Aspire.Marshal

  @socket_path_env "REMOTE_APP_HOST_SOCKET_PATH"
  @auth_token_env "ASPIRE_REMOTE_APPHOST_TOKEN"
  @connect_timeout 10_000

  @typedoc "A started transport: a pid or a registered name."
  @type transport :: GenServer.server()

  @type result :: {:ok, term()} | {:error, Error.t()}

  # ── Lifecycle ─────────────────────────────────────────────────────────────

  @doc """
  Connects to the AppHost and registers the process as `Aspire.Transport`.

  The other functions use that name by default.
  """
  @spec connect(keyword()) :: {:ok, pid()} | {:error, Error.t()}
  def connect(opts \\ []) do
    start_link(Keyword.put_new(opts, :name, __MODULE__))
  end

  @doc """
  Connects to the AppHost and links the process to the caller.

  ## Options

    * `:socket_path` — the socket path. It replaces `REMOTE_APP_HOST_SOCKET_PATH`.
    * `:name` — a registered name for the process. The default is no name.
    * `:auth_token` — the authentication token. It replaces
      `ASPIRE_REMOTE_APPHOST_TOKEN`. The transport skips authentication when
      neither is set.
    * `:connect_timeout` — the connect timeout in milliseconds.
  """
  @spec start_link(keyword()) :: {:ok, pid()} | {:error, Error.t()}
  def start_link(opts \\ []) do
    with {:ok, path} <- resolve_socket_path(opts),
         {:ok, socket} <- open_socket(path, opts) do
      start_process(socket, path, opts)
    end
  end

  @doc "Stops the transport."
  @spec stop(transport()) :: :ok
  def stop(transport \\ __MODULE__), do: GenServer.stop(transport, :normal)

  @doc "Returns the socket path the transport is connected to."
  @spec socket_path(transport()) :: String.t()
  def socket_path(transport \\ __MODULE__), do: GenServer.call(transport, :socket_path)

  # ── Requests ──────────────────────────────────────────────────────────────

  @doc "Sends `ping`. Returns `{:ok, \"pong\"}`."
  @spec ping(transport()) :: result()
  def ping(transport \\ __MODULE__), do: request(transport, "ping", [])

  @doc """
  Invokes a capability.

  `args` is a map of argument names to values. A function in `args` becomes a
  registered callback identifier. A handle becomes its wire form.
  """
  @spec invoke_capability(transport(), String.t(), map()) :: result()
  def invoke_capability(transport \\ __MODULE__, capability_id, args)
      when is_binary(capability_id) and is_map(args) do
    request(transport, "invokeCapability", [capability_id, args])
  end

  @doc "Sends `cancelToken`. Returns `{:ok, true}` when the host cancelled the token."
  @spec cancel_token(transport(), String.t() | CancellationToken.t()) :: result()
  def cancel_token(transport \\ __MODULE__, token)

  def cancel_token(transport, %CancellationToken{id: id}), do: cancel_token(transport, id)

  def cancel_token(transport, token_id) when is_binary(token_id) do
    request(transport, "cancelToken", [token_id])
  end

  @doc """
  Sends any JSON-RPC request and waits for the response.

  The call never times out. A capability such as `Aspire.Hosting/run` returns
  only when the application stops.
  """
  @spec request(transport(), String.t(), list()) :: result()
  def request(transport \\ __MODULE__, method, params)
      when is_binary(method) and is_list(params) do
    GenServer.call(transport, {:request, method, params}, :infinity)
  catch
    :exit, {:noproc, _} -> {:error, connection_closed()}
    :exit, {:normal, _} -> {:error, connection_closed()}
    :exit, {:shutdown, _} -> {:error, connection_closed()}
  end

  # ── Callbacks ─────────────────────────────────────────────────────────────

  @doc """
  Registers a function that the host can invoke.

  Returns the callback identifier. Pass the identifier, or the function itself,
  as a capability argument.
  """
  @spec register_callback(transport(), function()) :: String.t()
  def register_callback(transport \\ __MODULE__, fun) when is_function(fun) do
    GenServer.call(transport, {:register_callback, fun})
  end

  @doc "Removes a callback. Returns true when the callback existed."
  @spec unregister_callback(transport(), String.t()) :: boolean()
  def unregister_callback(transport \\ __MODULE__, callback_id) when is_binary(callback_id) do
    GenServer.call(transport, {:unregister_callback, callback_id})
  end

  # ── GenServer ─────────────────────────────────────────────────────────────

  @impl true
  def init({socket, path}) do
    state = %{
      socket: socket,
      path: path,
      buffer: "",
      next_id: 1,
      pending: %{},
      callbacks: %{},
      callback_counter: 0
    }

    {:ok, state}
  end

  @impl true
  def handle_call(:activate, _from, state) do
    :ok = :inet.setopts(state.socket, active: :once)
    {:reply, :ok, state}
  end

  def handle_call(:socket_path, _from, state), do: {:reply, state.path, state}

  def handle_call({:request, method, params}, from, state) do
    {params, state} = marshal(params, state)
    id = state.next_id
    message = %{"jsonrpc" => "2.0", "id" => id, "method" => method, "params" => params}

    case send_message(state, message) do
      :ok ->
        {:noreply, %{state | next_id: id + 1, pending: Map.put(state.pending, id, from)}}

      {:error, reason} ->
        {:reply, {:error, transport_error(reason)}, state}
    end
  end

  def handle_call({:register_callback, fun}, _from, state) do
    {id, state} = put_callback(fun, state)
    {:reply, id, state}
  end

  def handle_call({:unregister_callback, id}, _from, state) do
    {existing, callbacks} = Map.pop(state.callbacks, id)
    {:reply, existing != nil, %{state | callbacks: callbacks}}
  end

  @impl true
  def handle_cast({:send, message}, state) do
    _ = send_message(state, message)
    {:noreply, state}
  end

  @impl true
  def handle_info({:tcp, socket, data}, state) do
    {frames, buffer} = split_frames(state.buffer <> data, [])
    state = Enum.reduce(frames, %{state | buffer: buffer}, &dispatch/2)
    :ok = :inet.setopts(socket, active: :once)
    {:noreply, state}
  end

  def handle_info({:tcp_closed, _socket}, state) do
    {:stop, :normal, fail_pending(state, connection_closed())}
  end

  def handle_info({:tcp_error, _socket, reason}, state) do
    {:stop, :normal, fail_pending(state, transport_error(reason))}
  end

  def handle_info(_message, state), do: {:noreply, state}

  @impl true
  def terminate(_reason, state) do
    fail_pending(state, connection_closed())
    if state.socket, do: :gen_tcp.close(state.socket)
    :ok
  end

  # ── Connection ────────────────────────────────────────────────────────────

  defp resolve_socket_path(opts) do
    case Keyword.get(opts, :socket_path) || System.get_env(@socket_path_env) do
      path when is_binary(path) and path != "" ->
        {:ok, path}

      _ ->
        {:error,
         Error.new(
           "MISSING_SOCKET_PATH",
           "The #{@socket_path_env} environment variable is not set. " <>
             "The Aspire CLI sets it when it starts the guest. " <>
             "Pass socket_path: to Aspire.Transport.start_link/1 to override it."
         )}
    end
  end

  defp open_socket(path, opts) do
    timeout = Keyword.get(opts, :connect_timeout, @connect_timeout)

    case :gen_tcp.connect({:local, path}, 0, [:binary, active: false, packet: :raw], timeout) do
      {:ok, socket} ->
        {:ok, socket}

      {:error, reason} ->
        {:error,
         Error.new(
           "CONNECTION_FAILED",
           "Cannot connect to the AppHost socket #{path}: #{format_reason(reason)}",
           data: reason
         )}
    end
  end

  defp start_process(socket, path, opts) do
    name = Keyword.get(opts, :name)
    gen_opts = if name, do: [name: name], else: []

    case GenServer.start_link(__MODULE__, {socket, path}, gen_opts) do
      {:ok, pid} ->
        :ok = :gen_tcp.controlling_process(socket, pid)
        :ok = GenServer.call(pid, :activate)
        finish_start(pid, opts)

      other ->
        :gen_tcp.close(socket)
        other
    end
  end

  defp finish_start(pid, opts) do
    case authenticate(pid, opts) do
      :ok ->
        {:ok, pid}

      {:error, %Error{}} = error ->
        GenServer.stop(pid, :normal)
        error
    end
  end

  defp authenticate(pid, opts) do
    case Keyword.get(opts, :auth_token) || System.get_env(@auth_token_env) do
      token when is_binary(token) and token != "" ->
        case request(pid, "authenticate", [token]) do
          {:ok, true} ->
            :ok

          {:ok, _other} ->
            {:error,
             Error.new("AUTHENTICATION_FAILED", "The AppHost server rejected the guest token.")}

          {:error, %Error{}} = error ->
            error
        end

      _ ->
        :ok
    end
  end

  # ── Framing ───────────────────────────────────────────────────────────────

  defp send_message(state, message) do
    body = JSON.encode!(message)
    header = "Content-Length: #{byte_size(body)}\r\n\r\n"
    :gen_tcp.send(state.socket, [header, body])
  end

  # Returns the complete frame bodies and the bytes that stay in the buffer.
  defp split_frames(buffer, acc) do
    case :binary.split(buffer, "\r\n\r\n") do
      [_incomplete] ->
        {Enum.reverse(acc), buffer}

      [headers, rest] ->
        case content_length(headers) do
          {:ok, length} when byte_size(rest) >= length ->
            <<body::binary-size(length), remaining::binary>> = rest
            split_frames(remaining, [body | acc])

          {:ok, _length} ->
            {Enum.reverse(acc), buffer}

          :error ->
            # The header block has no usable Content-Length. Drop it and
            # continue with the bytes that follow.
            split_frames(rest, acc)
        end
    end
  end

  defp content_length(headers) do
    headers
    |> String.split("\r\n")
    |> Enum.find_value(:error, &parse_content_length/1)
  end

  defp parse_content_length(line) do
    case String.split(line, ":", parts: 2) do
      [name, value] ->
        if String.downcase(String.trim(name)) == "content-length" do
          case Integer.parse(String.trim(value)) do
            {length, ""} when length >= 0 -> {:ok, length}
            _ -> nil
          end
        end

      _ ->
        nil
    end
  end

  # ── Dispatch ──────────────────────────────────────────────────────────────

  defp dispatch(body, state) do
    case JSON.decode(body) do
      {:ok, %{"method" => method} = message} -> handle_host_request(method, message, state)
      {:ok, %{"id" => id} = message} -> reply_pending(id, message, state)
      _ -> state
    end
  end

  defp reply_pending(id, message, state) do
    case Map.pop(state.pending, id) do
      {nil, _pending} ->
        state

      {from, pending} ->
        GenServer.reply(from, to_result(message))
        %{state | pending: pending}
    end
  end

  defp to_result(%{"error" => error}) when is_map(error) do
    {:error, Error.from_json_rpc(error)}
  end

  defp to_result(%{"result" => %{"$error" => error}}) when is_map(error) do
    {:error, Error.from_ats(error)}
  end

  defp to_result(message), do: {:ok, Marshal.decode(Map.get(message, "result"))}

  defp handle_host_request("invokeCallback", message, state) do
    id = Map.get(message, "id")
    {callback_id, args} = callback_params(Map.get(message, "params"))

    case Map.fetch(state.callbacks, callback_id) do
      {:ok, fun} ->
        run_callback(fun, args, id, self())
        state

      :error ->
        respond(state, id, {:error, "Callback not found: #{inspect(callback_id)}"})
    end
  end

  defp handle_host_request(method, message, state) do
    respond(state, Map.get(message, "id"), {:error, "Unknown method: #{method}"})
  end

  defp callback_params([callback_id, args | _rest]), do: {callback_id, args}
  defp callback_params([callback_id]), do: {callback_id, nil}
  defp callback_params(_params), do: {nil, nil}

  # The callback runs outside the transport process. A callback is free to call
  # invoke_capability/3, which would deadlock inside the GenServer loop.
  defp run_callback(fun, args, id, transport) do
    {:ok, _pid} =
      Task.start(fn ->
        outcome =
          try do
            case apply(fun, positional_args(args)) do
              # Write-back protocol: a callback that returns nil returns the
              # original arguments, so the host can detect DTO mutations.
              nil -> {:ok, args}
              result -> {:ok, Marshal.encode(result)}
            end
          rescue
            exception -> {:error, Exception.message(exception)}
          catch
            :exit, reason -> {:error, "Callback exited: #{inspect(reason)}"}
            thrown -> {:error, "Callback threw: #{inspect(thrown)}"}
          end

        if id != nil do
          GenServer.cast(transport, {:send, response_message(id, outcome)})
        end
      end)

    :ok
  end

  # The host serializes callback arguments with the positional keys p0, p1, ...
  # A `$cancellationToken` entry repeats the identifier that one pN carries.
  defp positional_args(args) when is_map(args) do
    token = Map.get(args, "$cancellationToken")

    args
    |> collect_positional(0, [])
    |> Enum.map(&decode_callback_arg(&1, token))
  end

  defp positional_args(nil), do: []
  defp positional_args(args), do: [Marshal.decode(args)]

  defp collect_positional(args, index, acc) do
    case Map.fetch(args, "p#{index}") do
      {:ok, value} -> collect_positional(args, index + 1, [value | acc])
      :error -> Enum.reverse(acc)
    end
  end

  defp decode_callback_arg(value, token) when is_binary(token) and value == token do
    CancellationToken.from_id(token)
  end

  defp decode_callback_arg(value, _token), do: Marshal.decode(value)

  defp respond(state, nil, _outcome), do: state

  defp respond(state, id, outcome) do
    _ = send_message(state, response_message(id, outcome))
    state
  end

  defp response_message(id, {:ok, result}) do
    %{"jsonrpc" => "2.0", "id" => id, "result" => result}
  end

  defp response_message(id, {:error, message}) do
    %{
      "jsonrpc" => "2.0",
      "id" => id,
      "error" => %{"code" => -32_000, "message" => message}
    }
  end

  # ── Marshalling ───────────────────────────────────────────────────────────

  # Walks the arguments, registers every function as a callback, and converts
  # the rest with Aspire.Marshal.encode/1.
  defp marshal(value, state) when is_function(value), do: put_callback(value, state)

  defp marshal(%Handle{} = value, state), do: {Handle.to_json(value), state}
  defp marshal(%CancellationToken{id: id}, state), do: {id, state}
  defp marshal(%_struct{} = value, state), do: {Marshal.encode(value), state}

  defp marshal(value, state) when is_map(value) do
    Enum.reduce(value, {%{}, state}, fn {key, item}, {acc, acc_state} ->
      {item, acc_state} = marshal(item, acc_state)
      {Map.put(acc, marshal_key(key), item), acc_state}
    end)
  end

  defp marshal(value, state) when is_list(value) do
    Enum.map_reduce(value, state, &marshal/2)
  end

  defp marshal(value, state), do: {value, state}

  defp marshal_key(key) when is_binary(key), do: key
  defp marshal_key(key) when is_atom(key), do: Atom.to_string(key)
  defp marshal_key(key), do: to_string(key)

  defp put_callback(fun, state) do
    counter = state.callback_counter + 1
    id = "callback_#{counter}_#{System.os_time(:millisecond)}"
    state = %{state | callback_counter: counter, callbacks: Map.put(state.callbacks, id, fun)}
    {id, state}
  end

  # ── Errors ────────────────────────────────────────────────────────────────

  defp fail_pending(state, error) do
    Enum.each(state.pending, fn {_id, from} -> GenServer.reply(from, {:error, error}) end)
    %{state | pending: %{}}
  end

  defp connection_closed do
    Error.new("CONNECTION_CLOSED", "The AppHost closed the connection.")
  end

  defp transport_error(reason) do
    Error.new("TRANSPORT_ERROR", "The AppHost connection failed: #{format_reason(reason)}",
      data: reason
    )
  end

  defp format_reason(reason) when is_atom(reason) do
    case :inet.format_error(reason) do
      ~c"unknown POSIX error" -> inspect(reason)
      message -> List.to_string(message)
    end
  end

  defp format_reason(reason), do: inspect(reason)
end
