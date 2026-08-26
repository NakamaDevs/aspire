# Base types for the Aspire Elixir SDK.
#
# This file is copied verbatim into `.aspire/modules/` and loaded with
# `Code.require_file/1`. It has no Hex dependency. Load `base.ex` before
# `transport.ex`.

defmodule Aspire.Handle do
  @moduledoc """
  A reference to an object that lives in the .NET AppHost.

  A handle travels on the wire as `{"$handle": id, "$type": type}`.
  """

  @enforce_keys [:id, :type]
  defstruct [:id, :type]

  @type t :: %__MODULE__{id: String.t(), type: String.t()}

  @doc "Builds a handle."
  @spec new(String.t(), String.t()) :: t()
  def new(id, type) when is_binary(id) and is_binary(type) do
    %__MODULE__{id: id, type: type}
  end

  @doc "Returns the wire form of a handle."
  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{id: id, type: type}) do
    %{"$handle" => id, "$type" => type}
  end

  @doc "Returns true when the value is the wire form of a handle."
  @spec handle_map?(term()) :: boolean()
  def handle_map?(%{"$handle" => _, "$type" => _}), do: true
  def handle_map?(_value), do: false
end

defmodule Aspire.Error do
  @moduledoc """
  An error that the AppHost returned, or a transport failure.

  The struct is an exception, so `raise Aspire.Error, ...` works and
  `Exception.message/1` returns the `:message` field.

  | Code | Source |
  |---|---|
  | `CAPABILITY_NOT_FOUND`, `HANDLE_NOT_FOUND`, `TYPE_MISMATCH`, `INVALID_ARGUMENT`, `ARGUMENT_OUT_OF_RANGE`, `CALLBACK_ERROR`, `INTERNAL_ERROR` | the host, in `result.$error` |
  | `MISSING_SOCKET_PATH`, `CONNECTION_FAILED`, `CONNECTION_CLOSED`, `AUTHENTICATION_FAILED`, `TRANSPORT_ERROR` | the guest transport |
  """

  defexception [:code, :message, :data, :capability]

  @type t :: %__MODULE__{
          code: String.t() | nil,
          message: String.t() | nil,
          data: term(),
          capability: String.t() | nil
        }

  @doc "Builds an error."
  @spec new(String.t(), String.t(), keyword()) :: t()
  def new(code, message, opts \\ []) do
    %__MODULE__{
      code: code,
      message: message,
      data: Keyword.get(opts, :data),
      capability: Keyword.get(opts, :capability)
    }
  end

  @doc """
  Builds an error from the `$error` object of an ATS result.
  """
  @spec from_ats(map()) :: t()
  def from_ats(%{} = error) do
    %__MODULE__{
      code: Map.get(error, "code"),
      message: Map.get(error, "message", "Capability invocation failed"),
      capability: Map.get(error, "capability"),
      data: error
    }
  end

  @doc """
  Builds an error from a JSON-RPC `error` object.

  The host puts the ATS code in `data.code` when it has one. The numeric
  JSON-RPC code is the fallback.
  """
  @spec from_json_rpc(map()) :: t()
  def from_json_rpc(%{} = error) do
    data = Map.get(error, "data")

    %__MODULE__{
      code: json_rpc_code(data, Map.get(error, "code")),
      message: Map.get(error, "message", "JSON-RPC error"),
      capability: capability_of(data),
      data: data
    }
  end

  defp capability_of(data) when is_map(data), do: Map.get(data, "capability")
  defp capability_of(_data), do: nil

  defp json_rpc_code(data, fallback) when is_map(data) do
    Map.get(data, "code") || to_code(fallback)
  end

  defp json_rpc_code(_data, fallback), do: to_code(fallback)

  defp to_code(nil), do: nil
  defp to_code(code) when is_binary(code), do: code
  defp to_code(code) when is_integer(code), do: Integer.to_string(code)
  defp to_code(code), do: inspect(code)

  @doc "Returns a readable rendering of an error."
  @spec format(t()) :: String.t()
  def format(%__MODULE__{} = error) do
    [
      "Aspire Error: #{error.message}",
      error.code && "  Code: #{error.code}",
      error.capability && "  Capability: #{error.capability}"
    ]
    |> Enum.reject(&(&1 in [nil, false]))
    |> Enum.join("\n")
  end
end

defmodule Aspire.CancellationToken do
  @moduledoc """
  A cooperative cancellation token.

  The guest creates the token identifier. The identifier travels on the wire as
  a plain string. `cancel/2` sends the `cancelToken` request to the host.
  """

  @enforce_keys [:id]
  defstruct [:id]

  @type t :: %__MODULE__{id: String.t()}

  @doc "Creates a token with a new identifier."
  @spec new() :: t()
  def new do
    %__MODULE__{
      id: "ct_#{System.unique_integer([:positive, :monotonic])}_#{System.os_time(:millisecond)}"
    }
  end

  @doc "Wraps an identifier that the host supplied."
  @spec from_id(String.t()) :: t()
  def from_id(id) when is_binary(id), do: %__MODULE__{id: id}

  @doc """
  Cancels the token on the host.

  Returns `{:ok, true}` when the host found and cancelled the token.
  """
  @spec cancel(t(), GenServer.server() | nil) :: {:ok, boolean()} | {:error, Aspire.Error.t()}
  def cancel(%__MODULE__{id: id}, transport \\ nil) do
    module = transport_module()
    module.cancel_token(transport || module, id)
  end

  # The module name is built at run time. base.ex compiles before transport.ex,
  # so a literal name would warn about an undefined module.
  defp transport_module, do: Module.concat(["Aspire", "Transport"])
end

defmodule Aspire.ReferenceExpression do
  @moduledoc """
  A value that references endpoints, parameters and other value providers.

  `Aspire.ref/1` builds an expression from a list of parts. A string part goes
  into the format text. Every other part becomes a value provider and gets a
  `{n}` placeholder.

      Aspire.ref(["http://", endpoint, "/health"])

  The wire form is
  `%{"$expr" => %{"format" => "...", "valueProviders" => [...]}}`.

  The host also returns reference expressions. Such a struct holds a handle and
  no format. `get_value_async/2` resolves it on the host.
  """

  alias Aspire.Handle

  defstruct [:handle, :transport, :format, :value_providers]

  @type t :: %__MODULE__{
          handle: Handle.t() | nil,
          transport: GenServer.server() | nil,
          format: String.t() | nil,
          value_providers: [term()] | nil
        }

  @get_value_capability "Aspire.Hosting.ApplicationModel/getValueAsync"

  @doc """
  Builds a reference expression from a list of parts.

  A binary part is literal text. Every other part becomes a value provider.
  """
  @spec from_parts([term()]) :: t()
  def from_parts(parts) when is_list(parts) do
    {format, providers, _index} =
      Enum.reduce(parts, {"", [], 0}, fn
        part, {format, providers, index} when is_binary(part) ->
          {format <> part, providers, index}

        part, {format, providers, index} ->
          {format <> "{#{index}}", [part | providers], index + 1}
      end)

    %__MODULE__{format: format, value_providers: Enum.reverse(providers)}
  end

  @doc "Returns the wire form of the expression."
  @spec to_wire(t()) :: map()
  def to_wire(%__MODULE__{handle: %Handle{} = handle}), do: Handle.to_json(handle)

  def to_wire(%__MODULE__{format: format, value_providers: providers}) do
    expression = %{"format" => format || ""}

    expression =
      case providers do
        nil -> expression
        [] -> expression
        list -> Map.put(expression, "valueProviders", Enum.map(list, &provider/1))
      end

    %{"$expr" => expression}
  end

  defp provider(%Handle{} = handle), do: Handle.to_json(handle)
  defp provider(%__MODULE__{} = expression), do: to_wire(expression)
  defp provider(%{handle: %Handle{} = handle}), do: Handle.to_json(handle)
  defp provider(value) when is_binary(value), do: value
  defp provider(value) when is_number(value), do: to_string(value)

  defp provider(value) do
    raise ArgumentError,
          "a reference expression part is a string, a number or a handle, not #{inspect(value)}"
  end

  @doc """
  Resolves the expression on the host.

  The function needs an expression that the host returned. A local expression
  has no handle, so the host cannot resolve it.

  ## Options

    * `:cancellation_token` — an `Aspire.CancellationToken`.
  """
  @spec get_value_async(t(), keyword()) :: {:ok, String.t() | nil} | {:error, Aspire.Error.t()}
  def get_value_async(expression, opts \\ [])

  def get_value_async(%__MODULE__{handle: %Handle{} = handle} = expression, opts) do
    runtime = runtime_module()
    transport = runtime.transport_of(expression)

    args =
      runtime.put_opt(%{"context" => handle}, "cancellationToken", opts, :cancellation_token)

    transport
    |> runtime.invoke(@get_value_capability, args)
    |> runtime.result(nil, transport)
  end

  def get_value_async(%__MODULE__{}, _opts) do
    {:error,
     Aspire.Error.new(
       "INVALID_ARGUMENT",
       "get_value_async/2 needs a reference expression that the host returned."
     )}
  end

  @doc "The same as `get_value_async/2`. Raises `Aspire.Error` on a failure."
  @spec get_value_async!(t(), keyword()) :: String.t() | nil
  def get_value_async!(%__MODULE__{} = expression, opts \\ []) do
    runtime_module().ok!(get_value_async(expression, opts))
  end

  # The module name is built at run time. base.ex compiles before
  # aspire_runtime.ex, so a literal name would warn about an undefined module.
  defp runtime_module, do: Module.concat(["Aspire", "Runtime"])
end

defmodule Aspire.Marshal do
  @moduledoc """
  Converts values between Elixir terms and the ATS wire form.

  Generated code calls `encode/1` before it sends arguments and `decode/1`
  after it receives a result.
  """

  alias Aspire.CancellationToken
  alias Aspire.Handle
  alias Aspire.ReferenceExpression

  @doc """
  Converts an Elixir term into a JSON-ready term.

  Handles become `{"$handle": id, "$type": type}` and cancellation tokens
  become their identifier. Functions have no wire form: register a callback
  with `Aspire.Transport.register_callback/2` first.
  """
  @spec encode(term()) :: term()
  def encode(%Handle{} = handle), do: Handle.to_json(handle)
  def encode(%CancellationToken{id: id}), do: id
  def encode(%ReferenceExpression{} = value), do: ReferenceExpression.to_wire(value)
  def encode(%Date{} = value), do: Date.to_iso8601(value)
  def encode(%Time{} = value), do: Time.to_iso8601(value)
  def encode(%DateTime{} = value), do: DateTime.to_iso8601(value)
  def encode(%NaiveDateTime{} = value), do: NaiveDateTime.to_iso8601(value)

  def encode(%_struct{} = value) do
    value |> Map.from_struct() |> encode()
  end

  def encode(value) when is_map(value) do
    Map.new(value, fn {key, item} -> {encode_key(key), encode(item)} end)
  end

  def encode(value) when is_list(value), do: Enum.map(value, &encode/1)

  def encode(value) when is_function(value) do
    raise ArgumentError,
          "a function has no wire form: register it with Aspire.Transport.register_callback/2"
  end

  def encode(value), do: value

  @doc """
  Converts a decoded JSON term into an Elixir term.

  Every `{"$handle": id, "$type": type}` map becomes an `Aspire.Handle`, inside
  maps and lists as well.
  """
  @spec decode(term()) :: term()
  def decode(%{"$handle" => id, "$type" => type}) when is_binary(id) and is_binary(type) do
    Handle.new(id, type)
  end

  def decode(value) when is_map(value) do
    Map.new(value, fn {key, item} -> {key, decode(item)} end)
  end

  def decode(value) when is_list(value), do: Enum.map(value, &decode/1)
  def decode(value), do: value

  defp encode_key(key) when is_binary(key), do: key
  defp encode_key(key) when is_atom(key), do: Atom.to_string(key)
  defp encode_key(key), do: to_string(key)
end
