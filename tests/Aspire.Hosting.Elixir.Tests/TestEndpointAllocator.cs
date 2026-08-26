// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Elixir.Tests;

internal static class TestEndpointAllocator
{
    /// <summary>
    /// The orchestrator allocates endpoints at run time. Environment variable evaluation waits for
    /// that allocation, so a test that never starts the application must supply it. If the test does
    /// not supply it, the evaluation never returns.
    /// </summary>
    public static void AllocateEndpoints(IResource resource)
    {
        foreach (var endpoint in resource.Annotations.OfType<EndpointAnnotation>())
        {
            endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 4000, targetPortExpression: "4000");
        }
    }
}
