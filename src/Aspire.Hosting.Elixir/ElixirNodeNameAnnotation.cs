// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir;

internal sealed class ElixirNodeNameAnnotation(string nodeName, ParameterResource cookie) : IResourceAnnotation
{
    public string NodeName { get; } = nodeName;

    public ParameterResource Cookie { get; } = cookie;
}
