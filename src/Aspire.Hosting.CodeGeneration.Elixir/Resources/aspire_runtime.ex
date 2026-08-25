# Runtime helpers for the generated Aspire Elixir SDK.
#
# This file is copied verbatim into `.aspire/modules/` and loaded with
# `Code.require_file/1`. It has no Hex dependency. Load `base.ex` and
# `transport.ex` first.
#
# The generated modules stay small because the logic is here. Each generated
# function collects its arguments, calls `invoke/3`, and converts the result
# with `result/3`.

defmodule Aspire.Runtime do
  @moduledoc """
  Helper functions that the generated Aspire modules call.

  A generated module holds a struct with a `:handle` field and an optional
  `:transport` field. The functions here convert values to the ATS wire form,
  start the transport, and convert results back to Elixir terms.
  """

  # ── Transport ─────────────────────────────────────────────────────────────

  @doc """
  Returns the transport that a wrapper struct uses.

  The default is the registered `Aspire.Transport` process.
  """
  @spec transport_of(term()) :: GenServer.server()
  def transport_of(%{transport: nil}), do: Aspire.Transport
  def transport_of(%{transport: transport}), do: transport
  def transport_of(_value), do: Aspire.Transport

  @doc """
  Returns a started transport.

  The function uses the `:transport` option first, then the registered
  `Aspire.Transport` process, then it connects a new transport.
  """
  @spec ensure_transport(keyword()) :: {:ok, GenServer.server()} | {:error, Aspire.Error.t()}
  def ensure_transport(opts) do
    case Keyword.get(opts, :transport) do
      nil -> connect_default(opts)
      transport -> {:ok, transport}
    end
  end

  defp connect_default(opts) do
    case Process.whereis(Aspire.Transport) do
      nil -> Aspire.Transport.connect(Keyword.take(opts, [:socket_path, :auth_token, :connect_timeout]))
      pid -> {:ok, pid}
    end
  end

  @doc "Invokes a capability on the transport."
  @spec invoke(GenServer.server(), String.t(), map()) :: {:ok, term()} | {:error, Aspire.Error.t()}
  def invoke(transport, capability_id, args) do
    Aspire.Transport.invoke_capability(transport, capability_id, args)
  end

  # ── Results ───────────────────────────────────────────────────────────────

  @doc """
  Converts an invocation result into the return value of a generated function.

  `decoder` tells the function how to convert the value. See `decode/3`.
  """
  @spec result({:ok, term()} | {:error, Aspire.Error.t()}, term(), GenServer.server() | nil) ::
          {:ok, term()} | {:error, Aspire.Error.t()}
  def result({:ok, value}, decoder, transport), do: {:ok, decode(value, decoder, transport)}
  def result({:error, _reason} = error, _decoder, _transport), do: error

  @doc """
  Returns the value of an ok tuple. Raises the error of an error tuple.

  The generated bang functions call this.
  """
  @spec ok!({:ok, term()} | {:error, Aspire.Error.t()}) :: term()
  def ok!({:ok, value}), do: value
  def ok!({:error, %Aspire.Error{} = error}), do: raise(error)

  @doc """
  Converts a value that the host returned.

  | Decoder | Result |
  |---|---|
  | `nil` | the value without a change |
  | `{:handle, module}` | a struct of `module` that holds the handle |
  | `{:dto, module}` | `module.from_wire/1` of the value |
  | `{:enum, module}` | `module.from_wire/1` of the value |
  | `{:list, decoder}` | each item with `decoder` |
  """
  @spec decode(term(), term(), GenServer.server() | nil) :: term()
  def decode(value, nil, _transport), do: value
  def decode(value, {:handle, module}, transport), do: wrap(module, value, transport)
  def decode(value, {:dto, module}, _transport) when is_map(value), do: module.from_wire(value)
  def decode(value, {:dto, _module}, _transport), do: value
  def decode(value, {:enum, module}, _transport) when is_binary(value), do: module.from_wire(value)
  def decode(value, {:enum, _module}, _transport), do: value

  def decode(value, {:list, inner}, transport) when is_list(value) do
    Enum.map(value, fn item -> decode(item, inner, transport) end)
  end

  def decode(value, {:list, _inner}, _transport), do: value

  @doc "Wraps a handle in the struct of a generated module."
  @spec wrap(module(), term(), GenServer.server() | nil) :: term()
  def wrap(module, %Aspire.Handle{} = handle, transport) do
    struct(module, handle: handle, transport: transport)
  end

  def wrap(_module, value, _transport), do: value

  # ── Arguments ─────────────────────────────────────────────────────────────

  @doc """
  Returns the handle inside a value.

  A wrapper struct returns its handle. A list returns a list of handles. Any
  other value returns without a change.
  """
  @spec handle_of(term()) :: term()
  def handle_of(%Aspire.Handle{} = handle), do: handle
  def handle_of(%{handle: %Aspire.Handle{} = handle}), do: handle
  def handle_of(value) when is_list(value), do: Enum.map(value, &handle_of/1)
  def handle_of(value), do: value

  @doc """
  Converts a value into a form that the transport can send.

  A wrapper struct becomes its handle. A struct with a `to_wire/1` function
  becomes a map. A function stays as it is, because the transport registers it
  as a callback.
  """
  @spec encode(term()) :: term()
  def encode(%Aspire.Handle{} = value), do: value
  def encode(%Aspire.CancellationToken{} = value), do: value

  def encode(%module{} = value) do
    cond do
      match?(%Aspire.Handle{}, Map.get(value, :handle)) -> Map.get(value, :handle)
      wire_module?(module) -> module.to_wire(value)
      true -> value
    end
  end

  def encode(value) when is_list(value), do: Enum.map(value, &encode/1)
  def encode(value), do: value

  defp wire_module?(module) do
    Code.ensure_loaded?(module) and function_exported?(module, :to_wire, 1)
  end

  @doc "Converts an enum value into its wire form."
  @spec encode_enum!(term(), module()) :: term()
  def encode_enum!(nil, _module), do: nil
  def encode_enum!(value, module), do: module.to_wire(value)

  @doc "Adds an optional argument when the keyword list holds the key."
  @spec put_opt(map(), String.t(), keyword(), atom()) :: map()
  def put_opt(args, wire_name, opts, key) do
    case Keyword.fetch(opts, key) do
      {:ok, value} -> Map.put(args, wire_name, encode(value))
      :error -> args
    end
  end

  @doc """
  Adds an optional argument without a conversion.

  Callback options use this function. The transport registers the function and
  sends the callback identifier.
  """
  @spec put_opt_raw(map(), String.t(), keyword(), atom()) :: map()
  def put_opt_raw(args, wire_name, opts, key) do
    case Keyword.fetch(opts, key) do
      {:ok, value} -> Map.put(args, wire_name, value)
      :error -> args
    end
  end

  @doc "Adds an optional enum argument."
  @spec put_opt_enum!(map(), String.t(), keyword(), atom(), module()) :: map()
  def put_opt_enum!(args, wire_name, opts, key, module) do
    case Keyword.fetch(opts, key) do
      {:ok, value} -> Map.put(args, wire_name, encode_enum!(value, module))
      :error -> args
    end
  end

  @doc """
  Raises when the keyword list holds a key that the function does not accept.
  """
  @spec validate_opts!(keyword(), [atom()], String.t()) :: keyword()
  def validate_opts!(opts, allowed, function_name) when is_list(opts) do
    case Enum.reject(Keyword.keys(opts), fn key -> key in allowed end) do
      [] ->
        opts

      unknown ->
        raise ArgumentError,
              "#{function_name} does not accept the options #{inspect(unknown)}. " <>
                "It accepts #{inspect(allowed)}."
    end
  end

  # ── DTOs ──────────────────────────────────────────────────────────────────

  @doc """
  Builds the wire map of a DTO.

  The function removes the properties that are nil.
  """
  @spec build_wire([{String.t(), term()}]) :: map()
  def build_wire(pairs) do
    Enum.reduce(pairs, %{}, fn
      {_name, nil}, acc -> acc
      {name, value}, acc -> Map.put(acc, name, encode(value))
    end)
  end

  @doc "Reads one DTO property from a wire map."
  @spec wire_get(map(), String.t(), term()) :: term()
  def wire_get(map, name, decoder \\ nil) do
    decode(Map.get(map, name), decoder, nil)
  end

  # ── createBuilder ─────────────────────────────────────────────────────────

  @doc """
  Builds the `argsOrOptions` value of the `Aspire.Hosting/createBuilder`
  capability.

  The Aspire CLI sets `ASPIRE_PROJECT_DIRECTORY` and `ASPIRE_APPHOST_FILEPATH`.
  The host needs both values to match a `--apphost <directory>` request against
  the AppHost that runs.
  """
  @spec create_builder_args(keyword()) :: map()
  def create_builder_args(opts) do
    %{}
    |> Map.put("Args", Keyword.get(opts, :args) || System.argv())
    |> put_text("ProjectDirectory", [
      Keyword.get(opts, :project_directory),
      System.get_env("ASPIRE_PROJECT_DIRECTORY"),
      File.cwd!()
    ])
    |> put_text("AppHostFilePath", [
      Keyword.get(opts, :app_host_file_path),
      System.get_env("ASPIRE_APPHOST_FILEPATH")
    ])
    |> put_text("DashboardApplicationName", [Keyword.get(opts, :dashboard_application_name)])
  end

  defp put_text(resolved, name, candidates) do
    case Enum.find(candidates, fn value -> is_binary(value) and value != "" end) do
      nil -> resolved
      value -> Map.put(resolved, name, value)
    end
  end
end
