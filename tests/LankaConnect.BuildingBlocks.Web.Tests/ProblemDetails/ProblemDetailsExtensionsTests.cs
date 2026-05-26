using LankaConnect.BuildingBlocks.Web.ProblemDetails;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Web.Tests.ProblemDetailsTests;

public class ProblemDetailsExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksProblemDetails_registers_global_exception_handler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksProblemDetails();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IExceptionHandler>();

        handler.Should().NotBeNull();
        handler.Should().BeOfType<GlobalExceptionHandler>();
    }
}
