// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dart;

/// <summary>
/// The rendering mode of a Jaspr application. The <c>jaspr</c> block of <c>pubspec.yaml</c> holds
/// the same value.
/// </summary>
public enum JasprMode
{
    /// <summary>
    /// Jaspr renders the pages one time and writes static files. The application needs no server.
    /// </summary>
    Static,

    /// <summary>
    /// Jaspr renders the pages on a Dart server for each request.
    /// </summary>
    Server,

    /// <summary>
    /// Jaspr renders the pages in the browser. The application ships a client bundle only.
    /// </summary>
    Client,
}
