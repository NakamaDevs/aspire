// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

/// <summary>
/// A resource that runs <c>mix ecto.migrate</c> for an Elixir application before the application starts.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The Elixir application resource that owns this migration step.</param>
internal sealed class ElixirEctoMigrateResource(string name, ElixirAppResource parent)
    : ExecutableResource(name, "mix", parent.WorkingDirectory)
{
    public ElixirAppResource Parent { get; } = parent;
}
