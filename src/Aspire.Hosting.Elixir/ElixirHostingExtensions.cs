// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elixir;

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
        var resource = new ElixirAppResource(name, appDirectory);

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

        return rb;
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

        // The developer starts an explicit-start step by hand, so nothing must wait for it.
        var depsRunsAutomatically = depsBuilder is not null
            && !depsBuilder.Resource.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _);

        if (compileBuilder is not null)
        {
            if (depsRunsAutomatically)
            {
                // deps.get writes the deps directory that compile reads.
                compileBuilder.WaitForCompletion(depsBuilder!);
            }

            appBuilder.WaitForCompletion(compileBuilder);
        }
        else if (depsRunsAutomatically)
        {
            appBuilder.WaitForCompletion(depsBuilder!);
        }
    }
}
