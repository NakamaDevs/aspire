# aspire_generated.ex - Generated Aspire modules
# GENERATED CODE - DO NOT EDIT

defmodule Aspire.Enums.TestPersistenceMode do
  @moduledoc """
  Test persistence mode enum.

  The `TestPersistenceMode` enum. A value is an atom. The wire form is the .NET member name.
  """

  @values [:none, :volume, :bind]
  @to_wire %{none: "None", volume: "Volume", bind: "Bind"}
  @from_wire %{"None" => :none, "Volume" => :volume, "Bind" => :bind}

  @doc """
  Returns every value of the enum.
  """
  @spec values() :: [atom()]
  def values, do: @values

  @doc """
  Returns the wire form of a value.
  """
  @spec to_wire(atom() | String.t()) :: String.t()
  def to_wire(value) when is_binary(value), do: value

  def to_wire(value) when is_atom(value) do
    case Map.fetch(@to_wire, value) do
      {:ok, name} -> name
      :error -> raise ArgumentError, "Aspire.Enums.TestPersistenceMode has no value #{inspect(value)}"
    end
  end

  @doc """
  Returns the value of a wire form.
  """
  @spec from_wire(term()) :: term()
  def from_wire(name) when is_binary(name), do: Map.get(@from_wire, name, name)
  def from_wire(value), do: value
end

defmodule Aspire.Enums.TestResourceStatus do
  @moduledoc """
  Test enum for type generation verification.

  The `TestResourceStatus` enum. A value is an atom. The wire form is the .NET member name.
  """

  @values [:pending, :running, :stopped, :failed]
  @to_wire %{pending: "Pending", running: "Running", stopped: "Stopped", failed: "Failed"}
  @from_wire %{"Pending" => :pending, "Running" => :running, "Stopped" => :stopped, "Failed" => :failed}

  @doc """
  Returns every value of the enum.
  """
  @spec values() :: [atom()]
  def values, do: @values

  @doc """
  Returns the wire form of a value.
  """
  @spec to_wire(atom() | String.t()) :: String.t()
  def to_wire(value) when is_binary(value), do: value

  def to_wire(value) when is_atom(value) do
    case Map.fetch(@to_wire, value) do
      {:ok, name} -> name
      :error -> raise ArgumentError, "Aspire.Enums.TestResourceStatus has no value #{inspect(value)}"
    end
  end

  @doc """
  Returns the value of a wire form.
  """
  @spec from_wire(term()) :: term()
  def from_wire(name) when is_binary(name), do: Map.get(@from_wire, name, name)
  def from_wire(value), do: value
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestConfigDto do
  @moduledoc """
  Test DTO to verify [AspireDto] generates TypeScript interfaces.
  """

  defstruct [:name, :port, :enabled, :optional_field]

  @type t :: %__MODULE__{}

  @doc """
  Returns the wire form of the struct. The nil properties are removed.
  """
  @spec to_wire(t()) :: map()
  def to_wire(%__MODULE__{} = value) do
    Aspire.Runtime.build_wire([
      {"Name", value.name},
      {"Port", value.port},
      {"Enabled", value.enabled},
      {"OptionalField", value.optional_field}
    ])
  end

  @doc """
  Builds the struct from a wire map.
  """
  @spec from_wire(term()) :: term()
  def from_wire(%{} = wire) do
    %__MODULE__{
      name: Aspire.Runtime.wire_get(wire, "Name"),
      port: Aspire.Runtime.wire_get(wire, "Port"),
      enabled: Aspire.Runtime.wire_get(wire, "Enabled"),
      optional_field: Aspire.Runtime.wire_get(wire, "OptionalField")
    }
  end

  def from_wire(value), do: value
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestDeeplyNestedDto do
  @moduledoc """
  Test DTO with deeply nested generic types.
  """

  defstruct [:nested_data, :metadata_array]

  @type t :: %__MODULE__{}

  @doc """
  Returns the wire form of the struct. The nil properties are removed.
  """
  @spec to_wire(t()) :: map()
  def to_wire(%__MODULE__{} = value) do
    Aspire.Runtime.build_wire([
      {"NestedData", value.nested_data},
      {"MetadataArray", value.metadata_array}
    ])
  end

  @doc """
  Builds the struct from a wire map.
  """
  @spec from_wire(term()) :: term()
  def from_wire(%{} = wire) do
    %__MODULE__{
      nested_data: Aspire.Runtime.wire_get(wire, "NestedData"),
      metadata_array: Aspire.Runtime.wire_get(wire, "MetadataArray")
    }
  end

  def from_wire(value), do: value
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestNestedDto do
  @moduledoc """
  Test DTO with complex nested types.
  """

  defstruct [:id, :config, :tags, :counts]

  @type t :: %__MODULE__{}

  @doc """
  Returns the wire form of the struct. The nil properties are removed.
  """
  @spec to_wire(t()) :: map()
  def to_wire(%__MODULE__{} = value) do
    Aspire.Runtime.build_wire([
      {"Id", value.id},
      {"Config", value.config},
      {"Tags", value.tags},
      {"Counts", value.counts}
    ])
  end

  @doc """
  Builds the struct from a wire map.
  """
  @spec from_wire(term()) :: term()
  def from_wire(%{} = wire) do
    %__MODULE__{
      id: Aspire.Runtime.wire_get(wire, "Id"),
      config: Aspire.Runtime.wire_get(wire, "Config", {:dto, Aspire.CodeGeneration.Elixir.Tests.TestConfigDto}),
      tags: Aspire.Runtime.wire_get(wire, "Tags"),
      counts: Aspire.Runtime.wire_get(wire, "Counts")
    }
  end

  def from_wire(value), do: value
end

defmodule Aspire.Values.TestConfigs do
  @moduledoc """
  Exported Aspire values. The values are snapped when the SDK is generated.
  """

  @doc """
  The default test configuration.
  """
  def default do
    Aspire.Runtime.decode(%{"Enabled" => true, "Name" => "default", "OptionalField" => "cache", "Port" => 6379}, {:dto, Aspire.CodeGeneration.Elixir.Tests.TestConfigDto}, nil)
  end

  def secure do
    Aspire.Runtime.decode(%{"Enabled" => true, "Name" => "secure", "OptionalField" => nil, "Port" => 6380}, {:dto, Aspire.CodeGeneration.Elixir.Tests.TestConfigDto}, nil)
  end

  def unicode_greeting do
    "你好こんにちは"
  end
end

defmodule Aspire.Values.TestConfigs.Profiles do
  @moduledoc """
  Exported Aspire values. The values are snapped when the SDK is generated.
  """

  def development do
    Aspire.Runtime.decode(%{"Enabled" => false, "Name" => "development", "OptionalField" => nil, "Port" => 5001}, {:dto, Aspire.CodeGeneration.Elixir.Tests.TestConfigDto}, nil)
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.ITestVaultResource do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestCallbackContext do
  @moduledoc """
  Test callback context for WithCustomCallback. Also used to verify [AspireExport(ExposeProperties = true)] scanning.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  CancellationToken is supported by ATS.
  """
  def cancellation_token(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `cancellation_token/1`. Raises `Aspire.Error` on a failure.
  """
  def cancellation_token!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(cancellation_token(target))
  end

  @doc """
  Gets the Name property
  """
  def name(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `name/1`. Raises `Aspire.Error` on a failure.
  """
  def name!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(name(target))
  end

  @doc """
  CancellationToken is supported by ATS.
  """
  def set_cancellation_token(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestCallbackContext}, transport)
  end

  @doc """
  The same as `set_cancellation_token/2`. Raises `Aspire.Error` on a failure.
  """
  def set_cancellation_token!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_cancellation_token(target, value))
  end

  @doc """
  Sets the Name property
  """
  def set_name(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestCallbackContext}, transport)
  end

  @doc """
  The same as `set_name/2`. Raises `Aspire.Error` on a failure.
  """
  def set_name!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_name(target, value))
  end

  @doc """
  Sets the Value property
  """
  def set_value(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestCallbackContext}, transport)
  end

  @doc """
  The same as `set_value/2`. Raises `Aspire.Error` on a failure.
  """
  def set_value!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_value(target, value))
  end

  @doc """
  Gets the Value property
  """
  def value(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `value/1`. Raises `Aspire.Error` on a failure.
  """
  def value!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(value(target))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestCollectionContext do
  @moduledoc """
  Test context with collection properties to verify consistent code generation. Verifies both List and Dictionary properties generate proper getter patterns.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  List property - should generate AspireList getter like Dictionary properties.
  """
  def items(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `items/1`. Raises `Aspire.Error` on a failure.
  """
  def items!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(items(target))
  end

  @doc """
  Dictionary property - already works with AspireDict getter.
  """
  def metadata(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `metadata/1`. Raises `Aspire.Error` on a failure.
  """
  def metadata!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(metadata(target))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Waits for another resource (test version)
  """
  def test_wait_for(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/testWaitFor", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `test_wait_for/2`. Raises `Aspire.Error` on a failure.
  """
  def test_wait_for!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(test_wait_for(target, dependency))
  end

  @doc """
  Configures environment with callback (test version)
  """
  def test_with_environment_callback(%__MODULE__{} = target, callback) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("callback", callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/testWithEnvironmentCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithEnvironment}, transport)
  end

  @doc """
  The same as `test_with_environment_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def test_with_environment_callback!(%__MODULE__{} = target, callback) do
    Aspire.Runtime.ok!(test_with_environment_callback(target, callback))
  end

  @doc """
  Performs a cancellable operation
  """
  def with_cancellable_operation(%__MODULE__{} = target, operation) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("operation", operation)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCancellableOperation", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_cancellable_operation/2`. Raises `Aspire.Error` on a failure.
  """
  def with_cancellable_operation!(%__MODULE__{} = target, operation) do
    Aspire.Runtime.ok!(with_cancellable_operation(target, operation))
  end

  @doc """
  Configures the resource with a DTO
  """
  def with_config(%__MODULE__{} = target, config) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("config", Aspire.Runtime.encode(config))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withConfig", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_config/2`. Raises `Aspire.Error` on a failure.
  """
  def with_config!(%__MODULE__{} = target, config) do
    Aspire.Runtime.ok!(with_config(target, config))
  end

  @doc """
  Sets the correlation ID
  """
  def with_correlation_id(%__MODULE__{} = target, correlation_id) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("correlationId", Aspire.Runtime.encode(correlation_id))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCorrelationId", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_correlation_id/2`. Raises `Aspire.Error` on a failure.
  """
  def with_correlation_id!(%__MODULE__{} = target, correlation_id) do
    Aspire.Runtime.ok!(with_correlation_id(target, correlation_id))
  end

  @doc """
  Sets the created timestamp
  """
  def with_created_at(%__MODULE__{} = target, created_at) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("createdAt", Aspire.Runtime.encode(created_at))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCreatedAt", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_created_at/2`. Raises `Aspire.Error` on a failure.
  """
  def with_created_at!(%__MODULE__{} = target, created_at) do
    Aspire.Runtime.ok!(with_created_at(target, created_at))
  end

  @doc """
  Adds a data volume

  ## Options

    * `:name`
  """
  def with_data_volume(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:name], "Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource.with_data_volume/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt("name", opts, :name)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withDataVolume", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource}, transport)
  end

  @doc """
  The same as `with_data_volume/2`. Raises `Aspire.Error` on a failure.
  """
  def with_data_volume!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_data_volume(target, opts))
  end

  @doc """
  Adds a dependency on another resource
  """
  def with_dependency(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withDependency", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_dependency/2`. Raises `Aspire.Error` on a failure.
  """
  def with_dependency!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(with_dependency(target, dependency))
  end

  @doc """
  Sets the endpoints
  """
  def with_endpoints(%__MODULE__{} = target, endpoints) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpoints", Aspire.Runtime.encode(endpoints))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withEndpoints", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_endpoints/2`. Raises `Aspire.Error` on a failure.
  """
  def with_endpoints!(%__MODULE__{} = target, endpoints) do
    Aspire.Runtime.ok!(with_endpoints(target, endpoints))
  end

  @doc """
  Sets environment variables
  """
  def with_environment_variables(%__MODULE__{} = target, variables) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("variables", Aspire.Runtime.encode(variables))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withEnvironmentVariables", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithEnvironment}, transport)
  end

  @doc """
  The same as `with_environment_variables/2`. Raises `Aspire.Error` on a failure.
  """
  def with_environment_variables!(%__MODULE__{} = target, variables) do
    Aspire.Runtime.ok!(with_environment_variables(target, variables))
  end

  @doc """
  Configures a named endpoint
  """
  def with_merge_endpoint(%__MODULE__{} = target, endpoint_name, port) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpointName", Aspire.Runtime.encode(endpoint_name))
      |> Map.put("port", Aspire.Runtime.encode(port))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeEndpoint", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_endpoint/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_endpoint!(%__MODULE__{} = target, endpoint_name, port) do
    Aspire.Runtime.ok!(with_merge_endpoint(target, endpoint_name, port))
  end

  @doc """
  Configures a named endpoint with scheme
  """
  def with_merge_endpoint_scheme(%__MODULE__{} = target, endpoint_name, port, scheme) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpointName", Aspire.Runtime.encode(endpoint_name))
      |> Map.put("port", Aspire.Runtime.encode(port))
      |> Map.put("scheme", Aspire.Runtime.encode(scheme))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeEndpointScheme", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_endpoint_scheme/4`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_endpoint_scheme!(%__MODULE__{} = target, endpoint_name, port, scheme) do
    Aspire.Runtime.ok!(with_merge_endpoint_scheme(target, endpoint_name, port, scheme))
  end

  @doc """
  Adds a label to the resource
  """
  def with_merge_label(%__MODULE__{} = target, label) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("label", Aspire.Runtime.encode(label))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLabel", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_label/2`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_label!(%__MODULE__{} = target, label) do
    Aspire.Runtime.ok!(with_merge_label(target, label))
  end

  @doc """
  Adds a categorized label to the resource
  """
  def with_merge_label_categorized(%__MODULE__{} = target, label, category) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("label", Aspire.Runtime.encode(label))
      |> Map.put("category", Aspire.Runtime.encode(category))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLabelCategorized", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_label_categorized/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_label_categorized!(%__MODULE__{} = target, label, category) do
    Aspire.Runtime.ok!(with_merge_label_categorized(target, label, category))
  end

  @doc """
  Configures resource logging

  ## Options

    * `:enable_console`
    * `:max_files`
  """
  def with_merge_logging(%__MODULE__{} = target, log_level, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:enable_console, :max_files], "Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource.with_merge_logging/3")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("logLevel", Aspire.Runtime.encode(log_level))
      |> Aspire.Runtime.put_opt("enableConsole", opts, :enable_console)
      |> Aspire.Runtime.put_opt("maxFiles", opts, :max_files)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLogging", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_logging/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_logging!(%__MODULE__{} = target, log_level, opts \\ []) do
    Aspire.Runtime.ok!(with_merge_logging(target, log_level, opts))
  end

  @doc """
  Configures resource logging with file path

  ## Options

    * `:enable_console`
    * `:max_files`
  """
  def with_merge_logging_path(%__MODULE__{} = target, log_level, log_path, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:enable_console, :max_files], "Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource.with_merge_logging_path/4")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("logLevel", Aspire.Runtime.encode(log_level))
      |> Map.put("logPath", Aspire.Runtime.encode(log_path))
      |> Aspire.Runtime.put_opt("enableConsole", opts, :enable_console)
      |> Aspire.Runtime.put_opt("maxFiles", opts, :max_files)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLoggingPath", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_logging_path/4`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_logging_path!(%__MODULE__{} = target, log_level, log_path, opts \\ []) do
    Aspire.Runtime.ok!(with_merge_logging_path(target, log_level, log_path, opts))
  end

  @doc """
  Configures a route
  """
  def with_merge_route(%__MODULE__{} = target, path, method, handler, priority) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("path", Aspire.Runtime.encode(path))
      |> Map.put("method", Aspire.Runtime.encode(method))
      |> Map.put("handler", Aspire.Runtime.encode(handler))
      |> Map.put("priority", Aspire.Runtime.encode(priority))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeRoute", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_route/5`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_route!(%__MODULE__{} = target, path, method, handler, priority) do
    Aspire.Runtime.ok!(with_merge_route(target, path, method, handler, priority))
  end

  @doc """
  Configures a route with middleware
  """
  def with_merge_route_middleware(%__MODULE__{} = target, path, method, handler, priority, middleware) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("path", Aspire.Runtime.encode(path))
      |> Map.put("method", Aspire.Runtime.encode(method))
      |> Map.put("handler", Aspire.Runtime.encode(handler))
      |> Map.put("priority", Aspire.Runtime.encode(priority))
      |> Map.put("middleware", Aspire.Runtime.encode(middleware))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeRouteMiddleware", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_route_middleware/6`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_route_middleware!(%__MODULE__{} = target, path, method, handler, priority, middleware) do
    Aspire.Runtime.ok!(with_merge_route_middleware(target, path, method, handler, priority, middleware))
  end

  @doc """
  Sets the modified timestamp
  """
  def with_modified_at(%__MODULE__{} = target, modified_at) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("modifiedAt", Aspire.Runtime.encode(modified_at))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withModifiedAt", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_modified_at/2`. Raises `Aspire.Error` on a failure.
  """
  def with_modified_at!(%__MODULE__{} = target, modified_at) do
    Aspire.Runtime.ok!(with_modified_at(target, modified_at))
  end

  @doc """
  Configures with nested DTO
  """
  def with_nested_config(%__MODULE__{} = target, config) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("config", Aspire.Runtime.encode(config))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withNestedConfig", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_nested_config/2`. Raises `Aspire.Error` on a failure.
  """
  def with_nested_config!(%__MODULE__{} = target, config) do
    Aspire.Runtime.ok!(with_nested_config(target, config))
  end

  @doc """
  Configures with optional callback

  ## Options

    * `:callback`
  """
  def with_optional_callback(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:callback], "Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource.with_optional_callback/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt_raw("callback", opts, :callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withOptionalCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_optional_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def with_optional_callback!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_optional_callback(target, opts))
  end

  @doc """
  Adds an optional string parameter

  ## Options

    * `:value`
    * `:enabled`
  """
  def with_optional_string(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:value, :enabled], "Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource.with_optional_string/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt("value", opts, :value)
      |> Aspire.Runtime.put_opt("enabled", opts, :enabled)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withOptionalString", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_optional_string/2`. Raises `Aspire.Error` on a failure.
  """
  def with_optional_string!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_optional_string(target, opts))
  end

  @doc """
  Sets the resource status
  """
  def with_status(%__MODULE__{} = target, status) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("status", Aspire.Runtime.encode_enum!(status, Aspire.Enums.TestResourceStatus))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withStatus", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_status/2`. Raises `Aspire.Error` on a failure.
  """
  def with_status!(%__MODULE__{} = target, status) do
    Aspire.Runtime.ok!(with_status(target, status))
  end

  @doc """
  Adds a dependency from a string or another resource
  """
  def with_union_dependency(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withUnionDependency", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_union_dependency/2`. Raises `Aspire.Error` on a failure.
  """
  def with_union_dependency!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(with_union_dependency(target, dependency))
  end

  @doc """
  Adds validation callback
  """
  def with_validator(%__MODULE__{} = target, validator) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("validator", validator)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withValidator", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_validator/2`. Raises `Aspire.Error` on a failure.
  """
  def with_validator!(%__MODULE__{} = target, validator) do
    Aspire.Runtime.ok!(with_validator(target, validator))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestEnvironmentContext do
  @moduledoc """
  Test environment context used in callbacks. Verifies property-like object pattern (ctx.name.get(), ctx.name.set()).
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Gets the Description property
  """
  def description(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `description/1`. Raises `Aspire.Error` on a failure.
  """
  def description!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(description(target))
  end

  @doc """
  Gets the Name property
  """
  def name(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `name/1`. Raises `Aspire.Error` on a failure.
  """
  def name!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(name(target))
  end

  @doc """
  Gets the Priority property
  """
  def priority(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `priority/1`. Raises `Aspire.Error` on a failure.
  """
  def priority!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(priority(target))
  end

  @doc """
  Sets the Description property
  """
  def set_description(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestEnvironmentContext}, transport)
  end

  @doc """
  The same as `set_description/2`. Raises `Aspire.Error` on a failure.
  """
  def set_description!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_description(target, value))
  end

  @doc """
  Sets the Name property
  """
  def set_name(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestEnvironmentContext}, transport)
  end

  @doc """
  The same as `set_name/2`. Raises `Aspire.Error` on a failure.
  """
  def set_name!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_name(target, value))
  end

  @doc """
  Sets the Priority property
  """
  def set_priority(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestEnvironmentContext}, transport)
  end

  @doc """
  The same as `set_priority/2`. Raises `Aspire.Error` on a failure.
  """
  def set_priority!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_priority(target, value))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestMutableCollectionContext do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestMutableCollectionContext` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Gets the Counts property
  """
  def counts(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.counts", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `counts/1`. Raises `Aspire.Error` on a failure.
  """
  def counts!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(counts(target))
  end

  @doc """
  Sets the Counts property
  """
  def set_counts(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.setCounts", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestMutableCollectionContext}, transport)
  end

  @doc """
  The same as `set_counts/2`. Raises `Aspire.Error` on a failure.
  """
  def set_counts!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_counts(target, value))
  end

  @doc """
  Sets the Tags property
  """
  def set_tags(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.setTags", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestMutableCollectionContext}, transport)
  end

  @doc """
  The same as `set_tags/2`. Raises `Aspire.Error` on a failure.
  """
  def set_tags!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_tags(target, value))
  end

  @doc """
  Gets the Tags property
  """
  def tags(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestMutableCollectionContext.tags", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `tags/1`. Raises `Aspire.Error` on a failure.
  """
  def tags!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(tags(target))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestMutablePromiseCollisionResource do
  @moduledoc """
  A mutable-property-only resource used to verify that property setters do not require Promise wrappers.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Gets or sets the test value.
  """
  def set_value(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/ITestMutablePromiseCollisionResource.setValue", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestMutablePromiseCollisionResource}, transport)
  end

  @doc """
  The same as `set_value/2`. Raises `Aspire.Error` on a failure.
  """
  def set_value!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_value(target, value))
  end

  @doc """
  Gets or sets the test value.
  """
  def value(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/ITestMutablePromiseCollisionResource.value", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `value/1`. Raises `Aspire.Error` on a failure.
  """
  def value!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(value(target))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestMutablePromiseCollisionResourcePromise do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestMutablePromiseCollisionResourcePromise` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestPromiseCollisionResource do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResource` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestPromiseCollisionResourcePromise do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestPromiseCollisionResourcePromise` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end


# aspire_generated_2.ex - Generated Aspire modules
# GENERATED CODE - DO NOT EDIT

defmodule Aspire.CodeGeneration.Elixir.Tests.TestRedisResource do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Adds a child database to a test Redis resource

  ## Options

    * `:database_name`
  """
  def add_test_child_database(%__MODULE__{} = target, name, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:database_name], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.add_test_child_database/3")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("name", Aspire.Runtime.encode(name))
      |> Aspire.Runtime.put_opt("databaseName", opts, :database_name)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/addTestChildDatabase", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestDatabaseResource}, transport)
  end

  @doc """
  The same as `add_test_child_database/3`. Raises `Aspire.Error` on a failure.
  """
  def add_test_child_database!(%__MODULE__{} = target, name, opts \\ []) do
    Aspire.Runtime.ok!(add_test_child_database(target, name, opts))
  end

  @doc """
  Gets the endpoints
  """
  def get_endpoints(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"builder" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/getEndpoints", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `get_endpoints/1`. Raises `Aspire.Error` on a failure.
  """
  def get_endpoints!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(get_endpoints(target))
  end

  @doc """
  Gets the metadata for the resource
  """
  def get_metadata(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"builder" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/getMetadata", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `get_metadata/1`. Raises `Aspire.Error` on a failure.
  """
  def get_metadata!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(get_metadata(target))
  end

  @doc """
  Gets the status of the resource asynchronously

  ## Options

    * `:cancellation_token`
  """
  def get_status_async(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:cancellation_token], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.get_status_async/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt("cancellationToken", opts, :cancellation_token)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/getStatusAsync", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `get_status_async/2`. Raises `Aspire.Error` on a failure.
  """
  def get_status_async!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(get_status_async(target, opts))
  end

  @doc """
  Gets the tags for the resource
  """
  def get_tags(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"builder" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/getTags", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `get_tags/1`. Raises `Aspire.Error` on a failure.
  """
  def get_tags!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(get_tags(target))
  end

  @doc """
  Waits for another resource (test version)
  """
  def test_wait_for(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/testWaitFor", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `test_wait_for/2`. Raises `Aspire.Error` on a failure.
  """
  def test_wait_for!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(test_wait_for(target, dependency))
  end

  @doc """
  Configures environment with callback (test version)
  """
  def test_with_environment_callback(%__MODULE__{} = target, callback) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("callback", callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/testWithEnvironmentCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithEnvironment}, transport)
  end

  @doc """
  The same as `test_with_environment_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def test_with_environment_callback!(%__MODULE__{} = target, callback) do
    Aspire.Runtime.ok!(test_with_environment_callback(target, callback))
  end

  @doc """
  Waits for the resource to be ready

  ## Options

    * `:cancellation_token`
  """
  def wait_for_ready_async(%__MODULE__{} = target, timeout, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:cancellation_token], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.wait_for_ready_async/3")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("timeout", Aspire.Runtime.encode(timeout))
      |> Aspire.Runtime.put_opt("cancellationToken", opts, :cancellation_token)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/waitForReadyAsync", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `wait_for_ready_async/3`. Raises `Aspire.Error` on a failure.
  """
  def wait_for_ready_async!(%__MODULE__{} = target, timeout, opts \\ []) do
    Aspire.Runtime.ok!(wait_for_ready_async(target, timeout, opts))
  end

  @doc """
  Performs a cancellable operation
  """
  def with_cancellable_operation(%__MODULE__{} = target, operation) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("operation", operation)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCancellableOperation", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_cancellable_operation/2`. Raises `Aspire.Error` on a failure.
  """
  def with_cancellable_operation!(%__MODULE__{} = target, operation) do
    Aspire.Runtime.ok!(with_cancellable_operation(target, operation))
  end

  @doc """
  Configures a Redis resource with the concrete vault resource as a parameter.
  """
  def with_concrete_vault_resource(%__MODULE__{} = target, resource) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("resource", Aspire.Runtime.encode(resource))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withConcreteVaultResource", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_concrete_vault_resource/2`. Raises `Aspire.Error` on a failure.
  """
  def with_concrete_vault_resource!(%__MODULE__{} = target, resource) do
    Aspire.Runtime.ok!(with_concrete_vault_resource(target, resource))
  end

  @doc """
  Configures the resource with a DTO
  """
  def with_config(%__MODULE__{} = target, config) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("config", Aspire.Runtime.encode(config))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withConfig", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_config/2`. Raises `Aspire.Error` on a failure.
  """
  def with_config!(%__MODULE__{} = target, config) do
    Aspire.Runtime.ok!(with_config(target, config))
  end

  @doc """
  Sets the connection string using a reference expression
  """
  def with_connection_string(%__MODULE__{} = target, connection_string) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("connectionString", Aspire.Runtime.encode(connection_string))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withConnectionString", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithConnectionString}, transport)
  end

  @doc """
  The same as `with_connection_string/2`. Raises `Aspire.Error` on a failure.
  """
  def with_connection_string!(%__MODULE__{} = target, connection_string) do
    Aspire.Runtime.ok!(with_connection_string(target, connection_string))
  end

  @doc """
  Sets connection string using direct interface target
  """
  def with_connection_string_direct(%__MODULE__{} = target, connection_string) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("connectionString", Aspire.Runtime.encode(connection_string))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withConnectionStringDirect", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithConnectionString}, transport)
  end

  @doc """
  The same as `with_connection_string_direct/2`. Raises `Aspire.Error` on a failure.
  """
  def with_connection_string_direct!(%__MODULE__{} = target, connection_string) do
    Aspire.Runtime.ok!(with_connection_string_direct(target, connection_string))
  end

  @doc """
  Sets the correlation ID
  """
  def with_correlation_id(%__MODULE__{} = target, correlation_id) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("correlationId", Aspire.Runtime.encode(correlation_id))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCorrelationId", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_correlation_id/2`. Raises `Aspire.Error` on a failure.
  """
  def with_correlation_id!(%__MODULE__{} = target, correlation_id) do
    Aspire.Runtime.ok!(with_correlation_id(target, correlation_id))
  end

  @doc """
  Sets the created timestamp
  """
  def with_created_at(%__MODULE__{} = target, created_at) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("createdAt", Aspire.Runtime.encode(created_at))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCreatedAt", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_created_at/2`. Raises `Aspire.Error` on a failure.
  """
  def with_created_at!(%__MODULE__{} = target, created_at) do
    Aspire.Runtime.ok!(with_created_at(target, created_at))
  end

  @doc """
  Adds a data volume with persistence

  ## Options

    * `:name`
    * `:is_read_only`
  """
  def with_data_volume(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:name, :is_read_only], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_data_volume/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt("name", opts, :name)
      |> Aspire.Runtime.put_opt("isReadOnly", opts, :is_read_only)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withDataVolume", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_data_volume/2`. Raises `Aspire.Error` on a failure.
  """
  def with_data_volume!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_data_volume(target, opts))
  end

  @doc """
  Adds a dependency on another resource
  """
  def with_dependency(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withDependency", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_dependency/2`. Raises `Aspire.Error` on a failure.
  """
  def with_dependency!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(with_dependency(target, dependency))
  end

  @doc """
  Sets the endpoints
  """
  def with_endpoints(%__MODULE__{} = target, endpoints) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpoints", Aspire.Runtime.encode(endpoints))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withEndpoints", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_endpoints/2`. Raises `Aspire.Error` on a failure.
  """
  def with_endpoints!(%__MODULE__{} = target, endpoints) do
    Aspire.Runtime.ok!(with_endpoints(target, endpoints))
  end

  @doc """
  Sets environment variables
  """
  def with_environment_variables(%__MODULE__{} = target, variables) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("variables", Aspire.Runtime.encode(variables))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withEnvironmentVariables", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithEnvironment}, transport)
  end

  @doc """
  The same as `with_environment_variables/2`. Raises `Aspire.Error` on a failure.
  """
  def with_environment_variables!(%__MODULE__{} = target, variables) do
    Aspire.Runtime.ok!(with_environment_variables(target, variables))
  end

  @doc """
  Configures a named endpoint
  """
  def with_merge_endpoint(%__MODULE__{} = target, endpoint_name, port) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpointName", Aspire.Runtime.encode(endpoint_name))
      |> Map.put("port", Aspire.Runtime.encode(port))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeEndpoint", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_endpoint/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_endpoint!(%__MODULE__{} = target, endpoint_name, port) do
    Aspire.Runtime.ok!(with_merge_endpoint(target, endpoint_name, port))
  end

  @doc """
  Configures a named endpoint with scheme
  """
  def with_merge_endpoint_scheme(%__MODULE__{} = target, endpoint_name, port, scheme) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpointName", Aspire.Runtime.encode(endpoint_name))
      |> Map.put("port", Aspire.Runtime.encode(port))
      |> Map.put("scheme", Aspire.Runtime.encode(scheme))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeEndpointScheme", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_endpoint_scheme/4`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_endpoint_scheme!(%__MODULE__{} = target, endpoint_name, port, scheme) do
    Aspire.Runtime.ok!(with_merge_endpoint_scheme(target, endpoint_name, port, scheme))
  end

  @doc """
  Adds a label to the resource
  """
  def with_merge_label(%__MODULE__{} = target, label) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("label", Aspire.Runtime.encode(label))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLabel", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_label/2`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_label!(%__MODULE__{} = target, label) do
    Aspire.Runtime.ok!(with_merge_label(target, label))
  end

  @doc """
  Adds a categorized label to the resource
  """
  def with_merge_label_categorized(%__MODULE__{} = target, label, category) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("label", Aspire.Runtime.encode(label))
      |> Map.put("category", Aspire.Runtime.encode(category))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLabelCategorized", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_label_categorized/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_label_categorized!(%__MODULE__{} = target, label, category) do
    Aspire.Runtime.ok!(with_merge_label_categorized(target, label, category))
  end

  @doc """
  Configures resource logging

  ## Options

    * `:enable_console`
    * `:max_files`
  """
  def with_merge_logging(%__MODULE__{} = target, log_level, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:enable_console, :max_files], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_merge_logging/3")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("logLevel", Aspire.Runtime.encode(log_level))
      |> Aspire.Runtime.put_opt("enableConsole", opts, :enable_console)
      |> Aspire.Runtime.put_opt("maxFiles", opts, :max_files)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLogging", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_logging/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_logging!(%__MODULE__{} = target, log_level, opts \\ []) do
    Aspire.Runtime.ok!(with_merge_logging(target, log_level, opts))
  end

  @doc """
  Configures resource logging with file path

  ## Options

    * `:enable_console`
    * `:max_files`
  """
  def with_merge_logging_path(%__MODULE__{} = target, log_level, log_path, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:enable_console, :max_files], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_merge_logging_path/4")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("logLevel", Aspire.Runtime.encode(log_level))
      |> Map.put("logPath", Aspire.Runtime.encode(log_path))
      |> Aspire.Runtime.put_opt("enableConsole", opts, :enable_console)
      |> Aspire.Runtime.put_opt("maxFiles", opts, :max_files)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLoggingPath", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_logging_path/4`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_logging_path!(%__MODULE__{} = target, log_level, log_path, opts \\ []) do
    Aspire.Runtime.ok!(with_merge_logging_path(target, log_level, log_path, opts))
  end

  @doc """
  Configures a route
  """
  def with_merge_route(%__MODULE__{} = target, path, method, handler, priority) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("path", Aspire.Runtime.encode(path))
      |> Map.put("method", Aspire.Runtime.encode(method))
      |> Map.put("handler", Aspire.Runtime.encode(handler))
      |> Map.put("priority", Aspire.Runtime.encode(priority))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeRoute", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_route/5`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_route!(%__MODULE__{} = target, path, method, handler, priority) do
    Aspire.Runtime.ok!(with_merge_route(target, path, method, handler, priority))
  end

  @doc """
  Configures a route with middleware
  """
  def with_merge_route_middleware(%__MODULE__{} = target, path, method, handler, priority, middleware) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("path", Aspire.Runtime.encode(path))
      |> Map.put("method", Aspire.Runtime.encode(method))
      |> Map.put("handler", Aspire.Runtime.encode(handler))
      |> Map.put("priority", Aspire.Runtime.encode(priority))
      |> Map.put("middleware", Aspire.Runtime.encode(middleware))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeRouteMiddleware", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_route_middleware/6`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_route_middleware!(%__MODULE__{} = target, path, method, handler, priority, middleware) do
    Aspire.Runtime.ok!(with_merge_route_middleware(target, path, method, handler, priority, middleware))
  end

  @doc """
  Sets the modified timestamp
  """
  def with_modified_at(%__MODULE__{} = target, modified_at) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("modifiedAt", Aspire.Runtime.encode(modified_at))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withModifiedAt", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_modified_at/2`. Raises `Aspire.Error` on a failure.
  """
  def with_modified_at!(%__MODULE__{} = target, modified_at) do
    Aspire.Runtime.ok!(with_modified_at(target, modified_at))
  end

  @doc """
  Tests multi-param callback destructuring
  """
  def with_multi_param_handle_callback(%__MODULE__{} = target, callback) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("callback", callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMultiParamHandleCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_multi_param_handle_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def with_multi_param_handle_callback!(%__MODULE__{} = target, callback) do
    Aspire.Runtime.ok!(with_multi_param_handle_callback(target, callback))
  end

  @doc """
  Configures a Redis resource with mutable-property and parameter-only resources whose generated names collide.
  """
  def with_mutable_promise_collision_resources(%__MODULE__{} = target, resource, resource_promise) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("resource", Aspire.Runtime.encode(resource))
      |> Map.put("resourcePromise", Aspire.Runtime.encode(resource_promise))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMutablePromiseCollisionResources", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_mutable_promise_collision_resources/3`. Raises `Aspire.Error` on a failure.
  """
  def with_mutable_promise_collision_resources!(%__MODULE__{} = target, resource, resource_promise) do
    Aspire.Runtime.ok!(with_mutable_promise_collision_resources(target, resource, resource_promise))
  end

  @doc """
  Configures with nested DTO
  """
  def with_nested_config(%__MODULE__{} = target, config) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("config", Aspire.Runtime.encode(config))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withNestedConfig", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_nested_config/2`. Raises `Aspire.Error` on a failure.
  """
  def with_nested_config!(%__MODULE__{} = target, config) do
    Aspire.Runtime.ok!(with_nested_config(target, config))
  end

  @doc """
  Configures with optional callback

  ## Options

    * `:callback`
  """
  def with_optional_callback(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:callback], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_optional_callback/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt_raw("callback", opts, :callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withOptionalCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_optional_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def with_optional_callback!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_optional_callback(target, opts))
  end

  @doc """
  Adds an optional string parameter

  ## Options

    * `:value`
    * `:enabled`
  """
  def with_optional_string(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:value, :enabled], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_optional_string/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt("value", opts, :value)
      |> Aspire.Runtime.put_opt("enabled", opts, :enabled)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withOptionalString", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_optional_string/2`. Raises `Aspire.Error` on a failure.
  """
  def with_optional_string!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_optional_string(target, opts))
  end

  @doc """
  Configures the Redis resource with persistence

  ## Options

    * `:mode`
  """
  def with_persistence(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:mode], "Aspire.CodeGeneration.Elixir.Tests.TestRedisResource.with_persistence/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt_enum!("mode", opts, :mode, Aspire.Enums.TestPersistenceMode)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withPersistence", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_persistence/2`. Raises `Aspire.Error` on a failure.
  """
  def with_persistence!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_persistence(target, opts))
  end

  @doc """
  Configures a Redis resource with parameter-only resources whose generated names collide.
  """
  def with_promise_collision_resources(%__MODULE__{} = target, resource, resource_promise) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("resource", Aspire.Runtime.encode(resource))
      |> Map.put("resourcePromise", Aspire.Runtime.encode(resource_promise))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withPromiseCollisionResources", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_promise_collision_resources/3`. Raises `Aspire.Error` on a failure.
  """
  def with_promise_collision_resources!(%__MODULE__{} = target, resource, resource_promise) do
    Aspire.Runtime.ok!(with_promise_collision_resources(target, resource, resource_promise))
  end

  @doc """
  Redis-specific configuration
  """
  def with_redis_specific(%__MODULE__{} = target, option) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("option", Aspire.Runtime.encode(option))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withRedisSpecific", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `with_redis_specific/2`. Raises `Aspire.Error` on a failure.
  """
  def with_redis_specific!(%__MODULE__{} = target, option) do
    Aspire.Runtime.ok!(with_redis_specific(target, option))
  end

  @doc """
  Sets the resource status
  """
  def with_status(%__MODULE__{} = target, status) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("status", Aspire.Runtime.encode_enum!(status, Aspire.Enums.TestResourceStatus))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withStatus", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_status/2`. Raises `Aspire.Error` on a failure.
  """
  def with_status!(%__MODULE__{} = target, status) do
    Aspire.Runtime.ok!(with_status(target, status))
  end

  @doc """
  Adds a dependency from a string or another resource
  """
  def with_union_dependency(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withUnionDependency", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_union_dependency/2`. Raises `Aspire.Error` on a failure.
  """
  def with_union_dependency!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(with_union_dependency(target, dependency))
  end

  @doc """
  Adds validation callback
  """
  def with_validator(%__MODULE__{} = target, validator) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("validator", validator)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withValidator", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_validator/2`. Raises `Aspire.Error` on a failure.
  """
  def with_validator!(%__MODULE__{} = target, validator) do
    Aspire.Runtime.ok!(with_validator(target, validator))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestResourceContext do
  @moduledoc """
  Test context type with exposed instance methods. Verifies [AspireExport(ExposeMethods=true)] generates async methods.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Instance method that should be exposed as async method.
  """
  def get_value_async(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `get_value_async/1`. Raises `Aspire.Error` on a failure.
  """
  def get_value_async!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(get_value_async(target))
  end

  @doc """
  Gets the Name property
  """
  def name(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `name/1`. Raises `Aspire.Error` on a failure.
  """
  def name!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(name(target))
  end

  @doc """
  Sets the Name property
  """
  def set_name(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestResourceContext}, transport)
  end

  @doc """
  The same as `set_name/2`. Raises `Aspire.Error` on a failure.
  """
  def set_name!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_name(target, value))
  end

  @doc """
  Sets the Value property
  """
  def set_value(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestResourceContext}, transport)
  end

  @doc """
  The same as `set_value/2`. Raises `Aspire.Error` on a failure.
  """
  def set_value!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_value(target, value))
  end

  @doc """
  Instance method with parameter.
  """
  def set_value_async(%__MODULE__{} = target, value) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"context" => Aspire.Runtime.handle_of(target)}
      |> Map.put("value", Aspire.Runtime.encode(value))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `set_value_async/2`. Raises `Aspire.Error` on a failure.
  """
  def set_value_async!(%__MODULE__{} = target, value) do
    Aspire.Runtime.ok!(set_value_async(target, value))
  end

  @doc """
  Instance method with return type.
  """
  def validate_async(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `validate_async/1`. Raises `Aspire.Error` on a failure.
  """
  def validate_async!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(validate_async(target))
  end

  @doc """
  Gets the Value property
  """
  def value(%__MODULE__{} = target) do
    transport = Aspire.Runtime.transport_of(target)

    args = %{"context" => Aspire.Runtime.handle_of(target)}

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value", args)
    |> Aspire.Runtime.result(nil, transport)
  end

  @doc """
  The same as `value/1`. Raises `Aspire.Error` on a failure.
  """
  def value!(%__MODULE__{} = target) do
    Aspire.Runtime.ok!(value(target))
  end
end

defmodule Aspire.CodeGeneration.Elixir.Tests.TestVaultResource do
  @moduledoc """
  A handle to `Aspire.Hosting.CodeGeneration.Elixir.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Waits for another resource (test version)
  """
  def test_wait_for(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/testWaitFor", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `test_wait_for/2`. Raises `Aspire.Error` on a failure.
  """
  def test_wait_for!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(test_wait_for(target, dependency))
  end

  @doc """
  Configures environment with callback (test version)
  """
  def test_with_environment_callback(%__MODULE__{} = target, callback) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("callback", callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/testWithEnvironmentCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithEnvironment}, transport)
  end

  @doc """
  The same as `test_with_environment_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def test_with_environment_callback!(%__MODULE__{} = target, callback) do
    Aspire.Runtime.ok!(test_with_environment_callback(target, callback))
  end

  @doc """
  Performs a cancellable operation
  """
  def with_cancellable_operation(%__MODULE__{} = target, operation) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("operation", operation)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCancellableOperation", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_cancellable_operation/2`. Raises `Aspire.Error` on a failure.
  """
  def with_cancellable_operation!(%__MODULE__{} = target, operation) do
    Aspire.Runtime.ok!(with_cancellable_operation(target, operation))
  end

  @doc """
  Configures the resource with a DTO
  """
  def with_config(%__MODULE__{} = target, config) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("config", Aspire.Runtime.encode(config))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withConfig", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_config/2`. Raises `Aspire.Error` on a failure.
  """
  def with_config!(%__MODULE__{} = target, config) do
    Aspire.Runtime.ok!(with_config(target, config))
  end

  @doc """
  Sets the correlation ID
  """
  def with_correlation_id(%__MODULE__{} = target, correlation_id) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("correlationId", Aspire.Runtime.encode(correlation_id))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCorrelationId", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_correlation_id/2`. Raises `Aspire.Error` on a failure.
  """
  def with_correlation_id!(%__MODULE__{} = target, correlation_id) do
    Aspire.Runtime.ok!(with_correlation_id(target, correlation_id))
  end

  @doc """
  Sets the created timestamp
  """
  def with_created_at(%__MODULE__{} = target, created_at) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("createdAt", Aspire.Runtime.encode(created_at))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withCreatedAt", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_created_at/2`. Raises `Aspire.Error` on a failure.
  """
  def with_created_at!(%__MODULE__{} = target, created_at) do
    Aspire.Runtime.ok!(with_created_at(target, created_at))
  end

  @doc """
  Adds a dependency on another resource
  """
  def with_dependency(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withDependency", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_dependency/2`. Raises `Aspire.Error` on a failure.
  """
  def with_dependency!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(with_dependency(target, dependency))
  end

  @doc """
  Sets the endpoints
  """
  def with_endpoints(%__MODULE__{} = target, endpoints) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpoints", Aspire.Runtime.encode(endpoints))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withEndpoints", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_endpoints/2`. Raises `Aspire.Error` on a failure.
  """
  def with_endpoints!(%__MODULE__{} = target, endpoints) do
    Aspire.Runtime.ok!(with_endpoints(target, endpoints))
  end

  @doc """
  Sets environment variables
  """
  def with_environment_variables(%__MODULE__{} = target, variables) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("variables", Aspire.Runtime.encode(variables))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withEnvironmentVariables", args)
    |> Aspire.Runtime.result({:handle, Aspire.ResourceWithEnvironment}, transport)
  end

  @doc """
  The same as `with_environment_variables/2`. Raises `Aspire.Error` on a failure.
  """
  def with_environment_variables!(%__MODULE__{} = target, variables) do
    Aspire.Runtime.ok!(with_environment_variables(target, variables))
  end

  @doc """
  Configures a named endpoint
  """
  def with_merge_endpoint(%__MODULE__{} = target, endpoint_name, port) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpointName", Aspire.Runtime.encode(endpoint_name))
      |> Map.put("port", Aspire.Runtime.encode(port))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeEndpoint", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_endpoint/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_endpoint!(%__MODULE__{} = target, endpoint_name, port) do
    Aspire.Runtime.ok!(with_merge_endpoint(target, endpoint_name, port))
  end

  @doc """
  Configures a named endpoint with scheme
  """
  def with_merge_endpoint_scheme(%__MODULE__{} = target, endpoint_name, port, scheme) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("endpointName", Aspire.Runtime.encode(endpoint_name))
      |> Map.put("port", Aspire.Runtime.encode(port))
      |> Map.put("scheme", Aspire.Runtime.encode(scheme))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeEndpointScheme", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_endpoint_scheme/4`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_endpoint_scheme!(%__MODULE__{} = target, endpoint_name, port, scheme) do
    Aspire.Runtime.ok!(with_merge_endpoint_scheme(target, endpoint_name, port, scheme))
  end

  @doc """
  Adds a label to the resource
  """
  def with_merge_label(%__MODULE__{} = target, label) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("label", Aspire.Runtime.encode(label))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLabel", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_label/2`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_label!(%__MODULE__{} = target, label) do
    Aspire.Runtime.ok!(with_merge_label(target, label))
  end

  @doc """
  Adds a categorized label to the resource
  """
  def with_merge_label_categorized(%__MODULE__{} = target, label, category) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("label", Aspire.Runtime.encode(label))
      |> Map.put("category", Aspire.Runtime.encode(category))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLabelCategorized", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_label_categorized/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_label_categorized!(%__MODULE__{} = target, label, category) do
    Aspire.Runtime.ok!(with_merge_label_categorized(target, label, category))
  end

  @doc """
  Configures resource logging

  ## Options

    * `:enable_console`
    * `:max_files`
  """
  def with_merge_logging(%__MODULE__{} = target, log_level, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:enable_console, :max_files], "Aspire.CodeGeneration.Elixir.Tests.TestVaultResource.with_merge_logging/3")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("logLevel", Aspire.Runtime.encode(log_level))
      |> Aspire.Runtime.put_opt("enableConsole", opts, :enable_console)
      |> Aspire.Runtime.put_opt("maxFiles", opts, :max_files)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLogging", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_logging/3`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_logging!(%__MODULE__{} = target, log_level, opts \\ []) do
    Aspire.Runtime.ok!(with_merge_logging(target, log_level, opts))
  end

  @doc """
  Configures resource logging with file path

  ## Options

    * `:enable_console`
    * `:max_files`
  """
  def with_merge_logging_path(%__MODULE__{} = target, log_level, log_path, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:enable_console, :max_files], "Aspire.CodeGeneration.Elixir.Tests.TestVaultResource.with_merge_logging_path/4")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("logLevel", Aspire.Runtime.encode(log_level))
      |> Map.put("logPath", Aspire.Runtime.encode(log_path))
      |> Aspire.Runtime.put_opt("enableConsole", opts, :enable_console)
      |> Aspire.Runtime.put_opt("maxFiles", opts, :max_files)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeLoggingPath", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_logging_path/4`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_logging_path!(%__MODULE__{} = target, log_level, log_path, opts \\ []) do
    Aspire.Runtime.ok!(with_merge_logging_path(target, log_level, log_path, opts))
  end

  @doc """
  Configures a route
  """
  def with_merge_route(%__MODULE__{} = target, path, method, handler, priority) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("path", Aspire.Runtime.encode(path))
      |> Map.put("method", Aspire.Runtime.encode(method))
      |> Map.put("handler", Aspire.Runtime.encode(handler))
      |> Map.put("priority", Aspire.Runtime.encode(priority))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeRoute", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_route/5`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_route!(%__MODULE__{} = target, path, method, handler, priority) do
    Aspire.Runtime.ok!(with_merge_route(target, path, method, handler, priority))
  end

  @doc """
  Configures a route with middleware
  """
  def with_merge_route_middleware(%__MODULE__{} = target, path, method, handler, priority, middleware) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("path", Aspire.Runtime.encode(path))
      |> Map.put("method", Aspire.Runtime.encode(method))
      |> Map.put("handler", Aspire.Runtime.encode(handler))
      |> Map.put("priority", Aspire.Runtime.encode(priority))
      |> Map.put("middleware", Aspire.Runtime.encode(middleware))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withMergeRouteMiddleware", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_merge_route_middleware/6`. Raises `Aspire.Error` on a failure.
  """
  def with_merge_route_middleware!(%__MODULE__{} = target, path, method, handler, priority, middleware) do
    Aspire.Runtime.ok!(with_merge_route_middleware(target, path, method, handler, priority, middleware))
  end

  @doc """
  Sets the modified timestamp
  """
  def with_modified_at(%__MODULE__{} = target, modified_at) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("modifiedAt", Aspire.Runtime.encode(modified_at))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withModifiedAt", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_modified_at/2`. Raises `Aspire.Error` on a failure.
  """
  def with_modified_at!(%__MODULE__{} = target, modified_at) do
    Aspire.Runtime.ok!(with_modified_at(target, modified_at))
  end

  @doc """
  Configures with nested DTO
  """
  def with_nested_config(%__MODULE__{} = target, config) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("config", Aspire.Runtime.encode(config))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withNestedConfig", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_nested_config/2`. Raises `Aspire.Error` on a failure.
  """
  def with_nested_config!(%__MODULE__{} = target, config) do
    Aspire.Runtime.ok!(with_nested_config(target, config))
  end

  @doc """
  Configures with optional callback

  ## Options

    * `:callback`
  """
  def with_optional_callback(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:callback], "Aspire.CodeGeneration.Elixir.Tests.TestVaultResource.with_optional_callback/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt_raw("callback", opts, :callback)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withOptionalCallback", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_optional_callback/2`. Raises `Aspire.Error` on a failure.
  """
  def with_optional_callback!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_optional_callback(target, opts))
  end

  @doc """
  Adds an optional string parameter

  ## Options

    * `:value`
    * `:enabled`
  """
  def with_optional_string(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:value, :enabled], "Aspire.CodeGeneration.Elixir.Tests.TestVaultResource.with_optional_string/2")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Aspire.Runtime.put_opt("value", opts, :value)
      |> Aspire.Runtime.put_opt("enabled", opts, :enabled)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withOptionalString", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_optional_string/2`. Raises `Aspire.Error` on a failure.
  """
  def with_optional_string!(%__MODULE__{} = target, opts \\ []) do
    Aspire.Runtime.ok!(with_optional_string(target, opts))
  end

  @doc """
  Sets the resource status
  """
  def with_status(%__MODULE__{} = target, status) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("status", Aspire.Runtime.encode_enum!(status, Aspire.Enums.TestResourceStatus))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withStatus", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_status/2`. Raises `Aspire.Error` on a failure.
  """
  def with_status!(%__MODULE__{} = target, status) do
    Aspire.Runtime.ok!(with_status(target, status))
  end

  @doc """
  Adds a dependency from a string or another resource
  """
  def with_union_dependency(%__MODULE__{} = target, dependency) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("dependency", Aspire.Runtime.encode(dependency))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withUnionDependency", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_union_dependency/2`. Raises `Aspire.Error` on a failure.
  """
  def with_union_dependency!(%__MODULE__{} = target, dependency) do
    Aspire.Runtime.ok!(with_union_dependency(target, dependency))
  end

  @doc """
  Adds validation callback
  """
  def with_validator(%__MODULE__{} = target, validator) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("validator", validator)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withValidator", args)
    |> Aspire.Runtime.result({:handle, Aspire.Resource}, transport)
  end

  @doc """
  The same as `with_validator/2`. Raises `Aspire.Error` on a failure.
  """
  def with_validator!(%__MODULE__{} = target, validator) do
    Aspire.Runtime.ok!(with_validator(target, validator))
  end

  @doc """
  Configures vault using direct interface target
  """
  def with_vault_direct(%__MODULE__{} = target, option) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("option", Aspire.Runtime.encode(option))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/withVaultDirect", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.ITestVaultResource}, transport)
  end

  @doc """
  The same as `with_vault_direct/2`. Raises `Aspire.Error` on a failure.
  """
  def with_vault_direct!(%__MODULE__{} = target, option) do
    Aspire.Runtime.ok!(with_vault_direct(target, option))
  end
end

defmodule Aspire.DistributedApplicationBuilder do
  @moduledoc """
  A handle to `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}

  @doc """
  Adds a test Redis resource from ATS documentation.

  ## Options

    * `:port`
  """
  def add_test_redis(%__MODULE__{} = target, name, opts \\ []) do
    Aspire.Runtime.validate_opts!(opts, [:port], "Aspire.DistributedApplicationBuilder.add_test_redis/3")
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("name", Aspire.Runtime.encode(name))
      |> Aspire.Runtime.put_opt("port", opts, :port)

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/addTestRedis", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.TestRedisResource}, transport)
  end

  @doc """
  The same as `add_test_redis/3`. Raises `Aspire.Error` on a failure.
  """
  def add_test_redis!(%__MODULE__{} = target, name, opts \\ []) do
    Aspire.Runtime.ok!(add_test_redis(target, name, opts))
  end

  @doc """
  Adds a test vault resource
  """
  def add_test_vault(%__MODULE__{} = target, name) do
    transport = Aspire.Runtime.transport_of(target)

    args =
      %{"builder" => Aspire.Runtime.handle_of(target)}
      |> Map.put("name", Aspire.Runtime.encode(name))

    transport
    |> Aspire.Runtime.invoke("Aspire.Hosting.CodeGeneration.Elixir.Tests/addTestVault", args)
    |> Aspire.Runtime.result({:handle, Aspire.CodeGeneration.Elixir.Tests.ITestVaultResource}, transport)
  end

  @doc """
  The same as `add_test_vault/2`. Raises `Aspire.Error` on a failure.
  """
  def add_test_vault!(%__MODULE__{} = target, name) do
    Aspire.Runtime.ok!(add_test_vault(target, name))
  end
end

defmodule Aspire.ReferenceExpression do
  @moduledoc """
  A handle to `Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

defmodule Aspire.Resource do
  @moduledoc """
  A handle to `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

defmodule Aspire.ResourceWithConnectionString do
  @moduledoc """
  A handle to `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

defmodule Aspire.ResourceWithEnvironment do
  @moduledoc """
  A handle to `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment` in the AppHost.
  """

  @enforce_keys [:handle]
  defstruct [:handle, :transport]

  @type t :: %__MODULE__{handle: Aspire.Handle.t(), transport: GenServer.server() | nil}
end

