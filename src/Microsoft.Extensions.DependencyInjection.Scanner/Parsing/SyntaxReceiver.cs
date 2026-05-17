using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Parsing;

/// <summary>
/// Provides high-performance syntax filtering for identifying scanning invocations.
/// </summary>
public static class SyntaxFilters
{
    /// <summary>
    /// Identifies whether a syntax node is a candidate for a .Scan(...) call.
    /// </summary>
    public static bool IsScanInvocation(SyntaxNode node, System.Threading.CancellationToken cancellationToken)
    {
        return node is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax member &&
               member.Name.Identifier.Text == "Scan";
    }
}
