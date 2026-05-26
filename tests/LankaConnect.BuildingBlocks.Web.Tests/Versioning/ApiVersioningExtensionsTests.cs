using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using LankaConnect.BuildingBlocks.Web.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.BuildingBlocks.Web.Tests.VersioningTests;

public class ApiVersioningExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksApiVersioning_registers_api_version_description_provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksApiVersioning();

        var provider = services.BuildServiceProvider();
        provider.GetService<IApiVersionDescriptionProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksApiVersioning_returns_builder_for_chaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var builder = services.AddBuildingBlocksApiVersioning();

        builder.Should().NotBeNull();
        builder.Services.Should().BeSameAs(services);
    }
}
