// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Dart;

/// <summary>
/// Provides language support for Dart AppHosts.
/// Implements scaffolding, detection, and runtime configuration.
/// </summary>
internal sealed class DartLanguageSupport : ILanguageSupport
{
    /// <summary>
    /// The language/runtime identifier for Dart.
    /// </summary>
    private const string LanguageId = "dart";

    private const string AppHostFileName = "apphost.dart";

    /// <summary>
    /// The Dart package manifest. <c>dart run</c> resolves packages through it, so the file is part
    /// of the scaffold and part of the detection rule.
    /// </summary>
    private const string PubspecFileName = "pubspec.yaml";

    /// <summary>
    /// The code generation target language. This maps to the ICodeGenerator.Language property.
    /// </summary>
    private const string CodeGenTarget = "Dart";

    private const string LanguageDisplayName = "Dart";

    private static readonly string[] s_detectionPatterns = [AppHostFileName];

    /// <inheritdoc />
    public string Language => LanguageId;

    /// <inheritdoc />
    /// <remarks>
    /// The Dart VM reads the PEM bundle named by <c>SSL_CERT_FILE</c>, so the ASP.NET Core
    /// development certificate reaches every outbound connection the AppHost makes.
    /// </remarks>
    public string CertificateBundleEnvironmentVariable => "SSL_CERT_FILE";

    /// <inheritdoc />
    public Dictionary<string, string> Scaffold(ScaffoldRequest request)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        // The generated SDK loads through a single library entry file. AtsDartCodeGenerator emits
        // .aspire/modules/aspire.dart, which exports base.dart, transport.dart, aspire_runtime.dart
        // and the generated parts, and which defines createBuilder.
        files[AppHostFileName] = """
            // Aspire Dart AppHost
            // For more information, see: https://aspire.dev

            import '.aspire/modules/aspire.dart';

            Future<void> main(List<String> args) async {
              final builder = await createBuilder(args);

              // Add your resources here, for example:
              // final cache = await builder.addContainer('cache', 'redis:latest');
              // final db = await builder.addContainer('db', 'postgres:latest');

              final app = await builder.build();
              await app.run();
            }
            """;

        // The AppHost is a standalone script, not a published package, so the manifest declares only
        // a name and an SDK constraint. `dart pub get` then works with no network access.
        files[PubspecFileName] = """
            name: apphost
            publish_to: none

            environment:
              sdk: ^3.8.0
            """;

        // .aspire/ holds the generated SDK, which the CLI rewrites on every relevant change.
        // .dart_tool/ holds the package resolution that `dart pub get` writes.
        files[".gitignore"] = """
            .aspire/
            .dart_tool/
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

        // A Dart script runs only inside a package. Without pubspec.yaml `dart run apphost.dart`
        // cannot resolve the generated SDK, so the directory is not a Dart AppHost.
        var pubspecPath = Path.Combine(directoryPath, PubspecFileName);
        if (!File.Exists(pubspecPath))
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
            // `dart pub get` writes .dart_tool/package_config.json. The scaffolded pubspec.yaml
            // declares no dependency, so the command needs no network access.
            InstallDependencies = new CommandSpec
            {
                Command = "dart",
                Args = ["pub", "get"]
            },
            // `dart run` compiles the AppHost in memory on every launch, so there is no separate
            // build step to keep up to date.
            PreExecute = null,
            // Watch mode arrives with D2.6.
            WatchExecute = null,
            Execute = new CommandSpec
            {
                Command = "dart",
                Args = ["run", "{appHostFile}"]
            }
        };
    }
}
