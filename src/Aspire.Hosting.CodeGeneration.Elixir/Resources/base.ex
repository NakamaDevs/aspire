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

defmodule Aspire.Marshal do
  @moduledoc """
  Converts values between Elixir terms and the ATS wire form.

  Generated code calls `encode/1` before it sends arguments and `decode/1`
  after it receives a result.
  """

  alias Aspire.CancellationToken
  alias Aspire.Handle

  @doc """
  Converts an Elixir term into a JSON-ready term.

  Handles become `{"$handle": id, "$type": type}` and cancellation tokens
  become their identifier. Functions have no wire form: register a callback
  with `Aspire.Transport.register_callback/2` first.
  """
  @spec encode(term()) :: term()
  def encode(%Handle{} = handle), do: Handle.to_json(handle)
  def encode(%CancellationToken{id: id}), do: id
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
