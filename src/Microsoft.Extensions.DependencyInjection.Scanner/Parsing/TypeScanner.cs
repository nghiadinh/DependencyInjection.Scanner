using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection.Scanner.Model;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Parsing;

/// <summary>
/// Orchestrates the semantic analysis of scanning lambda expressions.
/// Produces a serializable model for the incremental pipeline.
/// </summary>
public static class TypeScanner
{
    public static ScanInstruction? AnalyzeScanInvocation(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.ArgumentList.Arguments.Count == 0) return null;

        var argument = invocation.ArgumentList.Arguments[0].Expression;
        if (argument is not LambdaExpressionSyntax lambda) return null;

        string? paramName = GetParamName(lambda);
        if (paramName == null) return null;

        bool scanCallingAssembly = false;
        var targetAssemblies = new List<string>();
        var rules = new List<RegistrationRule>();

        var currentRuleBuilder = new RuleBuilder();

        var descendantNodes = lambda.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Reverse();

        foreach (var node in descendantNodes)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var root = GetRootExpression(memberAccess);
                if (root is IdentifierNameSyntax id && id.Identifier.Text == paramName)
                {
                    ProcessMethodCall(context, memberAccess, ref scanCallingAssembly, targetAssemblies, ref currentRuleBuilder, rules);
                }
            }
        }

        rules.Add(currentRuleBuilder.ToRule());

        return new ScanInstruction(scanCallingAssembly, targetAssemblies.ToImmutableList(), rules.ToImmutableList());
    }

    private static string? GetParamName(LambdaExpressionSyntax lambda)
    {
        if (lambda is SimpleLambdaExpressionSyntax simple)
            return simple.Parameter.Identifier.Text;
        if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized && parenthesized.ParameterList.Parameters.Count > 0)
            return parenthesized.ParameterList.Parameters[0].Identifier.Text;
        return null;
    }

    private static void ProcessMethodCall(
        GeneratorSyntaxContext context,
        MemberAccessExpressionSyntax memberAccess,
        ref bool scanCallingAssembly,
        List<string> targetAssemblies,
        ref RuleBuilder ruleBuilder,
        List<RegistrationRule> rules)
    {
        var methodName = memberAccess.Name.Identifier.Text;

        switch (methodName)
        {
            case "TheCallingAssembly":
                scanCallingAssembly = true;
                break;
            case "FromAssemblyOf":
                if (memberAccess.Name is GenericNameSyntax genericAssembly)
                {
                    var typeSyntax = genericAssembly.TypeArgumentList.Arguments[0];
                    var typeSymbol = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
                    if (typeSymbol?.ContainingAssembly != null)
                        targetAssemblies.Add(typeSymbol.ContainingAssembly.Name);
                }
                break;
            case "WithDefaultConventions":
                ruleBuilder.UseDefaultConventions = true;
                break;
            case "AddAllTypesOf":
                if (ruleBuilder.IsDirty)
                {
                    rules.Add(ruleBuilder.ToRule());
                    ruleBuilder = new RuleBuilder();
                }

                if (memberAccess.Name is GenericNameSyntax genericType)
                {
                    var typeSyntax = genericType.TypeArgumentList.Arguments[0];
                    var typeSymbol = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
                    if (typeSymbol != null)
                        ruleBuilder.AddAllTypesOf.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
                break;
            case "Exclude":
                if (memberAccess.Name is GenericNameSyntax genericExclude)
                {
                    var typeSyntax = genericExclude.TypeArgumentList.Arguments[0];
                    var typeSymbol = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
                    if (typeSymbol != null)
                        ruleBuilder.ExcludedTypes.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
                break;
            case "IncludeInternalTypes":
                ruleBuilder.IncludeInternal = true;
                break;
            case "AsSelf":
                ruleBuilder.IsAsSelf = true;
                break;
            case "WithSingletonLifetime":
                ruleBuilder.Lifetime = "Singleton";
                break;
            case "WithScopedLifetime":
                ruleBuilder.Lifetime = "Scoped";
                break;
            case "WithTransientLifetime":
                ruleBuilder.Lifetime = "Transient";
                break;
        }
    }

    private static ExpressionSyntax GetRootExpression(ExpressionSyntax expression)
    {
        var current = expression;
        while (current is MemberAccessExpressionSyntax or InvocationExpressionSyntax)
        {
            if (current is MemberAccessExpressionSyntax nextMember)
                current = nextMember.Expression;
            else if (current is InvocationExpressionSyntax nextInvocation)
                current = nextInvocation.Expression;
        }
        return current;
    }

    private class RuleBuilder
    {
        public bool UseDefaultConventions { get; set; }
        public List<string> AddAllTypesOf { get; } = new();
        public List<string> ExcludedTypes { get; } = new();
        public bool IncludeInternal { get; set; }
        public bool IsAsSelf { get; set; }
        public string Lifetime { get; set; } = "Transient";

        public bool IsDirty => UseDefaultConventions || AddAllTypesOf.Count > 0 || ExcludedTypes.Count > 0 || IncludeInternal || IsAsSelf;

        public RegistrationRule ToRule() => new RegistrationRule(
            UseDefaultConventions,
            AddAllTypesOf.ToImmutableList(),
            ExcludedTypes.ToImmutableList(),
            IncludeInternal,
            IsAsSelf,
            Lifetime);
    }
}

internal static class ListExtensions
{
    public static IReadOnlyList<T> ToImmutableList<T>(this IEnumerable<T> source) => source.ToList().AsReadOnly();
}
