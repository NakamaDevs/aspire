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
    /// Use <see cref="WithAppArgs{T}"/> to pass extra arguments to the application. In run mode the
    /// resource sets <c>MIX_ENV</c> to <c>dev</c>.
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
                // `mix run --no-halt` starts the application and keeps the BEAM alive.
                ctx.Args.Add("run");
                ctx.Args.Add("--no-halt");

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

        if (builder.ExecutionContext.IsRunMode)
        {
            // MIX_ENV selects the Mix environment. Publish output must not force the dev environment.
            rb.WithEnvironment("MIX_ENV", "dev");
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
}
