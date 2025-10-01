using FluentAssertions;
using LankaConnect.Application.Common.Models.ConnectionPool;

namespace LankaConnect.Application.Tests.Common.Models;

public class ConfigurationImportParametersTests
{
    [Fact]
    public void ConfigurationImportParameters_Should_Exist_And_Be_Instantiable()
    {
        // Arrange & Act
        var parameters = new ConfigurationImportParameters();
        
        // Assert
        parameters.Should().NotBeNull();
    }

    [Fact]
    public void ConfigurationImportParameters_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var parameters = new ConfigurationImportParameters();
        
        // Assert
        parameters.Should().NotBeNull();
        parameters.GetType().GetProperty("ImportSource").Should().NotBeNull();
        parameters.GetType().GetProperty("ValidateBeforeImport").Should().NotBeNull();
    }
}