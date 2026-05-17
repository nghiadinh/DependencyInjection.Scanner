using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Scanner.Emission;
using Microsoft.Extensions.DependencyInjection.Scanner.Parsing;

namespace Microsoft.Extensions.DependencyInjection.Scanner;

/// <summary>
/// The main entry point for the DependencyInjection.Scanner source generator.
/// Orchestrates the incremental pipeline from syntax discovery to code emission.
/// </summary>
[Generator]
public sealed class ScannerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Discovery: Filter for .Scan(...) invocations
        var scanInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntaxFilters.IsScanInvocation,
                transform: static (ctx, _) => TypeScanner.AnalyzeScanInvocation(ctx))
            .Where(static m => m is not null);

        // 2. Compilation: Combine with the compilation provider
        var compilationAndInvocations = context.CompilationProvider.Combine(scanInvocations.Collect());

        // 3. Emission: Register the source output
        context.RegisterSourceOutput(
            compilationAndInvocations,
            static (spc, source) => CodeEmitter.Emit(source.Left, source.Right!, spc));
    }
}
