// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dart;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dart applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DartHostingExtensions
{
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
    /// Adds the shared Dart configuration to a new application resource and returns its builder.
    /// </summary>
    /// <remarks>
    /// Every Dart application shape calls this method, so every shape gets the same arguments, required
    /// commands, telemetry, and pub setup sibling.
    /// </remarks>
    private static IResourceBuilder<T> ConfigureDartApp<T>(
        IDistributedApplicationBuilder builder,
        T resource,
        string appDirectory)
        where T : DartAppResource
    {
        var rb = builder.AddResource(resource)
            .WithIconName("Code")
            .WithArgs(ctx =>
            {
                ctx.Args.Add("run");

                // `dart run` reads its own options before the file name and gives everything after the
                // file name to the program. A --define option must therefore come first.
                if (resource.TryGetAnnotationsOfType<DartDefineAnnotation>(out var defines))
                {
                    foreach (var define in defines)
                    {
                        ctx.Args.Add($"--define={define.Key}={define.Value}");
                    }
                }

                ctx.Args.Add(DartEntrypointAnnotation.Resolve(resource));

                if (!resource.TryGetLastAnnotation<DartAppArgsAnnotation>(out var argsAnnotation))
                {
                    return;
                }

                // The program gets every argument after the file name, so no separator is necessary.
                foreach (var arg in argsAnnotation.Args)
                {
                    ctx.Args.Add(arg);
                }
            })
            .WithRequiredCommand("dart", "https://dart.dev/get-dart")
            .WithOtlpExporter();

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
