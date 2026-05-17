using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Scanner.Tests;

public interface IRuntimeService { }
public class RuntimeService : IRuntimeService { }

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Scan_ShouldRegisterServices_WhenCalledWithDefaultConventions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        // The Source Generator emits ScannerRegistrations.AddScannedServices
        services.Scan(s => s.TheCallingAssembly().WithDefaultConventions());
        ScannerRegistrations.AddScannedServices(services);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<IRuntimeService>();

        Assert.NotNull(service);
        Assert.IsType<RuntimeService>(service);
    }
}
