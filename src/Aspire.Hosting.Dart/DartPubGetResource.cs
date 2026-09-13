// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// A resource that runs <c>dart pub get</c> for a Dart application before the application starts.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The Dart application resource that owns this setup step.</param>
internal sealed class DartPubGetResource(string name, DartAppResource parent)
    : ExecutableResource(name, "dart", parent.WorkingDirectory)
{
}
