using FluentAssertions;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.ValueObjects;

/// <summary>
/// TDD RED Phase: Tests for SLAReportingConfiguration ValueObject
/// These tests should FAIL until we implement the SLAReportingConfiguration class
/// Focus on SLA monitoring and compliance reporting with cultural intelligence
/// </summary>
public class SLAReportingConfigurationTests
{
    [Fact]
    public void SLAReportingConfiguration_Create_ShouldCreateValidConfiguration()
    {
        // Arrange
        var reportTitle = "SLA Compliance Report";
        var format = ReportFormat.Excel;
        var includeMetrics = true;
        var slaThreshold = 99.9;
        var culturalContext = "Festival Season Analysis";
        
        // Act
        var config = SLAReportingConfiguration.Create(reportTitle, format, includeMetrics, slaThreshold, culturalContext);
        
        // Assert
        config.Should().NotBeNull();
        config.ReportTitle.Should().Be(reportTitle);
        config.Format.Should().Be(format);
        config.IncludeMetrics.Should().BeTrue();
        config.SLAThreshold.Should().Be(slaThreshold);
        config.CulturalContext.Should().Be(culturalContext);
    }
    
    [Fact]
    public void SLAReportingConfiguration_CreateDefault_ShouldUseEnterpriseDefaults()
    {
        // Arrange
        var reportTitle = "Default SLA Report";
        
        // Act
        var config = SLAReportingConfiguration.CreateDefault(reportTitle);
        
        // Assert
        config.Should().NotBeNull();
        config.ReportTitle.Should().Be(reportTitle);
        config.Format.Should().Be(ReportFormat.PDF);
        config.IncludeMetrics.Should().BeTrue();
        config.SLAThreshold.Should().Be(99.5); // Standard enterprise SLA
        config.IsEnterpriseGrade.Should().BeTrue();
    }
    
    [Fact]
    public void SLAReportingConfiguration_WithFortune500Requirements_ShouldMeetHighestStandards()
    {
        // Arrange
        var config = SLAReportingConfiguration.CreateFortune500("Fortune 500 SLA Report");
        
        // Act & Assert
        config.Should().NotBeNull();
        config.SLAThreshold.Should().BeGreaterOrEqualTo(99.9);
        config.IsFortune500Compliant.Should().BeTrue();
        config.IncludeMetrics.Should().BeTrue();
        config.Format.Should().Be(ReportFormat.PDF);
    }
    
    [Theory]
    [InlineData(95.0, false)]  // Below enterprise grade
    [InlineData(99.0, false)]  // Below Fortune 500
    [InlineData(99.5, true)]   // Enterprise grade
    [InlineData(99.9, true)]   // Fortune 500 grade
    [InlineData(99.99, true)]  // Ultra high availability
    public void SLAReportingConfiguration_WithDifferentThresholds_ShouldClassifyCorrectly(double threshold, bool isEnterpriseGrade)
    {
        // Arrange & Act
        var config = SLAReportingConfiguration.Create("SLA Test", ReportFormat.JSON, true, threshold, null);
        
        // Assert
        config.IsEnterpriseGrade.Should().Be(isEnterpriseGrade);
        config.SLAThreshold.Should().Be(threshold);
    }
    
    [Fact]
    public void SLAReportingConfiguration_WithCulturalEvents_ShouldAdjustThresholds()
    {
        // Arrange
        var culturalContext = "Vesak Day Traffic Spike";
        var baseThreshold = 99.5;
        
        // Act
        var config = SLAReportingConfiguration.Create("Cultural SLA", ReportFormat.PDF, true, baseThreshold, culturalContext);
        var adjustedThreshold = config.GetCulturallyAdjustedThreshold();
        
        // Assert
        adjustedThreshold.Should().BeLessThan(baseThreshold); // Relaxed during cultural events
        config.HasCulturalAdjustments.Should().BeTrue();
        config.CulturalContext.Should().Contain("Vesak");
    }
    
    [Fact]
    public void SLAReportingConfiguration_GetComplianceLevel_ShouldReturnCorrectLevel()
    {
        // Arrange
        var config = SLAReportingConfiguration.Create("Compliance Test", ReportFormat.XML, true, 99.95, "Regular Operations");
        
        // Act
        var complianceLevel = config.GetComplianceLevel();
        
        // Assert
        complianceLevel.Should().NotBeNull();
        complianceLevel.Should().BeOneOf("Enterprise", "Fortune500", "UltraHighAvailability");
    }
    
    [Fact]
    public void SLAReportingConfiguration_GenerateReportMetadata_ShouldIncludeAllRequiredFields()
    {
        // Arrange
        var config = SLAReportingConfiguration.Create("Metadata Test", ReportFormat.JSON, true, 99.8, "Diaspora Community");
        
        // Act
        var metadata = config.GenerateReportMetadata();
        
        // Assert
        metadata.Should().NotBeNull();
        metadata.Should().ContainKey("SLAThreshold");
        metadata.Should().ContainKey("ComplianceLevel");
        metadata.Should().ContainKey("CulturalContext");
        metadata.Should().ContainKey("IsEnterpriseGrade");
        metadata.Should().ContainKey("ReportGeneratedAt");
    }
}