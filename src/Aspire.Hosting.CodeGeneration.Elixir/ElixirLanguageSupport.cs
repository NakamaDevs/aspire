// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Elixir;

/// <summary>
/// Provides language support for Elixir AppHosts.
/// Implements scaffolding, detection, and runtime configuration.
/// </summary>
internal sealed class ElixirLanguageSupport : ILanguageSupport
{
    /// <summary>
    /// The language/runtime identifier for Elixir.
    /// </summary>
    private const string LanguageId = "elixir";

    private const string AppHostFileName = "apphost.exs";

    /// <summary>
    /// The code generation target language. This maps to the ICodeGenerator.Language property.
    /// </summary>
    private const string CodeGenTarget = "Elixir";

    private const string LanguageDisplayName = "Elixir";

    private static readonly string[] s_detectionPatterns = [AppHostFileName];

    /// <inheritdoc />
    public string Language => LanguageId;

    /// <inheritdoc />
    /// <remarks>
    /// Erlang's TLS stack reads the PEM bundle named by <c>SSL_CERT_FILE</c>, so the ASP.NET Core
    /// development certificate reaches every outbound connection the AppHost makes.
    /// </remarks>
    public string CertificateBundleEnvironmentVariable => "SSL_CERT_FILE";

    /// <inheritdoc />
    public Dictionary<string, string> Scaffold(ScaffoldRequest request)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        // The generated SDK loads through a single entry file. AtsElixirCodeGenerator emits
        // .aspire/modules/aspire.ex, and that file requires base.ex, transport.ex,
        // aspire_runtime.ex and the generated modules in the order they depend on each other.
        files[AppHostFileName] = """
            # Aspire Elixir AppHost
            # For more information, see: https://aspire.dev

            Code.require_file(".aspire/modules/aspire.ex", __DIR__)

            builder = Aspire.create_builder!()

            # Add your resources here, for example:
            # {:ok, cache} = Aspire.DistributedApplicationBuilder.add_container(builder, "cache", "redis:latest")
            # postgres = Aspire.DistributedApplicationBuilder.add_postgres!(builder, "db")

            builder
            |> Aspire.build!()
            |> Aspire.run!()
            """;

        // _build/ holds the artifacts of a `mix` project that sits beside the AppHost.
        // .aspire/ holds the generated SDK, which the CLI rewrites on every relevant change.
        files[".gitignore"] = """
            .aspire/
            _build/
            """;

        // Create apphost.run.json with random ports.
        // Use PortSeed if provided (for testing), otherwise use random.
        var random = request.PortSeed.HasValue
            ? new Random(request.PortSeed.Value)
            : Random.Shared;

        var ports = AppHostProfilePortGenerator.Generate(random);

        files["apphost.run.json"] = $$"""
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:{{ports.DashboardHttpsPort}};http://localhost:{{ports.DashboardHttpPort}}",
                  "environmentVariables": {
                    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:{{ports.OtlpHttpsPort}}",
                    "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:{{ports.ResourceServiceHttpsPort}}"
                  }
                }
              }
            }
            """;

        return files;
    }

    /// <inheritdoc />
    public DetectionResult Detect(string directoryPath)
    {
        var appHostPath = Path.Combine(directoryPath, AppHostFileName);
        if (!File.Exists(appHostPath))
        {
            return DetectionResult.NotFound;
        }

        return DetectionResult.Found(LanguageId, AppHostFileName);
    }

    /// <inheritdoc />
    public RuntimeSpec GetRuntimeSpec()
    {
        return new RuntimeSpec
        {
            Language = LanguageId,
            DisplayName = LanguageDisplayName,
            CodeGenLanguage = CodeGenTarget,
            DetectionPatterns = s_detectionPatterns,
            ExtensionLaunchCapability = LanguageId,
            CertificateBundleEnvironmentVariable = CertificateBundleEnvironmentVariable,
            // `elixir` compiles the script in memory on every launch, so there is no install,
            // no pre-execution build, and nothing to keep up to date between runs.
            InstallDependencies = null,
            PreExecute = null,
            // Watch mode arrives with M2.8.
            WatchExecute = null,
            Execute = new CommandSpec
            {
                Command = "elixir",
                Args = ["{appHostFile}"]
            }
        };
    }
}
