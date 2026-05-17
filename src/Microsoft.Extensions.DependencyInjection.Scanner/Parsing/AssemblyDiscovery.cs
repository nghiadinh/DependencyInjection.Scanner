using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Parsing;

/// <summary>
/// Provides utility methods for discovering assemblies and types within the compilation.
/// </summary>
public static class AssemblyDiscovery
{
    /// <summary>
    /// Recursively retrieves all types within a namespace.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            yield return type;
        }

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNamespace))
            {
                yield return type;
            }
        }
    }
}
