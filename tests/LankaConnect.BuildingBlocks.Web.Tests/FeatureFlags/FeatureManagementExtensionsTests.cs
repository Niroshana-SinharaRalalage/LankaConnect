using LankaConnect.BuildingBlocks.Web.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace LankaConnect.BuildingBlocks.Web.Tests.FeatureFlagsTests;

public class FeatureManagementExtensionsTests
{
    [Fact]
    public async Task AddBuildingBlocksFeatureManagement_registers_feature_manager()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:Refactor.NotificationsNewPath"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksFeatureManagement(config);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IFeatureManager>();
        manager.Should().NotBeNull();

        (await manager!.IsEnabledAsync("Refactor.NotificationsNewPath")).Should().BeFalse();
    }

    [Fact]
    public async Task AddBuildingBlocksFeatureManagement_reads_enabled_flag_value()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:Refactor.NotificationsNewPath"] = "true",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksFeatureManagement(config);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IFeatureManager>();
        (await manager.IsEnabledAsync("Refactor.NotificationsNewPath")).Should().BeTrue();
    }

    [Fact]
    public void DefaultSectionName_constant_matches_microsoft_convention()
    {
        FeatureManagementExtensions.DefaultSectionName.Should().Be("FeatureManagement");
    }
}
