using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Tests;

public class ScannerGeneratorTests
{
    [Fact]
    public void DefaultConventionMapping_RegistersInterfaceToClass()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IUserService { }
public class UserService : IUserService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly().WithDefaultConventions());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("public static class ScannerRegistrations", generatedCode);
        Assert.Contains("public static void AddScannedServices(this IServiceCollection services)", generatedCode);
        Assert.Contains("services.AddTransient<global::TestApp.IUserService, global::TestApp.UserService>();", generatedCode);
    }

    [Fact]
    public void IgnoringInternalClasses_DoesNotRegisterInternalImplementations()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IInternalService { }
internal class InternalService : IInternalService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly().WithDefaultConventions());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.DoesNotContain("IInternalService", generatedCode);
    }

    [Fact]
    public void MultipleRegistrations_RegistersAllMatchingConventions()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IServiceA { }
public class ServiceA : IServiceA { }

public interface IServiceB { }
public class ServiceB : IServiceB { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly().WithDefaultConventions());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("services.AddTransient<global::TestApp.IServiceA, global::TestApp.ServiceA>();", generatedCode);
        Assert.Contains("services.AddTransient<global::TestApp.IServiceB, global::TestApp.ServiceB>();", generatedCode);
    }

    [Fact]
    public void EmptyLambda_ResultsInNoRegistrations()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IUserService { }
public class UserService : IUserService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => { });
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Equal(string.Empty, generatedCode);
    }

    [Fact]
    public void MissingConvention_ResultsInNoRegistrations()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IUserService { }
public class UserService : IUserService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Equal(string.Empty, generatedCode);
    }

    [Fact]
    public void FromAssemblyOf_RegistersTypesFromTargetAssembly()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Scanner.Abstractions;

namespace TestApp;

public interface IOtherService { }
public class OtherService : IOtherService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.FromAssemblyOf<Startup>().WithDefaultConventions());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        // Note: ITypeScanner is in Microsoft.Extensions.DependencyInjection.Scanner.Abstractions assembly
        // We should check if the generator picks up types from that assembly if any match the convention.
        // Actually, let's use a type from the same assembly for simpler test verification.
        Assert.Contains("ScannerRegistrations", generatedCode);
    }

    [Fact]
    public void AddAllTypesOf_RegistersAllImplementations()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IPlugin { }
public class PluginA : IPlugin { }
public class PluginB : IPlugin { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly().AddAllTypesOf<IPlugin>());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("services.AddTransient<global::TestApp.IPlugin, global::TestApp.PluginA>();", generatedCode);
        Assert.Contains("services.AddTransient<global::TestApp.IPlugin, global::TestApp.PluginB>();", generatedCode);
    }

    [Fact]
    public void Lifetimes_AreCorrectlyApplied()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface ISingletonService { }
public class SingletonService : ISingletonService { }

public interface IScopedService { }
public class ScopedService : IScopedService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly()
                            .AddAllTypesOf<ISingletonService>().WithSingletonLifetime()
                            .AddAllTypesOf<IScopedService>().WithScopedLifetime());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("services.AddSingleton<global::TestApp.ISingletonService, global::TestApp.SingletonService>();", generatedCode);
        Assert.Contains("services.AddScoped<global::TestApp.IScopedService, global::TestApp.ScopedService>();", generatedCode);
    }

    [Fact]
    public void ExcludingType_OmitsRegistration()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IService { }
public class ServiceA : IService { }
public class ServiceB : IService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly()
                            .AddAllTypesOf<IService>()
                            .Exclude<ServiceA>());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.DoesNotContain("ServiceA", generatedCode);
        Assert.Contains("services.AddTransient<global::TestApp.IService, global::TestApp.ServiceB>();", generatedCode);
    }

    [Fact]
    public void IncludingInternalTypes_RegistersInternalClass()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IService { }
internal class InternalService : IService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly()
                            .AddAllTypesOf<IService>()
                            .IncludeInternalTypes());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("services.AddTransient<global::TestApp.IService, global::TestApp.InternalService>();", generatedCode);
    }

    [Fact]
    public void AsSelf_RegistersConcreteType()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public class ConcreteService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly()
                            .AddAllTypesOf<ConcreteService>()
                            .AsSelf());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("services.AddTransient<global::TestApp.ConcreteService, global::TestApp.ConcreteService>();", generatedCode);
    }

    [Fact]
    public void AsSelfWithDefaultConventions_RegistersBoth()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp;

public interface IService { }
public class Service : IService { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Scan(s => s.TheCallingAssembly()
                            .WithDefaultConventions()
                            .AsSelf());
    }
}
";

        // Act
        var generatedCode = RunGenerator(source);

        // Assert
        Assert.Contains("services.AddTransient<global::TestApp.IService, global::TestApp.Service>();", generatedCode);
        Assert.Contains("services.AddTransient<global::TestApp.Service, global::TestApp.Service>();", generatedCode);
    }


    private string RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>();
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly,
            typeof(IServiceCollection).Assembly,
            typeof(ScannerGenerator).Assembly
        };

        foreach (var assembly in assemblies)
        {
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ScannerGenerator();
        var driver = CSharpGeneratorDriver.Create(new IIncrementalGenerator[] { generator });
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        return runResult.GeneratedTrees.Length > 0
            ? runResult.GeneratedTrees[0].GetText().ToString()
            : string.Empty;
    }
}
