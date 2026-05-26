using LankaConnect.BuildingBlocks.Web.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LankaConnect.BuildingBlocks.Web.Tests.HealthChecksTests;

public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksHealthChecks_with_no_connection_strings_registers_only_the_default_check()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksHealthChecks();

        var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<HealthCheckService>();

        // No explicit checks added — service still resolves.
        registrations.Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksHealthChecks_with_postgres_connection_string_registers_postgres_check()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksHealthChecks(
            postgresConnectionString: "Host=localhost;Database=test;Username=test;Password=test");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.Should().Contain(r => r.Name == "postgres");
        options.Registrations.Single(r => r.Name == "postgres").Tags
            .Should().Contain(HealthCheckExtensions.ReadinessTag);
    }

    [Fact]
    public void AddBuildingBlocksHealthChecks_with_redis_connection_string_registers_redis_check_as_degraded()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksHealthChecks(
            redisConnectionString: "localhost:6379");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value;

        var redis = options.Registrations.Single(r => r.Name == "redis");
        redis.FailureStatus.Should().Be(HealthStatus.Degraded);
        redis.Tags.Should().Contain(HealthCheckExtensions.ReadinessTag);
    }

    [Fact]
    public void ReadinessTag_and_LivenessTag_constants_are_stable()
    {
        HealthCheckExtensions.ReadinessTag.Should().Be("ready");
        HealthCheckExtensions.LivenessTag.Should().Be("live");
    }
}
