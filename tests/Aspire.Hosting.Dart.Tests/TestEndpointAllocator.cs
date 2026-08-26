// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart.Tests;

internal static class TestEndpointAllocator
{
    /// <summary>
    /// The orchestrator allocates endpoints at run time. Environment variable evaluation waits for
    /// that allocation, so a test that never starts the application must supply it. If the test does
    /// not supply it, the evaluation never returns.
    /// </summary>
    /// <remarks>
    /// The allocated port is the target port of the endpoint, so a resource with more than one
    /// endpoint gets one distinct port for each endpoint. An endpoint without a target port gets
    /// <paramref name="defaultPort"/>.
    /// </remarks>
    public static void AllocateEndpoints(IResource resource, int defaultPort = 8000)
    {
        foreach (var endpoint in resource.Annotations.OfType<EndpointAnnotation>())
        {
            var port = endpoint.TargetPort ?? defaultPort;
            endpoint.AllocatedEndpoint = new AllocatedEndpoint(
                endpoint,
                "localhost",
                port,
                targetPortExpression: port.ToString(CultureInfo.InvariantCulture));
        }
    }
}
