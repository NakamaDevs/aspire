// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Marks an application that watches its own source files and reloads without a restart.
/// </summary>
/// <remarks>
/// <c>jaspr serve</c> holds a file watcher and a hot reload channel. An Aspire restart on a file
/// change would stop that work, so the Dart live reload support must keep the restart off for a
/// resource that carries this annotation.
/// </remarks>
internal sealed class JasprSelfReloadsAnnotation : IResourceAnnotation;
