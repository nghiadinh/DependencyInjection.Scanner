namespace Microsoft.Extensions.DependencyInjection.Scanner.Model;

/// <summary>
/// Represents a single service registration discovered during scanning.
/// This model is strictly serializable for Incremental Generator caching.
/// </summary>
public sealed record RegistrationDefinition(
    string ServiceType,
    string ImplementationType,
    string Lifetime
);

/// <summary>
/// Defines a set of instructions for scanning assemblies and registering types.
/// </summary>
public sealed record ScanInstruction(
    bool ScanCallingAssembly,
    IReadOnlyList<string> TargetAssemblies,
    IReadOnlyList<RegistrationRule> Rules
);

/// <summary>
/// Defines the rules for mapping types within a scanned assembly.
/// </summary>
public sealed record RegistrationRule(
    bool UseDefaultConventions,
    IReadOnlyList<string> AddAllTypesOf,
    IReadOnlyList<string> ExcludedTypes,
    bool IncludeInternal,
    bool IsAsSelf,
    string Lifetime
);
