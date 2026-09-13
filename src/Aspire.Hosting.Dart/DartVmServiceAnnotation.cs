// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Marks a Dart application that starts the Dart VM service.
/// </summary>
/// <param name="Port">The port of the VM service, or <see langword="null"/> for a free port.</param>
internal sealed record DartVmServiceAnnotation(int? Port) : IResourceAnnotation
{
    /// <summary>The option that starts the VM service.</summary>
    private const string Option = "--enable-vm-service";

    /// <summary>
    /// Builds the <c>dart run</c> option for this annotation.
    /// </summary>
    /// <remarks>
    /// <c>--enable-vm-service</c> without a value makes the VM select a free port. The form
    /// <c>--enable-vm-service=&lt;port&gt;</c> binds the port that the developer named.
    /// </remarks>
    public string ToOption()
        => Port is { } port
            ? $"{Option}={port.ToString(CultureInfo.InvariantCulture)}"
            : Option;
}
