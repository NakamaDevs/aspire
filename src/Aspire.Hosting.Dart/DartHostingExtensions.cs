// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dart;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dart applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DartHostingExtensions
{
    /// <summary>The name of the Serverpod endpoint that serves the client API.</summary>
    private const string ServerpodApiEndpointName = "api";

    /// <summary>The name of the Serverpod endpoint that serves Serverpod Insights.</summary>
    private const string ServerpodInsightsEndpointName = "insights";

    /// <summary>The name of the Serverpod endpoint that serves the web pages.</summary>
    private const string ServerpodWebEndpointName = "web";

    /// <summary>
    /// Adds a Dart application to the application model. The Dart SDK must be available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory that contains <c>pubspec.yaml</c>.</param>
    /// <param name="entrypoint">The Dart file that starts the application, relative to <paramref name="appDirectory"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// This method runs the application with <c>dart run &lt;entrypoint&gt;</c>. The default entrypoint
    /// is <c>bin/main.dart</c>, which is the file that the Dart templates create.
    /// </para>
    /// <para>
    /// Use <see cref="WithAppArgs{T}"/> to pass extra arguments to the application, and
    /// <see cref="WithDartDefine{T}"/> to supply a compile-time environment value.
    /// </para>
    /// <para>
    /// In run mode the method adds a <c>dart pub get</c> step when the directory contains
    /// <c>pubspec.yaml</c>. Use <see cref="WithPubGet{T}"/> to control that step.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Dart application to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddDartApp("api", "../dart-api")
    ///        .WithHttpEndpoint(port: 8080, env: "PORT")
    ///        .WithExternalHttpEndpoints();
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<DartAppResource> AddDartApp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string appDirectory,
        string entrypoint = DartEntrypointAnnotation.DefaultEntrypoint)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        appDirectory = Path.GetFullPath(appDirectory, builder.AppHostDirectory);

        var resource = new DartAppResource(name, appDirectory);

        // The annotation carries the entrypoint, so the argument callback and a later publish step read
        // one value.
        resource.Annotations.Add(new DartEntrypointAnnotation(entrypoint));

        return ConfigureDartApp(builder, resource, appDirectory);
    }

    /// <summary>
    /// Replaces the complete command line of a Dart application.
    /// </summary>
    /// <typeparam name="T">The type of the Dart application resource.</typeparam>
    /// <param name="builder">The resource builder for the Dart application.</param>
    /// <param name="command">The command that starts the application, for example <c>dart_frog</c>.</param>
    /// <param name="args">The arguments of the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// A Dart web framework can supply its own command-line tool. The method replaces the command and
    /// every argument, so the resource no longer runs <c>dart run &lt;entrypoint&gt;</c>. It also adds
    /// a required command for the tool.
    /// </para>
    /// <para>
    /// The arguments of <see cref="WithAppArgs{T}"/> still come after the arguments of this method.
    /// The options of <see cref="WithDartDefine{T}"/> and <see cref="WithDartRunArgs{T}"/> belong to
    /// <c>dart run</c>. The method writes them directly after a leading <c>run</c> argument, and only
    /// when the command is <c>dart</c>. Every other command line drops them, because no other
    /// position and no other tool accepts them.
    /// </para>
    /// <para>A second call replaces the command line of the first call.</para>
    /// </remarks>
    /// <example>
    /// Run a Dart Frog application:
    /// <code lang="csharp">
    /// var api = builder.AddDartApp("api", "../api")
    ///                  .WithHttpEndpoint(env: "PORT");
    ///
    /// api.WithRunCommand("dart_frog", "dev", "--port", api.GetEndpoint("http").Property(EndpointProperty.TargetPort));
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithRunCommand<T>(
        this IResourceBuilder<T> builder,
        string command,
        params object[] args)
        where T : DartAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(command);
        ArgumentNullException.ThrowIfNull(args);

        builder.WithAnnotation(new DartRunCommandAnnotation(command, args), ResourceAnnotationMutationBehavior.Replace);

        // The executable resource holds the command, so the model must change with the annotation.
        return builder
            .WithCommand(command)
            .WithRequiredCommand(command);
    }

    /// <summary>
    /// Replaces the Dart file that <c>dart run</c> starts.
    /// </summary>
    /// <typeparam name="T">The type of the Dart application resource.</typeparam>
    /// <param name="builder">The resource builder for the Dart application.</param>
    /// <param name="entrypoint">The Dart file that starts the application, relative to the application directory.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// <see cref="AddDartApp"/> takes the entrypoint as its third parameter. This method changes the
    /// value later, which a preset such as <see cref="AddServerpodApp"/> needs.
    /// </para>
    /// <para>A second call replaces the entrypoint of the first call.</para>
    /// </remarks>
    /// <example>
    /// Start a different file:
    /// <code lang="csharp">
    /// builder.AddDartApp("api", "../dart-api")
    ///        .WithEntrypoint("bin/server.dart");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithEntrypoint<T>(this IResourceBuilder<T> builder, string entrypoint)
        where T : DartAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        return builder.WithAnnotation(
            new DartEntrypointAnnotation(entrypoint), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Passes options to <c>dart run</c> itself, before the entrypoint.
    /// </summary>
    /// <typeparam name="T">The type of the Dart application resource.</typeparam>
    /// <param name="builder">The resource builder for the Dart application.</param>
    /// <param name="args">The options of <c>dart run</c> (for example, <c>"--enable-asserts"</c>).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// <c>dart run</c> gives every argument after the entrypoint to the program, so an option of the
    /// runtime must come before the entrypoint. Use <see cref="WithAppArgs{T}"/> for an argument of
    /// the program.
    /// </para>
    /// <para>A second call replaces the options of the first call.</para>
    /// </remarks>
    /// <example>
    /// Turn the assertions on:
    /// <code lang="csharp">
    /// builder.AddDartApp("api", "../dart-api")
    ///        .WithDartRunArgs("--enable-asserts");
    /// // dart run --enable-asserts bin/main.dart
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithDartRunArgs<T>(this IResourceBuilder<T> builder, params string[] args)
        where T : DartAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);

        return builder.WithAnnotation(new DartRunArgsAnnotation(args), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Adds a Jaspr web application to the application model. The Dart SDK and the Jaspr
    /// command-line tool must be available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory that contains <c>pubspec.yaml</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// This method runs the application with <c>jaspr serve -p &lt;port&gt;</c> and adds one HTTP
    /// endpoint. The port comes from that endpoint, so Aspire controls the address.
    /// </para>
    /// <para>
    /// The method reads the rendering mode from the <c>jaspr</c> block of <c>pubspec.yaml</c>. When
    /// the file holds no mode, the mode is <see cref="JasprMode.Static"/>, which is the Jaspr
    /// default. Use <see cref="WithJasprMode{T}"/> to set a different mode.
    /// </para>
    /// <para>
    /// <c>jaspr serve</c> also binds a web port and a proxy port. The Jaspr defaults 5467 and 5567
    /// collide when two Jaspr applications run at the same time, so use
    /// <see cref="WithJasprDevPorts{T}"/> to give each application its own pair.
    /// </para>
    /// <para>
    /// <c>jaspr serve</c> watches the source files and reloads without a restart, so the method marks
    /// the resource and Aspire keeps its own restart off.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Jaspr application to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddJasprApp("web", "../jaspr-web")
    ///        .WithExternalHttpEndpoints();
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<JasprAppResource> AddJasprApp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string appDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);

        appDirectory = Path.GetFullPath(appDirectory, builder.AppHostDirectory);

        var resource = new JasprAppResource(name, appDirectory);

        // pubspec.yaml is the contract of the project, so the model follows the file.
        // WithJasprMode replaces the annotation.
        resource.Annotations.Add(new JasprModeAnnotation(ReadJasprModeFromPubspec(appDirectory)));

        // `jaspr serve` holds its own file watcher, so a restart from Aspire would stop that work.
        resource.Annotations.Add(new JasprSelfReloadsAnnotation());

        // The endpoint annotation comes from WithHttpEndpoint below. The reference resolves when the
        // argument callback runs, which happens after that call.
        var endpoint = new EndpointReference(resource, "http");

        return ConfigureDartApp(builder, resource, appDirectory, ctx =>
            {
                ctx.Args.Add("serve");

                // `jaspr serve` binds the port that -p names, so the value comes from the endpoint
                // that Aspire allocated.
                ctx.Args.Add("-p");
                ctx.Args.Add(endpoint.Property(EndpointProperty.TargetPort));

                if (resource.TryGetLastAnnotation<JasprDevPortsAnnotation>(out var devPorts))
                {
                    if (devPorts.WebPort is { } webPort)
                    {
                        ctx.Args.Add("--web-port");
                        ctx.Args.Add(webPort.ToString(CultureInfo.InvariantCulture));
                    }

                    if (devPorts.ProxyPort is { } proxyPort)
                    {
                        ctx.Args.Add("--proxy-port");
                        ctx.Args.Add(proxyPort.ToString(CultureInfo.InvariantCulture));
                    }
                }

                if (!resource.TryGetLastAnnotation<DartAppArgsAnnotation>(out var argsAnnotation))
                {
                    return;
                }

                foreach (var arg in argsAnnotation.Args)
                {
                    ctx.Args.Add(arg);
                }
            })
            .WithHttpEndpoint(env: "PORT")
            .WithRequiredCommand("jaspr", "https://docs.jaspr.site/get_started/installation");
    }

    /// <summary>
    /// Sets the rendering mode of a Jaspr application.
    /// </summary>
    /// <typeparam name="T">The type of the Jaspr application resource.</typeparam>
    /// <param name="builder">The resource builder for the Jaspr application.</param>
    /// <param name="mode">The rendering mode.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// <see cref="AddJasprApp"/> reads the mode from <c>pubspec.yaml</c>. Call this method when the
    /// AppHost must hold a different value, for example while the two files change.
    /// </para>
    /// <para>A second call replaces the mode of the first call.</para>
    /// </remarks>
    /// <example>
    /// Render the pages on a Dart server:
    /// <code lang="csharp">
    /// builder.AddJasprApp("web", "../jaspr-web")
    ///        .WithJasprMode(JasprMode.Server);
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithJasprMode<T>(this IResourceBuilder<T> builder, JasprMode mode)
        where T : JasprAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new JasprModeAnnotation(mode), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Sets the development ports of <c>jaspr serve</c>.
    /// </summary>
    /// <typeparam name="T">The type of the Jaspr application resource.</typeparam>
    /// <param name="builder">The resource builder for the Jaspr application.</param>
    /// <param name="webPort">The value of the <c>--web-port</c> option, or <see langword="null"/> for the Jaspr default 5467.</param>
    /// <param name="proxyPort">The value of the <c>--proxy-port</c> option, or <see langword="null"/> for the Jaspr default 5567.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// <c>jaspr serve</c> binds two more ports for its development tools. The Jaspr defaults are 5467
    /// and 5567. Two Jaspr applications in one distributed application collide on those defaults, so
    /// give each application its own pair.
    /// </para>
    /// <para>A second call replaces the ports of the first call.</para>
    /// </remarks>
    /// <example>
    /// Give a second Jaspr application its own development ports:
    /// <code lang="csharp">
    /// builder.AddJasprApp("docs", "../jaspr-docs")
    ///        .WithJasprDevPorts(webPort: 5468, proxyPort: 5568);
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithJasprDevPorts<T>(
        this IResourceBuilder<T> builder,
        int? webPort = null,
        int? proxyPort = null)
        where T : JasprAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(
            new JasprDevPortsAnnotation(webPort, proxyPort), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Reads the rendering mode from the <c>jaspr</c> block of <c>pubspec.yaml</c>.
    /// </summary>
    /// <remarks>
    /// The method reads the two lines that hold the value instead of a complete YAML parse, because
    /// the block has one fixed shape:
    /// <code lang="yaml">
    /// jaspr:
    ///   mode: server
    /// </code>
    /// The method returns <see cref="JasprMode.Static"/> when the file is absent, when it holds no
    /// <c>jaspr</c> block, or when the value is not a known mode. That value is the Jaspr default.
    /// </remarks>
    private static JasprMode ReadJasprModeFromPubspec(string appDirectory)
    {
        var pubspecPath = Path.Combine(appDirectory, "pubspec.yaml");

        if (!File.Exists(pubspecPath))
        {
            return JasprMode.Static;
        }

        var inJasprBlock = false;

        foreach (var line in File.ReadLines(pubspecPath))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            // A line without leading space starts a new top-level key, which closes the block.
            if (!char.IsWhiteSpace(line[0]))
            {
                inJasprBlock = trimmed.StartsWith("jaspr:", StringComparison.Ordinal);
                continue;
            }

            if (!inJasprBlock || !trimmed.StartsWith("mode:", StringComparison.Ordinal))
            {
                continue;
            }

            var value = trimmed["mode:".Length..];

            var comment = value.IndexOf('#', StringComparison.Ordinal);
            if (comment >= 0)
            {
                value = value[..comment];
            }

            value = value.Trim().Trim('"', '\'');

            return Enum.TryParse<JasprMode>(value, ignoreCase: true, out var mode)
                ? mode
                : JasprMode.Static;
        }

        return JasprMode.Static;
    }

    /// <summary>
    /// Adds a Serverpod server to the application model. The Dart SDK must be available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="serverDirectory">The path to the server directory of the Serverpod project, which has the name <c>{project}_server</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// This method runs the server with <c>dart run bin/main.dart --mode &lt;mode&gt;
    /// --apply-migrations</c>. The default mode is <c>development</c> in run mode and
    /// <c>production</c> in publish mode. Use <see cref="WithServerpodMode{T}"/> and
    /// <see cref="WithApplyMigrations{T}"/> to change the two values.
    /// </para>
    /// <para>
    /// The method adds the three endpoints of a Serverpod server: <c>api</c> on target port 8080,
    /// <c>insights</c> on target port 8081, and <c>web</c> on target port 8082. A Serverpod
    /// environment variable wins over the value in the configuration file below <c>config</c>, so the
    /// server listens on the ports that Aspire assigns.
    /// </para>
    /// <para>
    /// The method also sets the public host, port, and scheme of each endpoint. Serverpod builds the
    /// URLs that it sends to a client from those values.
    /// </para>
    /// <para>
    /// In publish mode the method sets <c>SERVERPOD_PASSWORD_serviceSecret</c> from a generated secret
    /// parameter with the name <c>{name}-service-secret</c>. In run mode the method does not set the
    /// variable, because <c>config/passwords.yaml</c> holds the development value.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Serverpod server with a database and a cache:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var db = builder.AddPostgres("pg").AddDatabase("serverpod");
    /// var cache = builder.AddRedis("cache");
    ///
    /// builder.AddServerpodApp("api", "../myapp_server")
    ///        .WithServerpodDatabase(db)
    ///        .WithServerpodRedis(cache);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<ServerpodAppResource> AddServerpodApp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string serverDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(serverDirectory);

        serverDirectory = Path.GetFullPath(serverDirectory, builder.AppHostDirectory);

        var resource = new ServerpodAppResource(name, serverDirectory);

        // A Serverpod server starts from bin/main.dart, which is the file that `serverpod create`
        // writes.
        resource.Annotations.Add(new DartEntrypointAnnotation(DartEntrypointAnnotation.DefaultEntrypoint));

        // A deployed server must not run the development configuration, so the mode follows the
        // execution context. WithServerpodMode replaces the annotation.
        resource.Annotations.Add(new ServerpodModeAnnotation(builder.ExecutionContext.IsRunMode
            ? ServerpodModeAnnotation.Development
            : ServerpodModeAnnotation.Production));

        // `serverpod create` writes the migrations into the repository, so the server applies them by
        // default. WithApplyMigrations replaces the annotation.
        resource.Annotations.Add(new ServerpodApplyMigrationsAnnotation(true));

        var rb = ConfigureDartApp(builder, resource, serverDirectory)
            .WithArgs(ctx =>
            {
                // WithRunCommand replaces the complete command line, so the preset adds nothing.
                if (resource.TryGetLastAnnotation<DartRunCommandAnnotation>(out _))
                {
                    return;
                }

                // `dart run` gives every argument after the file name to the program, and the
                // Serverpod entrypoint reads the two options itself.
                ctx.Args.Add("--mode");
                ctx.Args.Add(ServerpodModeAnnotation.Resolve(resource));

                if (ServerpodApplyMigrationsAnnotation.Resolve(resource))
                {
                    ctx.Args.Add("--apply-migrations");
                }
            })
            .WithHttpEndpoint(name: ServerpodApiEndpointName, env: "SERVERPOD_API_SERVER_PORT", targetPort: 8080)
            .WithHttpEndpoint(name: ServerpodInsightsEndpointName, env: "SERVERPOD_INSIGHTS_SERVER_PORT", targetPort: 8081)
            .WithHttpEndpoint(name: ServerpodWebEndpointName, env: "SERVERPOD_WEB_SERVER_PORT", targetPort: 8082);

        var apiEndpoint = rb.GetEndpoint(ServerpodApiEndpointName);
        var insightsEndpoint = rb.GetEndpoint(ServerpodInsightsEndpointName);
        var webEndpoint = rb.GetEndpoint(ServerpodWebEndpointName);

        // Serverpod signs the service protocol calls with this secret. A deployed server has no
        // config/passwords.yaml from the repository, so publish mode generates the value.
        var serviceSecret = builder.ExecutionContext.IsPublishMode
            ? ParameterResourceBuilderExtensions.CreateGeneratedParameter(
                builder,
                $"{name}-service-secret",
                secret: true,
                new GenerateParameterDefault { MinLength = 32 })
            : null;

        rb.WithEnvironment(ctx =>
        {
            ctx.EnvironmentVariables["SERVERPOD_RUN_MODE"] = ServerpodModeAnnotation.Resolve(resource);

            SetServerpodPublicEndpoint(ctx, "API", apiEndpoint);
            SetServerpodPublicEndpoint(ctx, "INSIGHTS", insightsEndpoint);
            SetServerpodPublicEndpoint(ctx, "WEB", webEndpoint);

            if (serviceSecret is not null)
            {
                ctx.EnvironmentVariables["SERVERPOD_PASSWORD_serviceSecret"] = serviceSecret;
            }
        });

        return rb;
    }

    /// <summary>
    /// Sets the <c>SERVERPOD_DATABASE_*</c> environment variables from a database resource.
    /// </summary>
    /// <typeparam name="T">The type of the Serverpod server resource.</typeparam>
    /// <param name="builder">The resource builder for the Serverpod server.</param>
    /// <param name="database">The resource builder for the database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The method reads the <c>Host</c>, <c>Port</c>, <c>DatabaseName</c>, <c>Username</c>, and
    /// <c>Password</c> connection properties of the resource. The comparison ignores letter case.
    /// Every resource that exposes those properties works, for example a PostgreSQL database or a
    /// connection-string parameter that carries them. Serverpod supports PostgreSQL only, so the
    /// values must point to a PostgreSQL server.
    /// </para>
    /// <para>
    /// The method sets an environment variable only when the resource has the property, so a
    /// resource without a user or a password still works. <c>Host</c> and <c>Port</c> are necessary:
    /// the method throws an <see cref="InvalidOperationException"/> when the resource has neither.
    /// </para>
    /// <para>
    /// The password remains a parameter reference, so the manifest holds no secret value.
    /// </para>
    /// <para>
    /// The method also adds a reference to the database and makes the server wait for it.
    /// </para>
    /// </remarks>
    /// <example>
    /// Connect a Serverpod server to a PostgreSQL database:
    /// <code lang="csharp">
    /// var db = builder.AddPostgres("pg").AddDatabase("serverpod");
    ///
    /// builder.AddServerpodApp("api", "../myapp_server")
    ///        .WithServerpodDatabase(db);
    /// </code>
    /// </example>
    // The database parameter makes the default capability id collide across integrations,
    // so give the capability a unique id and keep the polyglot method name.
    [AspireExport("withDartServerpodDatabase", MethodName = "withServerpodDatabase")]
    public static IResourceBuilder<T> WithServerpodDatabase<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithConnectionString> database)
        where T : ServerpodAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(database);

        return builder
            .WithReference(database)
            .WaitFor(database)
            .WithEnvironment(ctx =>
            {
                var properties = ReadConnectionProperties(database.Resource);

                // Serverpod cannot open a connection without an address, so these two must exist.
                ctx.EnvironmentVariables["SERVERPOD_DATABASE_HOST"] =
                    RequireConnectionProperty(database.Resource, properties, "Host");
                ctx.EnvironmentVariables["SERVERPOD_DATABASE_PORT"] =
                    RequireConnectionProperty(database.Resource, properties, "Port");

                SetFromConnectionProperty(ctx, properties, "DatabaseName", "SERVERPOD_DATABASE_NAME");
                SetFromConnectionProperty(ctx, properties, "Username", "SERVERPOD_DATABASE_USER");

                // Serverpod reads every password from one SERVERPOD_PASSWORD_<name> family.
                SetFromConnectionProperty(ctx, properties, "Password", "SERVERPOD_PASSWORD_database");
            });
    }

    /// <summary>
    /// Sets the <c>SERVERPOD_REDIS_*</c> environment variables from a cache resource and turns the
    /// cache support of Serverpod on.
    /// </summary>
    /// <typeparam name="T">The type of the Serverpod server resource.</typeparam>
    /// <param name="builder">The resource builder for the Serverpod server.</param>
    /// <param name="cache">The resource builder for the cache.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The method sets <c>SERVERPOD_REDIS_ENABLED</c> to <c>true</c>, and it reads the <c>Host</c>,
    /// <c>Port</c>, and <c>Password</c> connection properties of the resource. The comparison ignores
    /// letter case. Every resource that exposes those properties works, for example a Redis cache or
    /// a connection-string parameter that carries them. Serverpod supports Redis only, so the values
    /// must point to a Redis server.
    /// </para>
    /// <para>
    /// The method sets <c>SERVERPOD_PASSWORD_redis</c> only when the resource has a password.
    /// <c>Host</c> and <c>Port</c> are necessary: the method throws an
    /// <see cref="InvalidOperationException"/> when the resource has neither.
    /// </para>
    /// <para>
    /// The method also adds a reference to the cache and makes the server wait for it.
    /// </para>
    /// </remarks>
    /// <example>
    /// Connect a Serverpod server to a Redis cache:
    /// <code lang="csharp">
    /// var cache = builder.AddRedis("cache");
    ///
    /// builder.AddServerpodApp("api", "../myapp_server")
    ///        .WithServerpodRedis(cache);
    /// </code>
    /// </example>
    // The cache parameter makes the default capability id collide across integrations,
    // so give the capability a unique id and keep the polyglot method name.
    [AspireExport("withDartServerpodRedis", MethodName = "withServerpodRedis")]
    public static IResourceBuilder<T> WithServerpodRedis<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithConnectionString> cache)
        where T : ServerpodAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(cache);

        return builder
            .WithReference(cache)
            .WaitFor(cache)
            .WithEnvironment(ctx =>
            {
                var properties = ReadConnectionProperties(cache.Resource);

                ctx.EnvironmentVariables["SERVERPOD_REDIS_ENABLED"] = "true";

                // Serverpod cannot open a connection without an address, so these two must exist.
                ctx.EnvironmentVariables["SERVERPOD_REDIS_HOST"] =
                    RequireConnectionProperty(cache.Resource, properties, "Host");
                ctx.EnvironmentVariables["SERVERPOD_REDIS_PORT"] =
                    RequireConnectionProperty(cache.Resource, properties, "Port");

                // Serverpod reads every password from one SERVERPOD_PASSWORD_<name> family.
                SetFromConnectionProperty(ctx, properties, "Password", "SERVERPOD_PASSWORD_redis");
            });
    }

    /// <summary>
    /// Sets the run mode of a Serverpod server.
    /// </summary>
    /// <typeparam name="T">The type of the Serverpod server resource.</typeparam>
    /// <param name="builder">The resource builder for the Serverpod server.</param>
    /// <param name="mode">The run mode, for example <c>development</c>, <c>staging</c>, or <c>production</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The mode selects the configuration file <c>config/{mode}.yaml</c>. It becomes the value of the
    /// <c>--mode</c> option and of <c>SERVERPOD_RUN_MODE</c>.
    /// </para>
    /// <para>A second call replaces the mode of the first call.</para>
    /// </remarks>
    /// <example>
    /// Run the server with the staging configuration:
    /// <code lang="csharp">
    /// builder.AddServerpodApp("api", "../myapp_server")
    ///        .WithServerpodMode("staging");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithServerpodMode<T>(this IResourceBuilder<T> builder, string mode)
        where T : ServerpodAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(mode);

        return builder.WithAnnotation(new ServerpodModeAnnotation(mode), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Controls whether the Serverpod server applies its database migrations when it starts.
    /// </summary>
    /// <typeparam name="T">The type of the Serverpod server resource.</typeparam>
    /// <param name="builder">The resource builder for the Serverpod server.</param>
    /// <param name="applyMigrations">
    /// <see langword="true"/> to add the <c>--apply-migrations</c> option.
    /// <see langword="false"/> to remove it.
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// <see cref="AddServerpodApp"/> adds the option by default, so the schema of the database
    /// matches the code. Turn the option off when a separate step applies the migrations.
    /// </para>
    /// <para>A second call replaces the value of the first call.</para>
    /// </remarks>
    /// <example>
    /// Start the server without the migration step:
    /// <code lang="csharp">
    /// builder.AddServerpodApp("api", "../myapp_server")
    ///        .WithApplyMigrations(false);
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithApplyMigrations<T>(this IResourceBuilder<T> builder, bool applyMigrations = true)
        where T : ServerpodAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(
            new ServerpodApplyMigrationsAnnotation(applyMigrations), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Sets the public host, port, and scheme of one Serverpod endpoint.
    /// </summary>
    /// <remarks>
    /// Serverpod builds the URLs that it sends to a client from these values, so they must hold the
    /// address that a client reaches, not the address that the server binds.
    /// </remarks>
    private static void SetServerpodPublicEndpoint(
        EnvironmentCallbackContext ctx,
        string prefix,
        EndpointReference endpoint)
    {
        ctx.EnvironmentVariables[$"SERVERPOD_{prefix}_SERVER_PUBLIC_HOST"] = endpoint.Property(EndpointProperty.Host);
        ctx.EnvironmentVariables[$"SERVERPOD_{prefix}_SERVER_PUBLIC_PORT"] = endpoint.Property(EndpointProperty.Port);
        ctx.EnvironmentVariables[$"SERVERPOD_{prefix}_SERVER_PUBLIC_SCHEME"] = endpoint.Property(EndpointProperty.Scheme);
    }

    /// <summary>
    /// Reads the connection properties of a resource into a dictionary that ignores letter case.
    /// </summary>
    private static Dictionary<string, ReferenceExpression> ReadConnectionProperties(
        IResourceWithConnectionString resource)
    {
        var properties = new Dictionary<string, ReferenceExpression>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in resource.GetConnectionProperties())
        {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    /// <summary>
    /// Returns one connection property, or throws when the resource does not expose it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The resource exposes no property with that name.</exception>
    private static ReferenceExpression RequireConnectionProperty(
        IResourceWithConnectionString resource,
        Dictionary<string, ReferenceExpression> properties,
        string property)
    {
        if (properties.TryGetValue(property, out var value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"The resource '{resource.Name}' exposes no '{property}' connection property. " +
            $"Serverpod needs that value to open a connection.");
    }

    /// <summary>
    /// Sets one environment variable from a connection property, if the resource has that property.
    /// </summary>
    private static void SetFromConnectionProperty(
        EnvironmentCallbackContext ctx,
        Dictionary<string, ReferenceExpression> properties,
        string property,
        string variable)
    {
        if (properties.TryGetValue(property, out var value))
        {
            ctx.EnvironmentVariables[variable] = value;
        }
    }

    /// <summary>
    /// Adds the shared Dart configuration to a new application resource and returns its builder.
    /// </summary>
    /// <remarks>
    /// Every Dart application shape calls this method, so every shape gets the same arguments, required
    /// commands, telemetry, and pub setup sibling.
    /// </remarks>
    private static IResourceBuilder<T> ConfigureDartApp<T>(
        IDistributedApplicationBuilder builder,
        T resource,
        string appDirectory,
        Action<CommandLineArgsCallbackContext>? configureArgs = null)
        where T : DartAppResource
    {
        // A Dart framework can start the application with its own command-line tool. That tool takes
        // different arguments, so the caller replaces the `dart run` arguments.
        var shapeArgs = configureArgs ?? (ctx => WriteDartRunArgs(resource, ctx));

        // WithRunCommand replaces the complete command line, so it wins over every shape.
        var argsCallback = (CommandLineArgsCallbackContext ctx) =>
        {
            if (resource.TryGetLastAnnotation<DartRunCommandAnnotation>(out var runCommand))
            {
                WriteRunCommandArgs(resource, runCommand, ctx);
                return;
            }

            shapeArgs(ctx);
        };

        var rb = builder.AddResource(resource)
            .WithIconName("Code")
            .WithArgs(argsCallback)
            .WithRequiredCommand("dart", "https://dart.dev/get-dart")
            .WithOtlpExporter();

        // The Dart runtime replaces its trust set with the file that SSL_CERT_FILE names. It cannot
        // add to the trust set, so the default scope is System. That scope makes Aspire put the
        // system authorities and the custom authorities in one bundle.
        rb.WithCertificateTrustScope(CertificateTrustScope.System)
            .WithCertificateTrustConfiguration(ctx =>
            {
                // Aspire applies custom certificate trust in run mode only.
                if (ctx.ExecutionContext.IsPublishMode)
                {
                    return Task.CompletedTask;
                }

                // An OTLP exporter reads its own certificate bundle. The value is safe in every
                // scope, because the dashboard uses one of the custom authorities.
                ctx.EnvironmentVariables["OTEL_EXPORTER_OTLP_CERTIFICATE"] = ctx.CertificateBundlePath;

                if (ctx.Scope != CertificateTrustScope.Append)
                {
                    // SSL_CERT_FILE replaces the complete trust set, so it must not receive an Append
                    // bundle. An Append bundle holds the custom authorities only.
                    ctx.EnvironmentVariables["SSL_CERT_FILE"] = ctx.CertificateBundlePath;
                }

                return Task.CompletedTask;
            });

        if (builder.ExecutionContext.IsRunMode)
        {
            // The setup sibling only runs in run mode. Wire its wait relationship from the final model,
            // because WithPubGet can run after AddDartApp and can change an existing step.
            builder.OnBeforeStart((_, _) =>
            {
                SetupDartDependencies(builder, resource);
                return Task.CompletedTask;
            });

            // A directory with pubspec.yaml is a pub package, so fetch its dependencies by default.
            if (File.Exists(Path.Combine(appDirectory, "pubspec.yaml")))
            {
                rb.WithPubGet();
            }
        }

        AddContainerFilesBuildDependencies(rb);

        return rb;
    }

    /// <summary>
    /// Writes the <c>dart run</c> arguments of a Dart application.
    /// </summary>
    private static void WriteDartRunArgs(DartAppResource resource, CommandLineArgsCallbackContext ctx)
    {
        ctx.Args.Add("run");

        // `dart run` reads its own options before the file name and gives everything after the
        // file name to the program. Every option must therefore come directly after `run`.
        WriteDartRunOptions(resource, ctx);

        ctx.Args.Add(DartEntrypointAnnotation.Resolve(resource));

        WriteAppArgs(resource, ctx);
    }

    /// <summary>
    /// Writes the command line that <see cref="WithRunCommand{T}"/> supplied.
    /// </summary>
    private static void WriteRunCommandArgs(
        DartAppResource resource,
        DartRunCommandAnnotation runCommand,
        CommandLineArgsCallbackContext ctx)
    {
        // `--define` and the other run options belong to `dart run`. They are valid directly after
        // the `run` argument and nowhere else, so no other command-line tool receives them.
        var isDartRun = string.Equals(runCommand.Command, "dart", StringComparison.Ordinal)
            && runCommand.Args.Length > 0
            && runCommand.Args[0] is string first
            && string.Equals(first, "run", StringComparison.Ordinal);

        for (var i = 0; i < runCommand.Args.Length; i++)
        {
            ctx.Args.Add(runCommand.Args[i]);

            if (i == 0 && isDartRun)
            {
                WriteDartRunOptions(resource, ctx);
            }
        }

        WriteAppArgs(resource, ctx);
    }

    /// <summary>
    /// Writes the options of <c>dart run</c> itself: the <c>--define</c> options and the options of
    /// <see cref="WithDartRunArgs{T}"/>.
    /// </summary>
    /// <remarks>
    /// The options go directly after the <c>run</c> argument, because <c>dart run</c> gives every
    /// argument after the entrypoint to the program.
    /// </remarks>
    private static void WriteDartRunOptions(DartAppResource resource, CommandLineArgsCallbackContext ctx)
    {
        WriteDartDefines(resource, ctx);

        if (!resource.TryGetLastAnnotation<DartRunArgsAnnotation>(out var runArgs))
        {
            return;
        }

        foreach (var arg in runArgs.Args)
        {
            ctx.Args.Add(arg);
        }
    }

    /// <summary>
    /// Writes one <c>--define</c> option for each <see cref="DartDefineAnnotation"/>.
    /// </summary>
    private static void WriteDartDefines(DartAppResource resource, CommandLineArgsCallbackContext ctx)
    {
        if (!resource.TryGetAnnotationsOfType<DartDefineAnnotation>(out var defines))
        {
            return;
        }

        foreach (var define in defines)
        {
            ctx.Args.Add($"--define={define.Key}={define.Value}");
        }
    }

    /// <summary>
    /// Writes the arguments that <see cref="WithAppArgs{T}"/> supplied.
    /// </summary>
    private static void WriteAppArgs(DartAppResource resource, CommandLineArgsCallbackContext ctx)
    {
        if (!resource.TryGetLastAnnotation<DartAppArgsAnnotation>(out var argsAnnotation))
        {
            return;
        }

        // The program gets every argument after the file name, so no separator is necessary.
        foreach (var arg in argsAnnotation.Args)
        {
            ctx.Args.Add(arg);
        }
    }

    /// <summary>
    /// Passes extra arguments to the Dart application at runtime.
    /// The arguments appear after the entrypoint, as <c>dart run bin/main.dart &lt;args&gt;</c>.
    /// </summary>
    /// <typeparam name="T">The type of the Dart application resource.</typeparam>
    /// <param name="builder">The resource builder for the Dart application.</param>
    /// <param name="args">The application arguments (for example, <c>"--port"</c>, <c>"8080"</c>).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// <c>dart run</c> gives every argument after the entrypoint to the program, so the arguments need
    /// no <c>--</c> separator.
    /// </para>
    /// <para>A second call replaces the arguments of the first call.</para>
    /// </remarks>
    /// <example>
    /// Pass a port to the application:
    /// <code lang="csharp">
    /// builder.AddDartApp("api", "../dart-api")
    ///        .WithAppArgs("--port", "8080");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithAppArgs<T>(this IResourceBuilder<T> builder, params object[] args)
        where T : DartAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);

        return builder.WithAnnotation(new DartAppArgsAnnotation(args), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Supplies a compile-time environment value with the <c>--define</c> option of <c>dart run</c>.
    /// </summary>
    /// <typeparam name="T">The type of the Dart application resource.</typeparam>
    /// <param name="builder">The resource builder for the Dart application.</param>
    /// <param name="key">The name that <c>String.fromEnvironment</c> reads in the Dart code.</param>
    /// <param name="value">The value of the key.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The keys accumulate, so more calls with different keys give more <c>--define</c> options. A
    /// second call with the same key replaces the value of the first call.
    /// </para>
    /// <para>
    /// The options come before the entrypoint, because <c>dart run</c> gives every argument after the
    /// entrypoint to the program.
    /// </para>
    /// </remarks>
    /// <example>
    /// Select a build flavor:
    /// <code lang="csharp">
    /// builder.AddDartApp("api", "../dart-api")
    ///        .WithDartDefine("FLAVOR", "dev");
    /// // dart run --define=FLAVOR=dev bin/main.dart
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithDartDefine<T>(this IResourceBuilder<T> builder, string key, string value)
        where T : DartAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        // One annotation holds one key. A second call with the same key must not add a second option
        // for that key, because `dart run` accepts one value for each name.
        foreach (var annotation in builder.Resource.Annotations
            .OfType<DartDefineAnnotation>()
            .Where(a => string.Equals(a.Key, key, StringComparison.Ordinal))
            .ToArray())
        {
            builder.Resource.Annotations.Remove(annotation);
        }

        return builder.WithAnnotation(new DartDefineAnnotation(key, value));
    }

    /// <summary>
    /// Runs <c>dart pub get</c> before the application starts, so the pub dependencies are available.
    /// </summary>
    /// <typeparam name="T">The type of the Dart application resource.</typeparam>
    /// <param name="builder">The resource builder for the Dart application.</param>
    /// <param name="install">
    /// <see langword="true"/> to run the step automatically before the application starts.
    /// <see langword="false"/> to create the step but let the developer start it from the dashboard.
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The method creates one sibling resource with the name <c>{app}-pub-get</c>. More calls do not
    /// create more resources. The step runs only in run mode and stays out of the manifest.
    /// </para>
    /// <para>
    /// <see cref="AddDartApp"/> calls this method automatically when the application directory contains
    /// <c>pubspec.yaml</c>. Call it again with <c>install: false</c> to stop the automatic run, and once
    /// more with <c>install: true</c> to start it again.
    /// </para>
    /// <para>The step uses the working directory of the application.</para>
    /// </remarks>
    /// <example>
    /// Fetch dependencies, but start the step by hand:
    /// <code lang="csharp">
    /// builder.AddDartApp("api", "../dart-api")
    ///        .WithPubGet(install: false);
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithPubGet<T>(this IResourceBuilder<T> builder, bool install = true)
        where T : DartAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The setup sibling has no meaning during publish.
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        var pubGetName = $"{builder.Resource.Name}-pub-get";

        if (builder.ApplicationBuilder.TryCreateResourceBuilder<DartPubGetResource>(pubGetName, out var existing))
        {
            if (install)
            {
                // A later call with install: true turns the automatic run back on, so the two values
                // are symmetric.
                foreach (var annotation in existing.Resource.Annotations.OfType<ExplicitStartupAnnotation>().ToArray())
                {
                    existing.Resource.Annotations.Remove(annotation);
                }
            }
            else
            {
                // SetupDartDependencies reads this annotation and skips the wait relationship.
                existing.WithExplicitStart();
            }

            return builder;
        }

        var pubGet = new DartPubGetResource(pubGetName, builder.Resource);
        pubGet.Annotations.Add(NameValidationPolicyAnnotation.None);

        var pubGetBuilder = builder.ApplicationBuilder.AddResource(pubGet)
            .WithArgs("pub", "get")
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest()
            .WithCertificateTrustScope(CertificateTrustScope.None)
            .WithRequiredCommand("dart", "https://dart.dev/get-dart");

        if (!install)
        {
            pubGetBuilder.WithExplicitStart();
        }

        return builder;
    }

    /// <summary>
    /// Wires the wait relationship between a Dart application and its pub setup sibling.
    /// </summary>
    /// <remarks>
    /// The method runs from the final model, because <c>WithPubGet</c> can run after
    /// <c>AddDartApp</c>, and because <c>WithPubGet(install: false)</c> can change an existing step.
    /// </remarks>
    private static void SetupDartDependencies(IDistributedApplicationBuilder builder, DartAppResource resource)
    {
        if (!builder.TryCreateResourceBuilder<DartAppResource>(resource.Name, out var appBuilder))
        {
            return;
        }

        if (!builder.TryCreateResourceBuilder<DartPubGetResource>($"{resource.Name}-pub-get", out var pubGetBuilder))
        {
            return;
        }

        // An executable reads its working directory from an annotation, so WithWorkingDirectory moves
        // the application after AddDartApp created the sibling. The sibling must run pub in the same
        // directory, so the final model supplies the value.
        pubGetBuilder.WithWorkingDirectory(resource.WorkingDirectory);

        // The developer starts an explicit-start step by hand, so nothing must wait for it.
        if (pubGetBuilder.Resource.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _))
        {
            return;
        }

        appBuilder.WaitForCompletion(pubGetBuilder);
    }

    /// <summary>
    /// Makes the image build wait for every resource that supplies container files to it.
    /// </summary>
    /// <remarks>
    /// The Dockerfile copies from the images of those resources, so the images must exist first. The
    /// file dependency alone does not order the build.
    /// </remarks>
    private static void AddContainerFilesBuildDependencies<T>(IResourceBuilder<T> rb)
        where T : IResource
    {
        rb.WithPipelineConfiguration(context =>
        {
            if (rb.Resource.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(
                    out var containerFilesAnnotations))
            {
                var buildSteps = context.GetSteps(rb.Resource, WellKnownPipelineTags.BuildCompute);
                foreach (var containerFile in containerFilesAnnotations)
                {
                    buildSteps.DependsOn(context.GetSteps(containerFile.Source, WellKnownPipelineTags.BuildCompute));
                }
            }
        });
    }
}
