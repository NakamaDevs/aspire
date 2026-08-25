// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// Records the database that an Elixir application uses through Ecto.
/// </summary>
/// <remarks>
/// The migration sibling reads this annotation from its parent, so <c>WithEctoMigrate</c> and
/// <c>WithEctoDatabase</c> can run in either order.
/// </remarks>
internal sealed class ElixirEctoDatabaseAnnotation(IResourceBuilder<IResourceWithConnectionString> database)
    : IResourceAnnotation
{
    public IResourceBuilder<IResourceWithConnectionString> Database { get; } = database;
}
