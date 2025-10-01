using FluentAssertions;
using LankaConnect.Domain.Common.DisasterRecovery;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Business;

namespace LankaConnect.Domain.Tests.Common.DisasterRecovery;

/// <summary>
/// TDD RED Phase: Test for DisasterRecoveryContext domain type
/// These tests validate the DisasterRecoveryContext class with cultural intelligence features
/// </summary>
public class DisasterRecoveryContextTests
{
    [Fact]
    public void DisasterRecoveryContext_Creation_ShouldInitializeWithRequiredProperties()
    {
        // Arrange
        var scenarioId = "DR_VESAK_2025";
        var primaryRegion = "US_EAST";
        var failoverRegion = "US_WEST";
        var triggerTime = DateTime.UtcNow;
        
        // Act
        var context = DisasterRecoveryContext.Create(scenarioId, primaryRegion, failoverRegion, triggerTime);
        
        // Assert
        context.Should().NotBeNull();
        context.ScenarioId.Should().Be(scenarioId);
        context.PrimaryRegion.Should().Be(primaryRegion);
        context.FailoverRegion.Should().Be(failoverRegion);
        context.TriggerTime.Should().Be(triggerTime);
        context.ContextId.Should().NotBeEmpty();
    }
    
    [Fact]
    public void DisasterRecoveryContext_WithCulturalIntelligence_ShouldStoreCulturalContext()
    {
        // Arrange
        var context = DisasterRecoveryContext.Create("DR_CULTURAL_001", "ASIA_SOUTH", "US_WEST", DateTime.UtcNow);
        var culturalEvent = "Vesak_Day";
        var affectedCommunities = new[] { "Sri_Lankan_Diaspora", "Buddhist_Community" };
        var trafficMultiplier = 5.2;
        
        // Act
        context.SetCulturalIntelligenceContext(culturalEvent, affectedCommunities, trafficMultiplier);
        
        // Assert
        context.CulturalEventType.Should().Be(culturalEvent);
        context.AffectedCommunities.Should().BeEquivalentTo(affectedCommunities);
        context.ExpectedTrafficMultiplier.Should().Be(trafficMultiplier);
        context.HasCulturalIntelligence.Should().BeTrue();
    }
    
    [Fact]
    public void DisasterRecoveryContext_WithRevenueProtection_ShouldConfigureRevenueStrategy()
    {
        // Arrange
        var context = DisasterRecoveryContext.Create("DR_REVENUE_001", "EU_WEST", "US_EAST", DateTime.UtcNow);
        var strategy = RevenueProtectionStrategy.DisasterRecovery;
        var expectedRevenueLoss = 15000.00m;
        var maxAcceptableLoss = 50000.00m;
        
        // Act
        context.SetRevenueProtectionContext(strategy, expectedRevenueLoss, maxAcceptableLoss);
        
        // Assert
        context.RevenueProtectionStrategy.Should().Be(strategy);
        context.ExpectedRevenueLoss.Should().Be(expectedRevenueLoss);
        context.MaxAcceptableRevenueLoss.Should().Be(maxAcceptableLoss);
        context.RequiresRevenueProtection.Should().BeTrue();
    }
    
    [Fact]
    public void DisasterRecoveryContext_WithServiceLevelAgreements_ShouldMaintainSLAContext()
    {
        // Arrange
        var context = DisasterRecoveryContext.Create("DR_SLA_001", "US_CENTRAL", "US_WEST", DateTime.UtcNow);
        var goldSLA = ServiceLevelAgreement.Create("Gold", 
            new Dictionary<string, double> { { "Uptime", 99.9 }, { "ResponseTime", 200 } }, 
            TimeSpan.FromDays(30));
        var silverSLA = ServiceLevelAgreement.Create("Silver", 
            new Dictionary<string, double> { { "Uptime", 99.5 }, { "ResponseTime", 500 } }, 
            TimeSpan.FromDays(30));
        var slas = new[] { goldSLA, silverSLA };
        
        // Act
        context.SetServiceLevelAgreements(slas);
        
        // Assert
        context.ServiceLevelAgreements.Should().HaveCount(2);
        context.ServiceLevelAgreements.Should().Contain(goldSLA);
        context.ServiceLevelAgreements.Should().Contain(silverSLA);
        context.HasServiceLevelRequirements.Should().BeTrue();
    }
    
    [Fact]
    public void DisasterRecoveryContext_GetFailoverWindow_ShouldCalculateOptimalWindow()
    {
        // Arrange
        var context = DisasterRecoveryContext.Create("DR_WINDOW_001", "ASIA_EAST", "US_WEST", DateTime.UtcNow);
        context.SetCulturalIntelligenceContext("Ramadan", new[] { "Muslim_Community" }, 3.0);
        
        // Act
        var window = context.GetOptimalFailoverWindow();
        
        // Assert
        window.Should().NotBeNull();
        window.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        window.IsCulturallyOptimized.Should().BeTrue();
    }
    
    [Fact]
    public void DisasterRecoveryContext_CalculateRecoveryPriority_ShouldPrioritizeCulturalEvents()
    {
        // Arrange
        var culturalContext = DisasterRecoveryContext.Create("DR_CULTURAL", "EU_NORTH", "US_EAST", DateTime.UtcNow);
        culturalContext.SetCulturalIntelligenceContext("Diwali", new[] { "Hindu_Community" }, 4.5);
        
        var regularContext = DisasterRecoveryContext.Create("DR_REGULAR", "EU_NORTH", "US_EAST", DateTime.UtcNow);
        
        // Act
        var culturalPriority = culturalContext.CalculateRecoveryPriority();
        var regularPriority = regularContext.CalculateRecoveryPriority();
        
        // Assert
        culturalPriority.Should().BeGreaterThan(regularPriority);
        culturalPriority.Should().BeGreaterThan(80); // High priority for cultural events
        regularPriority.Should().BeLessThan(60);     // Normal priority for regular scenarios
    }
    
    [Theory]
    [InlineData("", "US_EAST", "US_WEST")]
    [InlineData(null, "US_EAST", "US_WEST")]
    [InlineData("DR_001", "", "US_WEST")]
    [InlineData("DR_001", "US_EAST", "")]
    public void DisasterRecoveryContext_WithInvalidParameters_ShouldThrowArgumentException(
        string scenarioId, string primaryRegion, string failoverRegion)
    {
        // Arrange
        var triggerTime = DateTime.UtcNow;
        
        // Act & Assert
        var act = () => DisasterRecoveryContext.Create(scenarioId, primaryRegion, failoverRegion, triggerTime);
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void DisasterRecoveryContext_WithFutureFailoverTime_ShouldScheduleProactiveFailover()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddHours(2);
        var context = DisasterRecoveryContext.Create("DR_SCHEDULED", "EU_WEST", "US_EAST", futureTime);
        
        // Act
        var isScheduled = context.IsScheduledFailover();
        var timeUntilFailover = context.GetTimeUntilFailover();
        
        // Assert
        isScheduled.Should().BeTrue();
        timeUntilFailover.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromMinutes(1));
    }
    
    [Fact]
    public void DisasterRecoveryContext_GenerateContextReport_ShouldProvideComprehensiveStatus()
    {
        // Arrange
        var context = DisasterRecoveryContext.Create("DR_REPORT_001", "ASIA_SOUTH", "US_WEST", DateTime.UtcNow);
        context.SetCulturalIntelligenceContext("Vesak_Day", new[] { "Buddhist_Community" }, 6.0);
        context.SetRevenueProtectionContext(RevenueProtectionStrategy.CulturalPriority, 25000m, 75000m);
        
        // Act
        var report = context.GenerateContextReport();
        
        // Assert
        report.Should().NotBeNull();
        report.Should().Contain("DR_REPORT_001");
        report.Should().Contain("Vesak_Day");
        report.Should().Contain("CulturalPriority");
        report.Should().Contain("ASIA_SOUTH");
        report.Should().Contain("US_WEST");
    }
}