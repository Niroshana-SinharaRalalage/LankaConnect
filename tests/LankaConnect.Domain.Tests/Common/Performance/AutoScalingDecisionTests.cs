using FluentAssertions;
using LankaConnect.Domain.Common.Performance;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.Performance;

/// <summary>
/// TDD RED PHASE: Tests for AutoScalingDecision (26 references - Tier 1 Priority)
/// These tests establish the contract for cultural intelligence-aware auto-scaling
/// </summary>
public class AutoScalingDecisionTests
{
    [Test]
    public void AutoScalingDecision_ShouldHaveRequiredProperties()
    {
        // Arrange
        var culturalFactors = new List<CulturalFactor>
        {
            new CulturalFactor("Vesak", 0.8),
            new CulturalFactor("AvuruduSeason", 0.9)
        };

        // Act
        var decision = new AutoScalingDecision(
            ScaleDirection.Up,
            recommendedCapacity: 150,
            culturalFactors: culturalFactors,
            estimatedDuration: TimeSpan.FromHours(4),
            confidenceScore: 0.95
        );

        // Assert
        decision.Direction.Should().Be(ScaleDirection.Up);
        decision.RecommendedCapacity.Should().Be(150);
        decision.CulturalFactors.Should().HaveCount(2);
        decision.EstimatedDuration.Should().Be(TimeSpan.FromHours(4));
        decision.ConfidenceScore.Should().Be(0.95);
    }

    [Test]
    public void AutoScalingDecision_ShouldValidateCulturalFactors()
    {
        // Arrange
        var culturalFactors = new List<CulturalFactor>
        {
            new CulturalFactor("Poson", 0.7),
            new CulturalFactor("Esala", 0.85)
        };

        // Act
        var decision = new AutoScalingDecision(
            ScaleDirection.Up,
            100,
            culturalFactors,
            TimeSpan.FromHours(2),
            0.88
        );

        // Assert
        decision.CulturalFactors.Should().Contain(f => f.Name == "Poson");
        decision.CulturalFactors.Should().Contain(f => f.Name == "Esala");
        decision.CulturalFactors.All(f => f.Impact >= 0.0 && f.Impact <= 1.0).Should().BeTrue();
    }

    [Test]
    public void AutoScalingDecision_ShouldHandleDownScaling()
    {
        // Arrange & Act
        var decision = new AutoScalingDecision(
            ScaleDirection.Down,
            50,
            new List<CulturalFactor>(),
            TimeSpan.FromMinutes(30),
            0.75
        );

        // Assert
        decision.Direction.Should().Be(ScaleDirection.Down);
        decision.RecommendedCapacity.Should().Be(50);
        decision.CulturalFactors.Should().BeEmpty();
    }

    [Test]
    public void AutoScalingDecision_ShouldRequireValidConfidenceScore()
    {
        // Arrange & Act
        var createWithInvalidScore = () => new AutoScalingDecision(
            ScaleDirection.Maintain,
            100,
            new List<CulturalFactor>(),
            TimeSpan.FromHours(1),
            1.5 // Invalid score > 1.0
        );

        // Assert
        createWithInvalidScore.Should().Throw<ArgumentException>()
            .WithMessage("Confidence score must be between 0.0 and 1.0");
    }

    [Test]
    public void AutoScalingDecision_ShouldRequirePositiveCapacity()
    {
        // Arrange & Act
        var createWithNegativeCapacity = () => new AutoScalingDecision(
            ScaleDirection.Up,
            -10, // Invalid negative capacity
            new List<CulturalFactor>(),
            TimeSpan.FromHours(1),
            0.8
        );

        // Assert
        createWithNegativeCapacity.Should().Throw<ArgumentException>()
            .WithMessage("Recommended capacity must be positive");
    }

    [Test]
    public void AutoScalingDecision_ShouldCalculateOverallCulturalImpact()
    {
        // Arrange
        var culturalFactors = new List<CulturalFactor>
        {
            new CulturalFactor("Vesak", 0.9),
            new CulturalFactor("Poson", 0.7),
            new CulturalFactor("AvuruduSeason", 0.95)
        };

        var decision = new AutoScalingDecision(
            ScaleDirection.Up,
            200,
            culturalFactors,
            TimeSpan.FromHours(6),
            0.92
        );

        // Act
        var overallImpact = decision.CalculateOverallCulturalImpact();

        // Assert
        overallImpact.Should().BeGreaterThan(0.0);
        overallImpact.Should().BeLessThanOrEqualTo(1.0);
        // Should be weighted average: (0.9 + 0.7 + 0.95) / 3 = 0.85
        overallImpact.Should().BeApproximately(0.85, 0.01);
    }

    [Test]
    public void AutoScalingDecision_ShouldSupportSriLankanCulturalEvents()
    {
        // Arrange
        var sriLankanEvents = new List<CulturalFactor>
        {
            new CulturalFactor("Vesak", 0.95),        // Buddha's birth, enlightenment, death
            new CulturalFactor("Poson", 0.9),         // Introduction of Buddhism to Sri Lanka
            new CulturalFactor("Esala", 0.85),        // Tooth Relic procession
            new CulturalFactor("Avurudu", 0.98),      // Sinhala and Tamil New Year
            new CulturalFactor("Deepavali", 0.8),     // Festival of Lights
            new CulturalFactor("Eid", 0.75)           // Islamic celebration
        };

        // Act
        var decision = new AutoScalingDecision(
            ScaleDirection.Up,
            300,
            sriLankanEvents,
            TimeSpan.FromHours(8),
            0.94
        );

        // Assert
        decision.CulturalFactors.Should().HaveCount(6);
        decision.GetHighestImpactCulturalEvent().Should().Be("Avurudu");
        decision.RequiresEmergencyScaling().Should().BeTrue();
    }
}

/// <summary>
/// Supporting value object for cultural scaling factors
/// </summary>
public record CulturalFactor(string Name, double Impact)
{
    public static CulturalFactor CreateFromSriLankanEvent(string eventName, double impact) =>
        new(eventName, Math.Clamp(impact, 0.0, 1.0));
}

/// <summary>
/// Scaling direction enumeration
/// </summary>
public enum ScaleDirection
{
    Down,
    Maintain,
    Up
}