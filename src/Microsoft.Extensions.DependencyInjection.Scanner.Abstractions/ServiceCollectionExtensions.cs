using System;
using Microsoft.Extensions.DependencyInjection.Scanner.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for IServiceCollection to enable compile-time assembly scanning.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures assembly scanning for service registration.
    /// This method is processed at compile-time by a Source Generator.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="action">The scanning configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection Scan(this IServiceCollection services, Action<ITypeScanner> action)
    {
        // This method is a hook for the Source Generator.
        // It does not execute the 'action' at runtime; the generator 
        // parses the syntax tree of the call site.
        return services;
    }
}
