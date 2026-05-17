using System;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Abstractions.Attributes;

/// <summary>
/// Marks a type or assembly as a target for automated dependency scanning.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ScanMarkerAttribute : Attribute
{
}
