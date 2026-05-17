using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Diagnostics;

/// <summary>
/// Centralized definitions for all diagnostics reported by the scanner.
/// </summary>
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor InternalError = new(
        id: "DISCAN001",
        title: "Internal Scanner Error",
        messageFormat: "An internal error occurred during DI scanning: {0}",
        category: "Scanner",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
