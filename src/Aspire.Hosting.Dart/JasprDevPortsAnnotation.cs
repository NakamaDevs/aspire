// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds the two development ports of <c>jaspr serve</c>.
/// </summary>
/// <remarks>
/// The Jaspr defaults are 5467 for the web port and 5567 for the proxy port. Two Jaspr applications
/// in one distributed application collide on those defaults, so the developer gives each one its own
/// pair.
/// </remarks>
internal sealed class JasprDevPortsAnnotation(int? webPort, int? proxyPort) : IResourceAnnotation
{
    /// <summary>The value of the <c>--web-port</c> option, or <see langword="null"/> for the Jaspr default.</summary>
    public int? WebPort { get; } = webPort;

    /// <summary>The value of the <c>--proxy-port</c> option, or <see langword="null"/> for the Jaspr default.</summary>
    public int? ProxyPort { get; } = proxyPort;
}
