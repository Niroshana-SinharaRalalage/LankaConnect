using LankaConnect.BuildingBlocks.Web.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LankaConnect.BuildingBlocks.Web.Tests.RateLimitingTests;

public class RateLimitingExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksRateLimiter_registers_rate_limiter_options()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksRateLimiter();

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<RateLimiterOptions>>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksRateLimiter_allows_host_to_add_additional_policies()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksRateLimiter(opts =>
        {
            opts.AddFixedWindowLimiter("test-host-policy", o =>
            {
                o.PermitLimit = 5;
                o.Window = TimeSpan.FromSeconds(10);
            });
        });

        var provider = services.BuildServiceProvider();
        provider.GetService<IOptions<RateLimiterOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void PerIpFixedWindowPolicy_constant_is_stable()
    {
        RateLimitingExtensions.PerIpFixedWindowPolicy.Should().Be("perip-fixedwindow");
    }
}
