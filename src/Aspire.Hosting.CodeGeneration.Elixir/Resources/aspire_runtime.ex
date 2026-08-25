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
      nil ->
        Aspire.Transport.connect(
          Keyword.take(opts, [:socket_path, :auth_token, :connect_timeout])
        )

      pid ->
        {:ok, pid}
    end
  end

  @doc "Invokes a capability on the transport."
  @spec invoke(GenServer.server(), String.t(), map()) ::
          {:ok, term()} | {:error, Aspire.Error.t()}
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

  def decode(value, {:enum, module}, _transport) when is_binary(value),
    do: module.from_wire(value)

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

  @doc "Converts an enum value into its wire form. Raises on an unknown value."
  @spec encode_enum!(term(), module()) :: term()
  def encode_enum!(nil, _module), do: nil
  def encode_enum!(value, module), do: module.to_wire!(value)

  @doc """
  Checks a union argument and converts it.

  `specs` holds the accepted forms. `:string`, `:number`, `:boolean` and `:any`
  accept a primitive. `{:module, Module}` accepts a struct of that module.

  The function raises `ArgumentError` and names every accepted form when the
  value matches none of them.
  """
  @spec encode_union!(term(), [term()], String.t(), String.t()) :: term()
  def encode_union!(value, specs, function_name, parameter_name) do
    if Enum.any?(specs, fn spec -> union_member?(value, spec) end) do
      encode(value)
    else
      raise ArgumentError,
            "#{function_name} does not accept #{inspect(value)} for #{parameter_name}. " <>
              "It accepts #{describe_union(specs)}."
    end
  end

  defp union_member?(_value, :any), do: true
  defp union_member?(value, :string), do: is_binary(value)
  defp union_member?(value, :number), do: is_number(value)
  defp union_member?(value, :boolean), do: is_boolean(value)
  defp union_member?(value, {:module, module}), do: is_struct(value, module)
  defp union_member?(_value, _spec), do: false

  defp describe_union(specs) do
    specs
    |> Enum.map(fn
      {:module, module} -> inspect(module)
      spec -> Atom.to_string(spec)
    end)
    |> Enum.join(", ")
  end

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

  @doc """
  Adds an optional callback argument.

  `wrapper` builds the typed function that the transport registers. The
  generated code passes a function that decodes the arguments of the host and
  encodes the result.
  """
  @spec put_opt_callback(map(), String.t(), keyword(), atom(), (term() -> term())) :: map()
  def put_opt_callback(args, wire_name, opts, key, wrapper) do
    case Keyword.fetch(opts, key) do
      {:ok, nil} -> args
      {:ok, value} when is_function(value) -> Map.put(args, wire_name, wrapper.(value))
      {:ok, value} -> Map.put(args, wire_name, value)
      :error -> args
    end
  end

  @doc """
  Wraps a callback that a DTO property holds.

  `wrapper` builds the typed function that the transport registers. A nil property and a value
  that is not a function stay as they are.
  """
  @spec wrap_callback(term(), (term() -> term())) :: term()
  def wrap_callback(nil, _wrapper), do: nil
  def wrap_callback(value, wrapper) when is_function(value), do: wrapper.(value)
  def wrap_callback(value, _wrapper), do: value

  @doc """
  Adds the flattened `options` DTO of a capability.

  The keyword list holds the properties of the DTO. An empty list sends no
  argument, so the host keeps its own defaults.
  """
  @spec put_opts_dto(map(), String.t(), keyword(), module()) :: map()
  def put_opts_dto(args, _wire_name, [], _module), do: args

  def put_opts_dto(args, wire_name, opts, module) do
    Map.put(args, wire_name, module.to_wire(module.new(opts)))
  end

  @doc """
  Returns the value a callback sends back to the host.

  `dto_arguments` holds `{index, module, decoded}` for every DTO argument. An
  Elixir struct never changes in place, so the callback returns the changed
  struct and this function puts it in the positional write-back map. A callback
  that returns something else keeps the arguments it received.

  The function returns `nil` when the callback has no DTO argument. The
  transport then echoes the original arguments.
  """
  @spec callback_writeback(term(), [{non_neg_integer(), module(), term()}]) :: map() | nil
  def callback_writeback(_result, []), do: nil

  def callback_writeback(result, dto_arguments) do
    replacements = writeback_replacements(result, dto_arguments)

    Map.new(dto_arguments, fn {index, _module, decoded} ->
      {"p#{index}", encode(Map.get(replacements, index, decoded))}
    end)
  end

  defp writeback_replacements(result, [{index, module, _decoded}]) do
    if is_struct(result, module), do: %{index => result}, else: %{}
  end

  defp writeback_replacements(result, dto_arguments) when is_list(result) do
    if length(result) == length(dto_arguments) do
      dto_arguments
      |> Enum.zip(result)
      |> Enum.reduce(%{}, fn {{index, module, _decoded}, value}, acc ->
        if is_struct(value, module), do: Map.put(acc, index, value), else: acc
      end)
    else
      %{}
    end
  end

  defp writeback_replacements(_result, _dto_arguments), do: %{}

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

defmodule Aspire.List do
  @moduledoc """
  A handle to a mutable .NET list.

  A capability that returns `List<T>` or `IList<T>` returns a handle, not a
  copy. Each function here is one round trip to the host. A read-only
  collection, such as `IReadOnlyList<T>` or `T[]`, arrives as a plain Elixir
  list instead.

      {:ok, tags} = Aspire.CodeGeneration.Tests.TestRedisResource.get_tags(redis)
      {:ok, 0} = Aspire.List.count(tags)
      {:ok, _} = Aspire.List.add(tags, "cache")
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @type result :: {:ok, term()} | {:error, Aspire.Error.t()}

  @doc "Returns every item as an Elixir list."
  @spec to_list(t()) :: result()
  def to_list(%__MODULE__{} = list), do: call(list, "Aspire.Hosting/List.toArray", %{})

  @doc "Returns the number of items."
  @spec count(t()) :: result()
  def count(%__MODULE__{} = list), do: call(list, "Aspire.Hosting/List.length", %{})

  @doc "Returns the item at an index, or nil."
  @spec get(t(), integer()) :: result()
  def get(%__MODULE__{} = list, index) when is_integer(index) do
    call(list, "Aspire.Hosting/List.get", %{"index" => index})
  end

  @doc "Adds an item to the end of the list."
  @spec add(t(), term()) :: result()
  def add(%__MODULE__{} = list, item) do
    call(list, "Aspire.Hosting/List.add", %{"item" => Aspire.Runtime.encode(item)})
  end

  @doc "Replaces the item at an index."
  @spec put(t(), integer(), term()) :: result()
  def put(%__MODULE__{} = list, index, item) when is_integer(index) do
    call(list, "Aspire.Hosting/List.set", %{
      "index" => index,
      "value" => Aspire.Runtime.encode(item)
    })
  end

  @doc "Inserts an item at an index."
  @spec insert(t(), integer(), term()) :: result()
  def insert(%__MODULE__{} = list, index, item) when is_integer(index) do
    call(list, "Aspire.Hosting/List.insert", %{
      "index" => index,
      "item" => Aspire.Runtime.encode(item)
    })
  end

  @doc "Returns the index of an item, or -1."
  @spec index_of(t(), term()) :: result()
  def index_of(%__MODULE__{} = list, item) do
    call(list, "Aspire.Hosting/List.indexOf", %{"item" => Aspire.Runtime.encode(item)})
  end

  @doc "Removes the item at an index."
  @spec remove_at(t(), integer()) :: result()
  def remove_at(%__MODULE__{} = list, index) when is_integer(index) do
    call(list, "Aspire.Hosting/List.removeAt", %{"index" => index})
  end

  @doc "Removes every item."
  @spec clear(t()) :: result()
  def clear(%__MODULE__{} = list), do: call(list, "Aspire.Hosting/List.clear", %{})

  defp call(%__MODULE__{handle: handle} = list, capability, arguments) do
    transport = Aspire.Runtime.transport_of(list)
    args = Map.put(arguments, "list", handle)

    transport
    |> Aspire.Runtime.invoke(capability, args)
    |> Aspire.Runtime.result(nil, transport)
  end
end

defmodule Aspire.Dict do
  @moduledoc """
  A handle to a mutable .NET dictionary.

  A capability that returns `Dictionary<K, V>` or `IDictionary<K, V>` returns a
  handle, not a copy. Each function here is one round trip to the host. A
  read-only dictionary arrives as a plain Elixir map instead.

      {:ok, variables} = Aspire.Hosting.EnvironmentCallbackContext.environment_variables(context)
      {:ok, _} = Aspire.Dict.put(variables, "PORT", "8080")
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @type result :: {:ok, term()} | {:error, Aspire.Error.t()}

  @doc "Returns the dictionary as a plain Elixir map. Every key has to be a string."
  @spec to_map(t()) :: result()
  def to_map(%__MODULE__{} = dict), do: call(dict, "Aspire.Hosting/Dict.toObject", %{})

  @doc "Returns the number of entries."
  @spec count(t()) :: result()
  def count(%__MODULE__{} = dict), do: call(dict, "Aspire.Hosting/Dict.count", %{})

  @doc "Returns the value of a key, or nil."
  @spec get(t(), term()) :: result()
  def get(%__MODULE__{} = dict, key) do
    call(dict, "Aspire.Hosting/Dict.get", %{"key" => Aspire.Runtime.encode(key)})
  end

  @doc "Sets the value of a key."
  @spec put(t(), term(), term()) :: result()
  def put(%__MODULE__{} = dict, key, value) do
    call(dict, "Aspire.Hosting/Dict.set", %{
      "key" => Aspire.Runtime.encode(key),
      "value" => Aspire.Runtime.encode(value)
    })
  end

  @doc "Returns true when the dictionary holds the key."
  @spec has_key?(t(), term()) :: result()
  def has_key?(%__MODULE__{} = dict, key) do
    call(dict, "Aspire.Hosting/Dict.has", %{"key" => Aspire.Runtime.encode(key)})
  end

  @doc "Removes a key."
  @spec delete(t(), term()) :: result()
  def delete(%__MODULE__{} = dict, key) do
    call(dict, "Aspire.Hosting/Dict.remove", %{"key" => Aspire.Runtime.encode(key)})
  end

  @doc "Returns every key."
  @spec keys(t()) :: result()
  def keys(%__MODULE__{} = dict), do: call(dict, "Aspire.Hosting/Dict.keys", %{})

  @doc "Returns every value."
  @spec values(t()) :: result()
  def values(%__MODULE__{} = dict), do: call(dict, "Aspire.Hosting/Dict.values", %{})

  @doc "Removes every entry."
  @spec clear(t()) :: result()
  def clear(%__MODULE__{} = dict), do: call(dict, "Aspire.Hosting/Dict.clear", %{})

  defp call(%__MODULE__{handle: handle} = dict, capability, arguments) do
    transport = Aspire.Runtime.transport_of(dict)
    args = Map.put(arguments, "dict", handle)

    transport
    |> Aspire.Runtime.invoke(capability, args)
    |> Aspire.Runtime.result(nil, transport)
  end
end
