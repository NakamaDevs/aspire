// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001
#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elixir;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Elixir applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class ElixirHostingExtensions
{
    /// <summary>
    /// Adds an Elixir application to the application model. Elixir and the Mix build tool must be available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory that contains <c>mix.exs</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// This method runs the Elixir application with <c>mix run --no-halt</c>. The command keeps the
    /// application alive after Mix starts it.
    /// </para>
    /// <para>
    /// Use <see cref="WithAppArgs{T}"/> to pass extra arguments to the application. The resource sets
    /// <c>MIX_ENV</c> to <c>dev</c> in run mode and to <c>prod</c> in publish mode. Use
    /// <see cref="WithMixEnv{T}"/> to select a different Mix environment.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add an Elixir application to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithHttpEndpoint(port: 4000)
    ///        .WithExternalHttpEndpoints();
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<ElixirAppResource> AddElixirApp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string appDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);

        appDirectory = Path.GetFullPath(appDirectory, builder.AppHostDirectory);

        return ConfigureElixirApp(builder, new ElixirAppResource(name, appDirectory), appDirectory);
    }

    /// <summary>
    /// Adds a Phoenix web application to the application model. Elixir and the Mix build tool must be
    /// available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory that contains <c>mix.exs</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// This method runs the application with <c>mix phx.server</c> and adds one HTTP endpoint. Phoenix
    /// reads the port of that endpoint from the <c>PORT</c> environment variable. Use
    /// <see cref="WithMixTask{T}"/> to run a different Mix task.
    /// </para>
    /// <para>
    /// The method sets <c>PHX_SERVER</c> to <c>true</c>, which tells a Mix release to start the
    /// endpoint. It also sets <c>PHX_HOST</c> to the host of the HTTP endpoint.
    /// </para>
    /// <para>
    /// In publish mode the method sets <c>SECRET_KEY_BASE</c> from a generated secret parameter with
    /// the name <c>{name}-secret-key-base</c>. The parameter has 64 characters, which is the minimum
    /// length that Phoenix accepts. In run mode the method does not set the variable, because
    /// <c>config/dev.exs</c> holds the development value.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Phoenix application to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPhoenixApp("web", "../phoenix-web")
    ///        .WithExternalHttpEndpoints();
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<PhoenixAppResource> AddPhoenixApp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string appDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);

        appDirectory = Path.GetFullPath(appDirectory, builder.AppHostDirectory);
        var resource = new PhoenixAppResource(name, appDirectory);

        // `mix phx.server` starts the Phoenix endpoint. WithMixTask replaces this annotation, so the
        // developer keeps control of the task.
        resource.Annotations.Add(new ElixirMixTaskAnnotation("phx.server", []));

        var rb = ConfigureElixirApp(builder, resource, appDirectory)
            .WithHttpEndpoint(env: "PORT");

        var endpoint = rb.GetEndpoint("http");

        // In publish mode Phoenix refuses to start without a secret of at least 64 characters.
        // In run mode config/dev.exs holds the value, so a generated parameter is not necessary.
        var secretKeyBase = builder.ExecutionContext.IsPublishMode
            ? ParameterResourceBuilderExtensions.CreateGeneratedParameter(
                builder,
                $"{name}-secret-key-base",
                secret: true,
                new GenerateParameterDefault { MinLength = 64, Special = false })
            : null;

        rb.WithEnvironment(ctx =>
        {
            // PHX_SERVER tells a Mix release to start the endpoint.
            ctx.EnvironmentVariables["PHX_SERVER"] = "true";

            // Phoenix builds its URLs from PHX_HOST, so the value must match the allocated endpoint.
            ctx.EnvironmentVariables["PHX_HOST"] = endpoint.Property(EndpointProperty.Host);

            if (secretKeyBase is not null)
            {
                ctx.EnvironmentVariables["SECRET_KEY_BASE"] = secretKeyBase;
            }
        });

        return rb;
    }

    /// <summary>
    /// Adds the shared Elixir configuration to a new application resource and returns its builder.
    /// </summary>
    /// <remarks>
    /// <see cref="AddElixirApp"/> and <see cref="AddPhoenixApp"/> both call this method, so both get the
    /// same arguments, required commands, telemetry, certificate trust, and Mix setup siblings.
    /// </remarks>
    private static IResourceBuilder<T> ConfigureElixirApp<T>(
        IDistributedApplicationBuilder builder,
        T resource,
        string appDirectory)
        where T : ElixirAppResource
    {
        var rb = builder.AddResource(resource)
            .WithIconName("Code")
            .WithArgs(ctx =>
            {
                if (ctx.Resource.TryGetLastAnnotation<ElixirMixTaskAnnotation>(out var taskAnnotation))
                {
                    ctx.Args.Add(taskAnnotation.Task);
                    foreach (var taskArg in taskAnnotation.Args)
                    {
                        ctx.Args.Add(taskArg);
                    }
                }
                else
                {
                    // `mix run --no-halt` starts the application and keeps the BEAM alive.
                    ctx.Args.Add("run");
                    ctx.Args.Add("--no-halt");
                }

                var appArgs = ctx.Resource.TryGetLastAnnotation<ElixirAppArgsAnnotation>(out var argsAnnotation)
                    ? argsAnnotation.Args
                    : [];

                if (appArgs.Length == 0)
                {
                    return;
                }

                // Mix passes everything after `--` to the application instead of parsing it itself.
                ctx.Args.Add("--");
                foreach (var arg in appArgs)
                {
                    ctx.Args.Add(arg);
                }
            })
            .WithRequiredCommand("mix", "https://elixir-lang.org/install.html")
            .WithRequiredCommand("elixir", "https://elixir-lang.org/install.html")
            .WithOtlpExporter();

        // The Erlang :ssl application replaces its trust set with the file that SSL_CERT_FILE names.
        // It cannot add to the trust set, so the default scope is System. That scope makes Aspire put
        // the system authorities and the custom authorities in one bundle.
        rb.WithCertificateTrustScope(CertificateTrustScope.System)
            .WithCertificateTrustConfiguration(ctx =>
            {
                // Aspire applies custom certificate trust in run mode only.
                if (ctx.ExecutionContext.IsPublishMode)
                {
                    return Task.CompletedTask;
                }

                // The OTLP exporter of the Erlang SDK reads its own certificate bundle. The value is
                // safe in every scope, because the dashboard uses one of the custom authorities.
                ctx.EnvironmentVariables["OTEL_EXPORTER_OTLP_CERTIFICATE"] = ctx.CertificateBundlePath;

                if (ctx.Scope != CertificateTrustScope.Append)
                {
                    // SSL_CERT_FILE replaces the complete trust set, so it must not receive an Append
                    // bundle. An Append bundle holds the custom authorities only.
                    ctx.EnvironmentVariables["SSL_CERT_FILE"] = ctx.CertificateBundlePath;
                }

                return Task.CompletedTask;
            });

        // MIX_ENV selects the Mix environment. Local work uses dev, and publish output uses prod.
        rb.WithEnvironment("MIX_ENV", builder.ExecutionContext.IsRunMode ? "dev" : "prod");

        // WithElixirErlOptions and WithNodeName both write ELIXIR_ERL_OPTIONS, so one callback
        // composes the complete value from the annotations that the developer applied.
        rb.WithEnvironment(ctx =>
        {
            var hasOptions = resource.TryGetLastAnnotation<ElixirErlOptionsAnnotation>(out var optionsAnnotation);
            var hasNodeName = resource.TryGetLastAnnotation<ElixirNodeNameAnnotation>(out var nodeNameAnnotation);

            if (!hasOptions && !hasNodeName)
            {
                return;
            }

            var options = new ReferenceExpressionBuilder();

            if (hasOptions)
            {
                options.AppendLiteral(optionsAnnotation!.Options);
            }

            if (hasNodeName)
            {
                if (hasOptions)
                {
                    options.AppendLiteral(" ");
                }

                // --sname names the node, and --cookie authenticates the distribution connections.
                options.AppendLiteral($"--sname {nodeNameAnnotation!.NodeName} --cookie ");
                options.AppendFormatted(nodeNameAnnotation.Cookie);
            }

            ctx.EnvironmentVariables["ELIXIR_ERL_OPTIONS"] = options.Build();

            if (hasNodeName)
            {
                // Mix releases read RELEASE_NODE and RELEASE_COOKIE instead of ELIXIR_ERL_OPTIONS.
                ctx.EnvironmentVariables["RELEASE_NODE"] = nodeNameAnnotation!.NodeName;
                ctx.EnvironmentVariables["RELEASE_COOKIE"] = nodeNameAnnotation.Cookie;
            }
        });

        if (builder.ExecutionContext.IsRunMode)
        {
            // The setup siblings only run in run mode. Wire their wait relationships from the final
            // model, because WithMixDeps and WithMixCompile can run in either order.
            builder.OnBeforeStart((_, _) =>
            {
                SetupMixDependencies(builder, resource);
                return Task.CompletedTask;
            });

            // A directory with mix.exs is a Mix project, so fetch its dependencies by default.
            if (File.Exists(Path.Combine(appDirectory, "mix.exs")))
            {
                rb.WithMixDeps();
            }
        }

        // `aspire publish` turns the executable into a container image. The generated Dockerfile builds
        // a Mix release, so the image holds no compiler and no source.
        var isPhoenix = resource is PhoenixAppResource;
        rb.PublishAsDockerFile(containerBuilder =>
        {
            // An authored Dockerfile is the contract of the repository, so Aspire must not replace it.
            if (File.Exists(Path.Combine(appDirectory, "Dockerfile")))
            {
                return;
            }

            containerBuilder.WithDockerfileBuilder(
                appDirectory,
                ctx => ElixirDockerfileGenerator.WriteApplication(appDirectory, isPhoenix, ctx));
        });

        AddContainerFilesBuildDependencies(rb);

        return rb;
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

    /// <summary>
    /// Passes extra arguments to the Elixir application at runtime.
    /// The arguments appear after the <c>--</c> separator, as <c>mix run --no-halt -- &lt;args&gt;</c>.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="args">The application arguments (for example, <c>"--port"</c>, <c>"4000"</c>).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>A second call replaces the arguments of the first call.</remarks>
    [AspireExport]
    public static IResourceBuilder<T> WithAppArgs<T>(this IResourceBuilder<T> builder, params object[] args)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);
        return builder.WithAnnotation(new ElixirAppArgsAnnotation(args), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Runs <c>mix deps.get</c> before the application starts, so the Hex dependencies are available.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="install">
    /// <see langword="true"/> to run the step automatically before the application starts.
    /// <see langword="false"/> to create the step but let the developer start it from the dashboard.
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The method creates one sibling resource with the name <c>{app}-mix-deps</c>. More calls do not
    /// create more resources. The step runs only in run mode and stays out of the manifest.
    /// </para>
    /// <para>
    /// <see cref="AddElixirApp"/> calls this method automatically when the application directory
    /// contains <c>mix.exs</c>. Call it again with <c>install: false</c> to stop the automatic run.
    /// </para>
    /// </remarks>
    /// <example>
    /// Fetch dependencies, but start the step by hand:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithMixDeps(install: false);
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithMixDeps<T>(this IResourceBuilder<T> builder, bool install = true)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The setup sibling has no meaning during publish.
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        var depsName = $"{builder.Resource.Name}-mix-deps";

        if (builder.ApplicationBuilder.TryCreateResourceBuilder<ElixirMixDepsResource>(depsName, out var existing))
        {
            if (!install)
            {
                // SetupMixDependencies reads this annotation and skips the wait relationships.
                existing.WithExplicitStart();
            }

            return builder;
        }

        var deps = new ElixirMixDepsResource(depsName, builder.Resource);
        deps.Annotations.Add(NameValidationPolicyAnnotation.None);

        var depsBuilder = builder.ApplicationBuilder.AddResource(deps)
            .WithArgs("deps.get")
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest()
            .WithCertificateTrustScope(CertificateTrustScope.None)
            .WithRequiredCommand("mix", "https://elixir-lang.org/install.html");

        if (!install)
        {
            depsBuilder.WithExplicitStart();
        }

        return builder;
    }

    /// <summary>
    /// Runs <c>mix compile</c> before the application starts, so compile errors appear before startup.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// The method creates one sibling resource with the name <c>{app}-mix-compile</c>. More calls do not
    /// create more resources. When <see cref="WithMixDeps{T}"/> also created a step, the compile step
    /// waits for the dependency step. The step runs only in run mode and stays out of the manifest.
    /// </remarks>
    /// <example>
    /// Fetch dependencies and compile before the application starts:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithMixDeps()
    ///        .WithMixCompile();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithMixCompile<T>(this IResourceBuilder<T> builder)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        var compileName = $"{builder.Resource.Name}-mix-compile";

        if (builder.ApplicationBuilder.TryCreateResourceBuilder<ElixirMixCompileResource>(compileName, out _))
        {
            return builder;
        }

        var compile = new ElixirMixCompileResource(compileName, builder.Resource);
        compile.Annotations.Add(NameValidationPolicyAnnotation.None);

        builder.ApplicationBuilder.AddResource(compile)
            .WithArgs("compile")
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest()
            .WithCertificateTrustScope(CertificateTrustScope.None)
            .WithRequiredCommand("mix", "https://elixir-lang.org/install.html");

        return builder;
    }

    /// <summary>
    /// Selects the Mix environment with the <c>MIX_ENV</c> environment variable.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="env">The Mix environment, for example <c>dev</c>, <c>test</c>, or <c>prod</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// The value replaces the default, which is <c>dev</c> in run mode and <c>prod</c> in publish mode.
    /// </remarks>
    /// <example>
    /// Run the application in the test environment:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithMixEnv("test");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithMixEnv<T>(this IResourceBuilder<T> builder, string env)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(env);

        return builder.WithEnvironment("MIX_ENV", env);
    }

    /// <summary>
    /// Runs a different Mix task instead of <c>run --no-halt</c>.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="task">The Mix task, for example <c>phx.server</c>.</param>
    /// <param name="args">The arguments for the Mix task.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// A second call replaces the task of the first call. Arguments from
    /// <see cref="WithAppArgs{T}"/> stay after the <c>--</c> separator.
    /// </remarks>
    /// <example>
    /// Start a Phoenix web server:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithMixTask("phx.server");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithMixTask<T>(this IResourceBuilder<T> builder, string task, params object[] args)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(task);
        ArgumentNullException.ThrowIfNull(args);

        return builder.WithAnnotation(new ElixirMixTaskAnnotation(task, args), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Passes flags to the Erlang virtual machine with the <c>ERL_FLAGS</c> environment variable.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="flags">The Erlang virtual machine flags, for example <c>"+S 4:4"</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>A second call replaces the flags of the first call.</remarks>
    /// <example>
    /// Limit the virtual machine to four schedulers:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithErlFlags("+S 4:4");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithErlFlags<T>(this IResourceBuilder<T> builder, string flags)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(flags);

        return builder.WithEnvironment("ERL_FLAGS", flags);
    }

    /// <summary>
    /// Passes options to the <c>elixir</c> command with the <c>ELIXIR_ERL_OPTIONS</c> environment variable.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="options">The options for the <c>elixir</c> command, for example <c>"+K true"</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// A second call replaces the options of the first call. <see cref="WithNodeName{T}"/> adds its
    /// own options after these options.
    /// </remarks>
    /// <example>
    /// Turn on kernel poll:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithElixirErlOptions("+K true");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithElixirErlOptions<T>(this IResourceBuilder<T> builder, string options)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(options);

        return builder.WithAnnotation(new ElixirErlOptionsAnnotation(options), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Starts the application as a named node, so other nodes can connect to it.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="name">The short node name, for example <c>api</c>.</param>
    /// <param name="cookie">
    /// The parameter that holds the Erlang distribution cookie. When the value is
    /// <see langword="null"/>, the method creates a secret parameter with the name <c>{app}-cookie</c>.
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The method adds <c>--sname</c> and <c>--cookie</c> to <c>ELIXIR_ERL_OPTIONS</c>, after any
    /// options from <see cref="WithElixirErlOptions{T}"/>.
    /// </para>
    /// <para>
    /// The method also sets <c>RELEASE_NODE</c> and <c>RELEASE_COOKIE</c>, which a Mix release reads.
    /// </para>
    /// </remarks>
    /// <example>
    /// Start a named node with a generated cookie:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithNodeName("api");
    /// </code>
    /// </example>
    // The cookie parameter makes the default capability id collide across integrations,
    // so give the capability a unique id and keep the polyglot method name.
    [AspireExport("withElixirNodeName", MethodName = "withNodeName")]
    public static IResourceBuilder<T> WithNodeName<T>(
        this IResourceBuilder<T> builder,
        string name,
        IResourceBuilder<ParameterResource>? cookie = null)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // An Erlang cookie is an atom, so keep the generated value free of special characters.
        var cookieParameter = cookie?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
                builder.ApplicationBuilder, $"{builder.Resource.Name}-cookie", special: false);

        return builder.WithAnnotation(
            new ElixirNodeNameAnnotation(name, cookieParameter), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Sets the <c>DATABASE_URL</c> environment variable from a database resource, so Ecto can connect.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="database">The resource builder for the database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// Ecto reads a URI, for example <c>postgresql://user:password@host:port/database</c>. The method
    /// uses the <c>Uri</c> connection property of the database when the database has one. If the
    /// database has no <c>Uri</c> property, the method uses the connection string of the database, and
    /// the developer must confirm that Ecto accepts that format.
    /// </para>
    /// <para>
    /// The method also adds a reference to the database and makes the application wait for it.
    /// </para>
    /// </remarks>
    /// <example>
    /// Connect an Elixir application to a PostgreSQL database:
    /// <code lang="csharp">
    /// var db = builder.AddPostgres("pg").AddDatabase("appdb");
    ///
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithEctoDatabase(db);
    /// </code>
    /// </example>
    // The database parameter makes the default capability id collide across integrations,
    // so give the capability a unique id and keep the polyglot method name.
    [AspireExport("withElixirEctoDatabase", MethodName = "withEctoDatabase")]
    public static IResourceBuilder<T> WithEctoDatabase<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithConnectionString> database)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(database);

        builder.WithAnnotation(new ElixirEctoDatabaseAnnotation(database), ResourceAnnotationMutationBehavior.Replace);

        return builder
            .WithReference(database)
            .WaitFor(database)
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["DATABASE_URL"] = BuildEctoDatabaseUrl(database.Resource);
            });
    }

    /// <summary>
    /// Runs <c>mix ecto.migrate</c> before the application starts, so the database schema is current.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The method creates one sibling resource with the name <c>{app}-ecto-migrate</c>. More calls do
    /// not create more resources. The step runs only in run mode and stays out of the manifest.
    /// </para>
    /// <para>
    /// The step gets the same <c>DATABASE_URL</c> value as the application. It waits for the database
    /// from <see cref="WithEctoDatabase{T}"/> and for the Mix setup siblings. The application waits for
    /// the step to complete. <see cref="WithEctoDatabase{T}"/> and this method can run in either order.
    /// </para>
    /// <para>
    /// In publish mode a Mix release has no Mix tasks. Run the migration from the release script, for
    /// example <c>bin/my_app eval "MyApp.Release.migrate"</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Migrate the database before the application starts:
    /// <code lang="csharp">
    /// var db = builder.AddPostgres("pg").AddDatabase("appdb");
    ///
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithEctoDatabase(db)
    ///        .WithEctoMigrate();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithEctoMigrate<T>(this IResourceBuilder<T> builder)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        // A Mix release has no Mix tasks, so the migration sibling has no meaning during publish.
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        var migrateName = $"{builder.Resource.Name}-ecto-migrate";

        if (builder.ApplicationBuilder.TryCreateResourceBuilder<ElixirEctoMigrateResource>(migrateName, out _))
        {
            return builder;
        }

        var migrate = new ElixirEctoMigrateResource(migrateName, builder.Resource);
        migrate.Annotations.Add(NameValidationPolicyAnnotation.None);

        builder.ApplicationBuilder.AddResource(migrate)
            .WithArgs("ecto.migrate")
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest()
            .WithCertificateTrustScope(CertificateTrustScope.None)
            .WithRequiredCommand("mix", "https://elixir-lang.org/install.html")
            .WithEnvironment(ctx =>
            {
                // WithEctoDatabase can run after this method, so read the database from the parent
                // when the environment is evaluated instead of when the sibling is created.
                if (migrate.Parent.TryGetLastAnnotation<ElixirEctoDatabaseAnnotation>(out var databaseAnnotation))
                {
                    ctx.EnvironmentVariables["DATABASE_URL"] = BuildEctoDatabaseUrl(databaseAnnotation.Database.Resource);
                }
            });

        return builder;
    }

    /// <summary>
    /// Builds the value of <c>DATABASE_URL</c> for a database resource.
    /// </summary>
    /// <remarks>
    /// Ecto accepts <c>ecto://</c>, <c>postgres://</c>, and <c>postgresql://</c>. The <c>Uri</c>
    /// connection property already has that shape, so the method uses it when the resource has one.
    /// </remarks>
    private static object BuildEctoDatabaseUrl(IResourceWithConnectionString database)
    {
        foreach (var property in database.GetConnectionProperties())
        {
            if (string.Equals(property.Key, "Uri", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        // The resource exposes no URI property. The connection string is the only value left, and the
        // developer must confirm that Ecto accepts its format.
        return new ConnectionStringReference(database, optional: false);
    }

    /// <summary>
    /// Wires the wait relationships between an Elixir application and its Mix setup siblings.
    /// </summary>
    /// <remarks>
    /// The method runs from the final model, because <c>WithMixDeps</c> and <c>WithMixCompile</c> can
    /// run in either order, and because <c>WithMixDeps(install: false)</c> can change an existing step.
    /// </remarks>
    private static void SetupMixDependencies(IDistributedApplicationBuilder builder, ElixirAppResource resource)
    {
        if (!builder.TryCreateResourceBuilder<ElixirAppResource>(resource.Name, out var appBuilder))
        {
            return;
        }

        builder.TryCreateResourceBuilder<ElixirMixDepsResource>($"{resource.Name}-mix-deps", out var depsBuilder);
        builder.TryCreateResourceBuilder<ElixirMixCompileResource>($"{resource.Name}-mix-compile", out var compileBuilder);
        builder.TryCreateResourceBuilder<ElixirEctoMigrateResource>($"{resource.Name}-ecto-migrate", out var migrateBuilder);

        // The developer starts an explicit-start step by hand, so nothing must wait for it.
        var depsRunsAutomatically = depsBuilder is not null
            && !depsBuilder.Resource.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _);

        // The Mix setup steps run in order: deps.get, then compile. The last one is the step that the
        // migration, or the application when there is no migration, must wait for.
        IResourceBuilder<IResource>? lastSetupStep = null;

        if (compileBuilder is not null)
        {
            if (depsRunsAutomatically)
            {
                // deps.get writes the deps directory that compile reads.
                compileBuilder.WaitForCompletion(depsBuilder!);
            }

            lastSetupStep = compileBuilder;
        }
        else if (depsRunsAutomatically)
        {
            lastSetupStep = depsBuilder;
        }

        if (migrateBuilder is null)
        {
            if (lastSetupStep is not null)
            {
                appBuilder.WaitForCompletion(lastSetupStep);
            }

            return;
        }

        if (lastSetupStep is not null)
        {
            // `mix ecto.migrate` compiles the application, so the dependencies must be present first.
            migrateBuilder.WaitForCompletion(lastSetupStep);
        }

        if (resource.TryGetLastAnnotation<ElixirEctoDatabaseAnnotation>(out var databaseAnnotation))
        {
            // The migration cannot run before the database accepts connections.
            migrateBuilder.WaitFor(databaseAnnotation.Database);
        }

        appBuilder.WaitForCompletion(migrateBuilder);
    }

    /// <summary>
    /// Sets the name of the Mix release that <c>aspire publish</c> builds and starts.
    /// </summary>
    /// <typeparam name="T">The type of the Elixir application resource.</typeparam>
    /// <param name="builder">The resource builder for the Elixir application.</param>
    /// <param name="name">The release name, for example <c>my_app</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// Without this method the name comes from the <c>app:</c> key in <c>mix.exs</c>. If <c>mix.exs</c>
    /// gives no name, the name is the resource name with each hyphen replaced by an underscore.
    /// </para>
    /// <para>
    /// Use this method when <c>mix.exs</c> declares more than one release, or when the release name is
    /// different from the application name. The method changes publish output only. It has no effect in
    /// run mode, because run mode starts the application with Mix.
    /// </para>
    /// <para>A second call replaces the name of the first call.</para>
    /// </remarks>
    /// <example>
    /// Build the <c>api_release</c> release:
    /// <code lang="csharp">
    /// builder.AddElixirApp("api", "../elixir-api")
    ///        .WithReleaseName("api_release");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<T> WithReleaseName<T>(this IResourceBuilder<T> builder, string name)
        where T : ElixirAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return builder.WithAnnotation(
            new ElixirReleaseNameAnnotation(name), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Adds an HTTP health check that reads a path of the Phoenix endpoint.
    /// </summary>
    /// <param name="builder">The resource builder for the Phoenix application.</param>
    /// <param name="path">The path of the health check. The default is <c>/health</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// The application must serve the path. Add a route to the Phoenix router, or use a plug such as
    /// <c>PlugCheckup</c>. The check reports healthy when the path answers with status 200.
    /// </remarks>
    /// <example>
    /// Add a health check that reads <c>/healthz</c>:
    /// <code lang="csharp">
    /// builder.AddPhoenixApp("web", "../phoenix-web")
    ///        .WithPhoenixHealthCheck("/healthz");
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<PhoenixAppResource> WithPhoenixHealthCheck(
        this IResourceBuilder<PhoenixAppResource> builder,
        string path = "/health")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(path);

        return builder.WithHttpHealthCheck(path);
    }

    /// <summary>
    /// Adds a Mix release that another build step already produced. Elixir and Mix are not necessary.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="releaseDirectory">
    /// The root directory of the release, for example <c>_build/prod/rel/my_app</c>. The directory holds
    /// the <c>bin</c> directory with the launcher script.
    /// </param>
    /// <param name="releaseName">
    /// The release name, which is also the name of the launcher script. The default is the name of
    /// <paramref name="releaseDirectory"/>.
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// The resource starts the release with <c>bin/&lt;releaseName&gt; start</c>. On Windows it starts
    /// <c>bin\&lt;releaseName&gt;.bat</c>. A release carries its compiled dependencies, so this method
    /// adds no Mix setup steps.
    /// </para>
    /// <para>
    /// In publish mode the resource becomes a container image with one stage. The image copies the
    /// release directory and runs it as a user that is not root.
    /// </para>
    /// <para>
    /// A Mix release reads <c>RELEASE_NODE</c> and <c>RELEASE_COOKIE</c> to name and authenticate its
    /// node. Set both with <c>WithEnvironment</c> when the release must join a cluster.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a release that Mix already built:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddMixRelease("api", "../elixir-api/_build/prod/rel/my_app")
    ///        .WithHttpEndpoint(env: "PORT");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [AspireExport]
    public static IResourceBuilder<MixReleaseResource> AddMixRelease(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string releaseDirectory,
        string? releaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(releaseDirectory);

        releaseDirectory = Path.GetFullPath(releaseDirectory, builder.AppHostDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // `mix release` names the directory after the release, so the directory name is the best default.
        releaseName ??= Path.GetFileName(releaseDirectory);

        if (string.IsNullOrEmpty(releaseName))
        {
            throw new ArgumentException(
                $"Cannot read a release name from '{releaseDirectory}'. Pass the name in the releaseName parameter.",
                nameof(releaseDirectory));
        }

        // `mix release` writes bin/<name> for Unix and bin/<name>.bat for Windows.
        var launcher = OperatingSystem.IsWindows() ? $"{releaseName}.bat" : releaseName;
        var command = Path.Combine(releaseDirectory, "bin", launcher);

        var resource = new MixReleaseResource(name, command, releaseDirectory, releaseName);

        var rb = builder.AddResource(resource)
            .WithIconName("Code")
            .WithArgs("start")
            .WithOtlpExporter();

        // The Erlang :ssl application replaces its trust set with the file that SSL_CERT_FILE names.
        // It cannot add to the trust set, so the default scope is System. That scope makes Aspire put
        // the system authorities and the custom authorities in one bundle.
        rb.WithCertificateTrustScope(CertificateTrustScope.System)
            .WithCertificateTrustConfiguration(ctx =>
            {
                // Aspire applies custom certificate trust in run mode only.
                if (ctx.ExecutionContext.IsPublishMode)
                {
                    return Task.CompletedTask;
                }

                // The OTLP exporter of the Erlang SDK reads its own certificate bundle. The value is
                // safe in every scope, because the dashboard uses one of the custom authorities.
                ctx.EnvironmentVariables["OTEL_EXPORTER_OTLP_CERTIFICATE"] = ctx.CertificateBundlePath;

                if (ctx.Scope != CertificateTrustScope.Append)
                {
                    // SSL_CERT_FILE replaces the complete trust set, so it must not receive an Append
                    // bundle. An Append bundle holds the custom authorities only.
                    ctx.EnvironmentVariables["SSL_CERT_FILE"] = ctx.CertificateBundlePath;
                }

                return Task.CompletedTask;
            });

        rb.PublishAsDockerFile(containerBuilder =>
        {
            // An authored Dockerfile is the contract of the repository, so Aspire must not replace it.
            if (File.Exists(Path.Combine(releaseDirectory, "Dockerfile")))
            {
                return;
            }

            containerBuilder.WithDockerfileBuilder(
                releaseDirectory,
                ctx => ElixirDockerfileGenerator.WriteRelease(releaseDirectory, releaseName, ctx));
        });

        AddContainerFilesBuildDependencies(rb);

        return rb;
    }
}
