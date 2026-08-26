// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dart;

/// <summary>
/// Holds one compile-time environment value that <c>dart run --define</c> supplies to the program.
/// </summary>
/// <remarks>
/// A resource carries one annotation for each key. The keys accumulate, so a resource can hold more
/// than one annotation. A second call with the same key replaces the annotation of the first call.
/// </remarks>
/// <param name="key">The name that <c>String.fromEnvironment</c> reads.</param>
/// <param name="value">The value of the key.</param>
internal sealed class DartDefineAnnotation(string key, string value) : IResourceAnnotation
{
    public string Key { get; } = key;

    public string Value { get; } = value;
}
