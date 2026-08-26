// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Records whether the Serverpod server applies its database migrations when it starts.
/// </summary>
/// <remarks>
/// The value becomes the <c>--apply-migrations</c> option of <c>dart bin/main.dart</c>.
/// </remarks>
internal sealed class ServerpodApplyMigrationsAnnotation(bool applyMigrations) : IResourceAnnotation
{
    /// <summary>Whether the server applies the migrations.</summary>
    public bool ApplyMigrations { get; } = applyMigrations;

    /// <summary>Returns the value of <paramref name="resource"/>, or the default value.</summary>
    public static bool Resolve(ServerpodAppResource resource)
        => !resource.TryGetLastAnnotation<ServerpodApplyMigrationsAnnotation>(out var annotation)
            || annotation.ApplyMigrations;
}
