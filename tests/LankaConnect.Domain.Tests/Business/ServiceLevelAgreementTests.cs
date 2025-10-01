using FluentAssertions;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Business;

/// <summary>
/// TDD RED Phase: Test for ServiceLevelAgreement domain entity
/// These tests should FAIL until we implement the ServiceLevelAgreement class
/// </summary>
public class ServiceLevelAgreementTests
{
    [Fact]
    public void ServiceLevelAgreement_Creation_ShouldInitializeWithValidProperties()
    {
        // Arrange
        var slaLevel = "Gold";
        var performanceTargets = new Dictionary<string, double>
        {
            { "Uptime", 99.9 },
            { "ResponseTime", 200.0 },
            { "Throughput", 1000.0 }
        };
        var compliancePeriod = TimeSpan.FromDays(30);
        
        // Act
        var sla = ServiceLevelAgreement.Create(slaLevel, performanceTargets, compliancePeriod);
        
        // Assert
        sla.Should().NotBeNull();
        sla.SlaLevel.Should().Be(slaLevel);
        sla.PerformanceTargets.Should().BeEquivalentTo(performanceTargets);
        sla.CompliancePeriod.Should().Be(compliancePeriod);
        sla.Id.Should().NotBeEmpty();
    }
    
    [Fact]
    public void ServiceLevelAgreement_WithCulturalContext_ShouldStoreCulturalRequirements()
    {
        // Arrange
        var slaLevel = "Cultural_Premium";
        var performanceTargets = new Dictionary<string, double>
        {
            { "CulturalEventResponseTime", 50.0 },
            { "SacredContentProtection", 100.0 },
            { "DiasporaLoadBalancing", 99.9 }
        };
        var compliancePeriod = TimeSpan.FromDays(90); // Quarterly for cultural events
        
        // Act
        var sla = ServiceLevelAgreement.Create(slaLevel, performanceTargets, compliancePeriod);
        sla.SetCulturalContext("Sri_Lankan_New_Year", "Global_Diaspora");
        
        // Assert
        sla.CulturalEventContext.Should().Be("Sri_Lankan_New_Year");
        sla.DiasporaRegion.Should().Be("Global_Diaspora");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ServiceLevelAgreement_WithInvalidSlaLevel_ShouldThrowException(string invalidSlaLevel)
    {
        // Arrange
        var performanceTargets = new Dictionary<string, double> { { "Uptime", 99.0 } };
        var compliancePeriod = TimeSpan.FromDays(30);
        
        // Act & Assert
        var act = () => ServiceLevelAgreement.Create(invalidSlaLevel, performanceTargets, compliancePeriod);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*SLA level*");
    }
    
    [Fact]
    public void ServiceLevelAgreement_WithEmptyPerformanceTargets_ShouldThrowException()
    {
        // Arrange
        var slaLevel = "Silver";
        var performanceTargets = new Dictionary<string, double>();
        var compliancePeriod = TimeSpan.FromDays(30);
        
        // Act & Assert
        var act = () => ServiceLevelAgreement.Create(slaLevel, performanceTargets, compliancePeriod);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*performance targets*");
    }
    
    [Fact]
    public void ServiceLevelAgreement_CheckCompliance_ShouldValidatePerformanceMetrics()
    {
        // Arrange
        var sla = ServiceLevelAgreement.Create("Gold", 
            new Dictionary<string, double> { { "Uptime", 99.9 } }, 
            TimeSpan.FromDays(30));
        var actualMetrics = new Dictionary<string, double> { { "Uptime", 99.95 } };
        
        // Act
        var complianceResult = sla.CheckCompliance(actualMetrics);
        
        // Assert
        complianceResult.Should().NotBeNull();
        complianceResult.IsCompliant.Should().BeTrue();
        complianceResult.CompliancePercentage.Should().BeGreaterThan(100.0);
    }
    
    [Fact]
    public void ServiceLevelAgreement_CheckCompliance_ShouldDetectViolations()
    {
        // Arrange
        var sla = ServiceLevelAgreement.Create("Gold", 
            new Dictionary<string, double> { { "Uptime", 99.9 } }, 
            TimeSpan.FromDays(30));
        var actualMetrics = new Dictionary<string, double> { { "Uptime", 98.5 } };
        
        // Act
        var complianceResult = sla.CheckCompliance(actualMetrics);
        
        // Assert
        complianceResult.Should().NotBeNull();
        complianceResult.IsCompliant.Should().BeFalse();
        complianceResult.Violations.Should().Contain(v => v.Contains("Uptime"));
    }
}