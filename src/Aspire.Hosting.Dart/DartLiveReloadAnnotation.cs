// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Marks a Dart application resource that restarts when its source files change.
/// </summary>
internal sealed class DartLiveReloadAnnotation : IResourceAnnotation;
