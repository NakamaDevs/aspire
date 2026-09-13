// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

internal sealed class DartAppArgsAnnotation(object[] args) : IResourceAnnotation
{
    public object[] Args { get; } = args;
}
