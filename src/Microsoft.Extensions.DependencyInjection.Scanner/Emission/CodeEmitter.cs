using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Scanner.Model;
using Microsoft.Extensions.DependencyInjection.Scanner.Parsing;
using System.Collections.Immutable;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Emission;

/// <summary>
/// Handles the discovery of registrations and emission of C# source code.
/// Resolves serializable models back to symbols during the emission phase.
/// </summary>
public static class CodeEmitter
{
    public static void Emit(Compilation compilation, ImmutableArray<ScanInstruction> instructions, SourceProductionContext context)
    {
        if (instructions.IsDefaultOrEmpty) return;

        var registrations = DiscoverRegistrations(compilation, instructions);
        if (registrations.Count == 0) return;

        var source = SourceBuilder.Build(registrations);
        context.AddSource("ScannerRegistrations.g.cs", source);
    }

    private static HashSet<RegistrationDefinition> DiscoverRegistrations(Compilation compilation, ImmutableArray<ScanInstruction> instructions)
    {
        var registrations = new HashSet<RegistrationDefinition>();

        foreach (var instruction in instructions)
        {
            var targetAssemblies = ResolveAssemblies(compilation, instruction);

            foreach (var assembly in targetAssemblies)
            {
                var allTypes = AssemblyDiscovery.GetAllTypes(assembly.GlobalNamespace).ToList();

                foreach (var rule in instruction.Rules)
                {
                    foreach (var type in allTypes)
                    {
                        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
                            continue;

                        // Check visibility
                        if (type.DeclaredAccessibility != Accessibility.Public && !rule.IncludeInternal)
                            continue;

                        string implementationTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        // Check exclusion
                        if (rule.ExcludedTypes.Contains(implementationTypeName))
                            continue;

                        // 1. AsSelf
                        if (rule.IsAsSelf)
                        {
                            registrations.Add(new RegistrationDefinition(implementationTypeName, implementationTypeName, rule.Lifetime));
                        }

                        // 2. Default Conventions
                        if (rule.UseDefaultConventions)
                        {
                            foreach (var @interface in type.AllInterfaces)
                            {
                                if (@interface.Name == $"I{type.Name}")
                                {
                                    registrations.Add(new RegistrationDefinition(
                                        @interface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                        implementationTypeName,
                                        rule.Lifetime));
                                }
                            }
                        }

                        // 3. AddAllTypesOf
                        foreach (var serviceTypeName in rule.AddAllTypesOf)
                        {
                            if (IsImplementationOf(type, serviceTypeName))
                            {
                                registrations.Add(new RegistrationDefinition(serviceTypeName, implementationTypeName, rule.Lifetime));
                            }
                        }
                    }
                }
            }
        }

        return registrations;
    }

    private static HashSet<IAssemblySymbol> ResolveAssemblies(Compilation compilation, ScanInstruction instruction)
    {
        var assemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        if (instruction.ScanCallingAssembly) assemblies.Add(compilation.Assembly);

        var referencedAssemblies = compilation.SourceModule.ReferencedAssemblySymbols;
        foreach (var targetName in instruction.TargetAssemblies)
        {
            var found = referencedAssemblies.FirstOrDefault(a => a.Name == targetName);
            if (found != null) assemblies.Add(found);
            else if (compilation.Assembly.Name == targetName) assemblies.Add(compilation.Assembly);
        }

        return assemblies;
    }

    private static bool IsImplementationOf(INamedTypeSymbol type, string serviceTypeName)
    {
        // Compare by FullyQualifiedFormat display string
        if (type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == serviceTypeName) return true;

        if (type.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == serviceTypeName)) return true;

        var current = type.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == serviceTypeName) return true;
            current = current.BaseType;
        }

        return false;
    }
}
