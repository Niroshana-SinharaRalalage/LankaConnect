using FluentAssertions;
using LankaConnect.Domain.Business;

namespace LankaConnect.Domain.Tests.Business;

/// <summary>
/// TDD RED Phase: Test for RevenueProtectionStrategy enum
/// These tests should FAIL until we implement the RevenueProtectionStrategy enum
/// </summary>
public class RevenueProtectionStrategyTests
{
    [Fact]
    public void RevenueProtectionStrategy_ShouldHaveEmergencyStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.Emergency;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.Emergency);
        ((int)strategy).Should().Be(0); // First enum value
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldHaveGracefulDegradationStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.GracefulDegradation;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.GracefulDegradation);
        strategy.ToString().Should().Be("GracefulDegradation");
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldHaveLoadSheddingStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.LoadShedding;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.LoadShedding);
        strategy.Should().NotBe(RevenueProtectionStrategy.Emergency);
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldHaveCulturalPriorityStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.CulturalPriority;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.CulturalPriority);
        strategy.ToString().Should().Be("CulturalPriority");
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldHaveMaintenanceWindowStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.MaintenanceWindow;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.MaintenanceWindow);
        strategy.Should().NotBe(RevenueProtectionStrategy.CulturalPriority);
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldHaveDisasterRecoveryStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.DisasterRecovery;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.DisasterRecovery);
        strategy.ToString().Should().Be("DisasterRecovery");
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldHavePerformanceFirstStrategy()
    {
        // Arrange & Act
        var strategy = RevenueProtectionStrategy.PerformanceFirst;
        
        // Assert
        strategy.Should().Be(RevenueProtectionStrategy.PerformanceFirst);
        strategy.Should().NotBe(RevenueProtectionStrategy.DisasterRecovery);
    }
    
    [Theory]
    [InlineData(RevenueProtectionStrategy.Emergency, "Emergency revenue protection for critical situations")]
    [InlineData(RevenueProtectionStrategy.CulturalPriority, "Cultural event prioritization for diaspora communities")]
    [InlineData(RevenueProtectionStrategy.DisasterRecovery, "Revenue continuity during failover scenarios")]
    public void RevenueProtectionStrategy_GetDescription_ShouldReturnBusinessContext(
        RevenueProtectionStrategy strategy, string expectedContext)
    {
        // Act
        var description = strategy.GetDescription();
        
        // Assert
        description.Should().Contain(expectedContext);
    }
    
    [Fact]
    public void RevenueProtectionStrategy_GetPriority_ShouldReturnCorrectBusinessPriority()
    {
        // Arrange & Act
        var emergencyPriority = RevenueProtectionStrategy.Emergency.GetPriority();
        var culturalPriority = RevenueProtectionStrategy.CulturalPriority.GetPriority();
        var maintenancePriority = RevenueProtectionStrategy.MaintenanceWindow.GetPriority();
        
        // Assert
        emergencyPriority.Should().BeGreaterThan(culturalPriority);
        culturalPriority.Should().BeGreaterThan(maintenancePriority);
        emergencyPriority.Should().Be(100); // Highest priority
    }
    
    [Fact]
    public void RevenueProtectionStrategy_IsApplicableForCulturalEvents_ShouldIdentifyCorrectStrategies()
    {
        // Arrange
        var culturalStrategy = RevenueProtectionStrategy.CulturalPriority;
        var emergencyStrategy = RevenueProtectionStrategy.Emergency;
        var maintenanceStrategy = RevenueProtectionStrategy.MaintenanceWindow;
        
        // Act
        var culturalApplicable = culturalStrategy.IsApplicableForCulturalEvents();
        var emergencyApplicable = emergencyStrategy.IsApplicableForCulturalEvents();
        var maintenanceApplicable = maintenanceStrategy.IsApplicableForCulturalEvents();
        
        // Assert
        culturalApplicable.Should().BeTrue();
        emergencyApplicable.Should().BeTrue(); // Emergency applies to all scenarios
        maintenanceApplicable.Should().BeFalse();
    }
    
    [Fact]
    public void RevenueProtectionStrategy_GetRevenueImpactLevel_ShouldReturnCorrectImpactAssessment()
    {
        // Arrange
        var loadShedding = RevenueProtectionStrategy.LoadShedding;
        var gracefulDegradation = RevenueProtectionStrategy.GracefulDegradation;
        var performanceFirst = RevenueProtectionStrategy.PerformanceFirst;
        
        // Act
        var loadSheddingImpact = loadShedding.GetRevenueImpactLevel();
        var gracefulImpact = gracefulDegradation.GetRevenueImpactLevel();
        var performanceImpact = performanceFirst.GetRevenueImpactLevel();
        
        // Assert
        loadSheddingImpact.Should().Be("High"); // Load shedding has high revenue impact
        gracefulImpact.Should().Be("Medium");   // Graceful degradation has medium impact
        performanceImpact.Should().Be("Low");   // Performance first has low impact
    }
    
    [Fact]
    public void RevenueProtectionStrategy_ShouldSupportAllBusinessScenarios()
    {
        // Arrange
        var allStrategies = Enum.GetValues<RevenueProtectionStrategy>();
        
        // Act & Assert
        allStrategies.Should().HaveCount(7);
        allStrategies.Should().Contain(RevenueProtectionStrategy.Emergency);
        allStrategies.Should().Contain(RevenueProtectionStrategy.GracefulDegradation);
        allStrategies.Should().Contain(RevenueProtectionStrategy.LoadShedding);
        allStrategies.Should().Contain(RevenueProtectionStrategy.CulturalPriority);
        allStrategies.Should().Contain(RevenueProtectionStrategy.MaintenanceWindow);
        allStrategies.Should().Contain(RevenueProtectionStrategy.DisasterRecovery);
        allStrategies.Should().Contain(RevenueProtectionStrategy.PerformanceFirst);
    }
}