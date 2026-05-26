using LankaConnect.BuildingBlocks.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Web.Tests.Authentication;

public class JwtAuthenticationExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_with_valid_settings_registers_bearer_scheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(new()
        {
            ["Jwt:Key"] = "this-is-a-32-char-test-secret!!!!",
            ["Jwt:Issuer"] = "lankaconnect-tests",
            ["Jwt:Audience"] = "lankaconnect-clients",
        });

        services.AddBuildingBlocksJwtAuthentication(config);
        var provider = services.BuildServiceProvider();

        provider.GetService<IAuthenticationSchemeProvider>().Should().NotBeNull();
        provider.GetService<IAuthorizationService>().Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_throws_when_key_missing()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new()
        {
            ["Jwt:Issuer"] = "lankaconnect-tests",
            ["Jwt:Audience"] = "lankaconnect-clients",
        });

        Action act = () => services.AddBuildingBlocksJwtAuthentication(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Key*");
    }

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_throws_when_issuer_missing()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new()
        {
            ["Jwt:Key"] = "this-is-a-32-char-test-secret!!!!",
            ["Jwt:Audience"] = "lankaconnect-clients",
        });

        Action act = () => services.AddBuildingBlocksJwtAuthentication(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Issuer*");
    }

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_throws_when_audience_missing()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new()
        {
            ["Jwt:Key"] = "this-is-a-32-char-test-secret!!!!",
            ["Jwt:Issuer"] = "lankaconnect-tests",
        });

        Action act = () => services.AddBuildingBlocksJwtAuthentication(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Audience*");
    }

    [Fact]
    public void JwtSettings_section_name_constant_matches_microsoft_convention()
    {
        JwtSettings.SectionName.Should().Be("Jwt");
    }
}
