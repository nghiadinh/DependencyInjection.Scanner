namespace Microsoft.Extensions.DependencyInjection.Scanner.Abstractions;

/// <summary>
/// Defines a fluent API for configuring assembly scanning and service discovery.
/// </summary>
public interface ITypeScanner
{
    /// <summary>
    /// Scans the types in the assembly that contains the code calling this method.
    /// </summary>
    ITypeScanner TheCallingAssembly();

    /// <summary>
    /// Scans the types in the assembly containing the specified marker type.
    /// </summary>
    ITypeScanner FromAssemblyOf<T>();

    /// <summary>
    /// Registers types using the default convention (e.g. IFoo mapped to Foo).
    /// </summary>
    ITypeScanner WithDefaultConventions();

    /// <summary>
    /// Registers all implementations of the specified service type.
    /// </summary>
    ITypeScanner AddAllTypesOf<T>();

    /// <summary>
    /// Excludes the specified type from registration.
    /// </summary>
    ITypeScanner Exclude<T>();

    /// <summary>
    /// Includes non-public (internal) types in the scanning process.
    /// By default, only public types are scanned.
    /// </summary>
    ITypeScanner IncludeInternalTypes();

    /// <summary>
    /// Registers the implementation type as itself (e.g. AddTransient&lt;Foo, Foo&gt;).
    /// </summary>
    ITypeScanner AsSelf();

    /// <summary>
    /// Registers scanned services with Singleton lifetime.
    /// </summary>
    ITypeScanner WithSingletonLifetime();

    /// <summary>
    /// Registers scanned services with Scoped lifetime.
    /// </summary>
    ITypeScanner WithScopedLifetime();

    /// <summary>
    /// Registers scanned services with Transient lifetime (default).
    /// </summary>
    ITypeScanner WithTransientLifetime();
}
