using FluentAssertions;
using LankaConnect.Application.Common.Models.ConnectionPool;

namespace LankaConnect.Application.Tests.Common.Models;

public class ConfigurationImportResultTests
{
    [Fact]
    public void ConfigurationImportResult_Should_Exist_And_Be_Instantiable()
    {
        // Arrange & Act
        var result = new ConfigurationImportResult();
        
        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ConfigurationImportResult_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var result = new ConfigurationImportResult();
        
        // Assert
        result.Should().NotBeNull();
        result.GetType().GetProperty("IsSuccess").Should().NotBeNull();
        result.GetType().GetProperty("ImportedConfigurationCount").Should().NotBeNull();
    }
}