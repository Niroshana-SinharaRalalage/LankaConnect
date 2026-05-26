using LankaConnect.BuildingBlocks.Web.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace LankaConnect.BuildingBlocks.Web.Tests.TelemetryTests;

public class TelemetryExtensionsTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public void AddBuildingBlocksTelemetry_without_connection_string_still_registers_tracer_provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksTelemetry(EmptyConfig(), serviceName: "LankaConnect.Test");

        var provider = services.BuildServiceProvider();
        provider.GetService<TracerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksTelemetry_throws_on_empty_service_name()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddBuildingBlocksTelemetry(EmptyConfig(), serviceName: "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddBuildingBlocksTelemetry_reads_connection_string_from_config_section()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TelemetryExtensions.ConfigKey] =
                    "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example.example/",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBuildingBlocksTelemetry(config, serviceName: "LankaConnect.Test");

        var provider = services.BuildServiceProvider();
        provider.GetService<TracerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigKey_and_env_var_constants_are_stable()
    {
        TelemetryExtensions.ConfigKey.Should().Be("ApplicationInsights:ConnectionString");
        TelemetryExtensions.ConnectionStringEnvVar.Should().Be("APPLICATIONINSIGHTS_CONNECTION_STRING");
    }
}
