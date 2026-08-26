defmodule Worker do
  @moduledoc """
  Reads the Redis connection string that Aspire injects and gives the options to Redix.
  """

  @default_host "localhost"
  @default_port 6379

  @doc """
  Reads `ConnectionStrings__cache` and returns the Redix options.
  """
  @spec redix_options() :: keyword()
  def redix_options do
    "cache"
    |> connection_string()
    |> parse()
    |> with_certificate_bundle()
  end

  # Aspire puts its certificate authorities in the bundle that SSL_CERT_FILE names. Redix
  # must read that file, because the Erlang :ssl application does not read the variable.
  defp with_certificate_bundle(options) do
    with true <- options[:ssl] == true,
         bundle when is_binary(bundle) <- System.get_env("SSL_CERT_FILE"),
         {:ok, pem} <- File.read(bundle) do
      Keyword.put(options, :socket_opts,
        cacertfile: bundle,
        verify_fun: {&accept_trusted_self_signed/3, certificates(pem)}
      )
    else
      _ -> options
    end
  end

  defp certificates(pem) do
    pem
    |> :public_key.pem_decode()
    |> Enum.flat_map(fn
      {:Certificate, der, :not_encrypted} -> [der]
      _ -> []
    end)
  end

  # Aspire signs the Redis certificate with itself. The Erlang :ssl application refuses a
  # self-signed leaf certificate, even when the trust bundle holds the same certificate.
  # This function accepts the certificate when the bundle holds the same bytes.
  defp accept_trusted_self_signed(certificate, {:bad_cert, :selfsigned_peer}, trusted) do
    if :public_key.pkix_encode(:OTPCertificate, certificate, :otp) in trusted do
      {:valid, trusted}
    else
      {:fail, :selfsigned_peer}
    end
  end

  defp accept_trusted_self_signed(_certificate, {:bad_cert, reason}, _trusted),
    do: {:fail, reason}

  defp accept_trusted_self_signed(_certificate, {:extension, _extension}, trusted),
    do: {:unknown, trusted}

  defp accept_trusted_self_signed(_certificate, event, trusted)
       when event in [:valid, :valid_peer],
       do: {:valid, trusted}

  @doc """
  Reads the connection string of a resource. Aspire writes `ConnectionStrings__<name>`.
  """
  @spec connection_string(String.t()) :: String.t() | nil
  def connection_string(name), do: System.get_env("ConnectionStrings__#{name}")

  @doc """
  Parses a Redis connection string, for example `localhost:6379,password=secret,ssl=true`.
  """
  @spec parse(String.t() | nil) :: keyword()
  def parse(nil), do: [host: @default_host, port: @default_port]

  def parse(value) do
    [endpoint | options] = String.split(value, ",")

    Enum.reduce(options, endpoint(endpoint), fn option, acc ->
      case String.split(option, "=", parts: 2) do
        ["password", password] -> Keyword.put(acc, :password, password)
        ["ssl", flag] -> Keyword.put(acc, :ssl, String.downcase(flag) == "true")
        _ -> acc
      end
    end)
  end

  defp endpoint(value) do
    case String.split(String.trim(value), ":") do
      [host, port] -> [host: host, port: String.to_integer(port)]
      [host] -> [host: host, port: @default_port]
    end
  end
end
