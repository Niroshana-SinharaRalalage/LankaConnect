using FluentAssertions;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.ValueObjects;

/// <summary>
/// TDD RED Phase: Tests for ReportingConfiguration ValueObject
/// These tests should FAIL until we implement the ReportingConfiguration class
/// Focus on enterprise-grade reporting with cultural intelligence awareness
/// </summary>
public class ReportingConfigurationTests
{
    [Fact]
    public void ReportingConfiguration_Create_ShouldCreateValidConfiguration()
    {
        // Arrange
        var reportTitle = "Security Analysis Report";
        var format = ReportFormat.PDF;
        var includeCharts = true;
        var culturalContext = "Sri Lankan Community";
        
        // Act
        var config = ReportingConfiguration.Create(reportTitle, format, includeCharts, culturalContext);
        
        // Assert
        config.Should().NotBeNull();
        config.ReportTitle.Should().Be(reportTitle);
        config.Format.Should().Be(format);
        config.IncludeCharts.Should().BeTrue();
        config.CulturalContext.Should().Be(culturalContext);
    }
    
    [Fact]
    public void ReportingConfiguration_CreateWithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var reportTitle = "Default Report";
        
        // Act
        var config = ReportingConfiguration.CreateDefault(reportTitle);
        
        // Assert
        config.Should().NotBeNull();
        config.ReportTitle.Should().Be(reportTitle);
        config.Format.Should().Be(ReportFormat.JSON);
        config.IncludeCharts.Should().BeFalse();
        config.CulturalContext.Should().BeNull();
        config.IsDefault.Should().BeTrue();
    }
    
    [Fact]
    public void ReportingConfiguration_WithCulturalIntelligence_ShouldDetectCulturalMetrics()
    {
        // Arrange
        var culturalContext = "Vesak Day Performance Analysis";
        var config = ReportingConfiguration.Create("Cultural Report", ReportFormat.PDF, true, culturalContext);
        
        // Act
        var hasCulturalMetrics = config.HasCulturalMetrics;
        
        // Assert
        hasCulturalMetrics.Should().BeTrue();
        config.CulturalContext.Should().Contain("Vesak");
    }
    
    [Theory]
    [InlineData(ReportFormat.PDF)]
    [InlineData(ReportFormat.Excel)]
    [InlineData(ReportFormat.JSON)]
    [InlineData(ReportFormat.XML)]
    [InlineData(ReportFormat.CSV)]
    [InlineData(ReportFormat.HTML)]
    public void ReportingConfiguration_WithDifferentFormats_ShouldAcceptAllValidFormats(ReportFormat format)
    {
        // Arrange & Act
        var config = ReportingConfiguration.Create("Test Report", format, false, null);
        
        // Assert
        config.Format.Should().Be(format);
        config.SupportedFormat.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ReportingConfiguration_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Act & Assert
        var act = () => ReportingConfiguration.Create(invalidTitle, ReportFormat.PDF, false, null);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Report title cannot be null or empty*");
    }
    
    [Fact]
    public void ReportingConfiguration_GetReportParameters_ShouldReturnConfiguredParameters()
    {
        // Arrange
        var config = ReportingConfiguration.Create("Performance Report", ReportFormat.Excel, true, "Diaspora Community");
        
        // Act
        var parameters = config.GetReportParameters();
        
        // Assert
        parameters.Should().NotBeNull();
        parameters.Should().ContainKey("Title");
        parameters.Should().ContainKey("Format");
        parameters.Should().ContainKey("IncludeCharts");
        parameters.Should().ContainKey("CulturalContext");
        parameters["Title"].Should().Be("Performance Report");
    }
    
    [Fact]
    public void ReportingConfiguration_Equality_ShouldBeEqualForSameConfiguration()
    {
        // Arrange
        var config1 = ReportingConfiguration.Create("Test Report", ReportFormat.PDF, true, "Cultural Context");
        var config2 = ReportingConfiguration.Create("Test Report", ReportFormat.PDF, true, "Cultural Context");
        
        // Act & Assert
        config1.Should().Be(config2);
        (config1 == config2).Should().BeTrue();
        config1.GetHashCode().Should().Be(config2.GetHashCode());
    }
    
    [Fact]
    public void ReportingConfiguration_ToString_ShouldProvideReadableFormat()
    {
        // Arrange
        var config = ReportingConfiguration.Create("Security Analysis", ReportFormat.PDF, true, "Sri Lankan Community");
        
        // Act
        var result = config.ToString();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Security Analysis");
        result.Should().Contain("PDF");
        result.Should().Contain("Sri Lankan Community");
    }
}