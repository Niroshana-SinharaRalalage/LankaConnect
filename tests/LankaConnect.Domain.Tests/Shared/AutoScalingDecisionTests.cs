using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Common.Enums;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Domain.Tests.Shared;

public class AutoScalingDecisionTests
{
    [Fact]
    public void Create_WithValidScalingParameters_ShouldCreateAutoScalingDecision()
    {
        // Arrange
        var scalingAction = ScalingAction.ScaleUp;
        var targetCapacity = 150;
        var culturalContext = "Vesak celebration preparation";
        var confidenceScore = 0.95m;

        // Act
        var result = AutoScalingDecision.Create(scalingAction, targetCapacity, culturalContext, confidenceScore);

        // Assert
        result.Should().NotBeNull();
        result.ScalingAction.Should().Be(scalingAction);
        result.TargetCapacity.Should().Be(targetCapacity);
        result.CulturalContext.Should().Be(culturalContext);
        result.ConfidenceScore.Should().Be(confidenceScore);
    }

    [Fact]
    public void Create_WithCulturalEventScaling_ShouldIncludeCulturalIntelligence()
    {
        // Arrange - Vesak celebration requires 5x scaling
        var scalingAction = ScalingAction.ScaleUp;
        var targetCapacity = 500; // 5x normal capacity for Vesak
        var culturalContext = "Vesak Full Moon Day - Sacred Buddhist celebration";

        // Act
        var decision = AutoScalingDecision.Create(scalingAction, targetCapacity, culturalContext, 0.98m);

        // Assert
        decision.IsCulturalEvent.Should().BeTrue();
        decision.CulturalEventType.Should().Be(CulturalEventType.Vesak);
        decision.SacredPriorityLevel.Should().Be(SacredPriorityLevel.Level10Sacred);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10001)] // Max capacity exceeded
    public void Create_WithInvalidTargetCapacity_ShouldThrowArgumentException(int invalidCapacity)
    {
        // Act & Assert
        var action = () => AutoScalingDecision.Create(ScalingAction.ScaleUp, invalidCapacity, "test", 0.5m);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_WithInvalidConfidenceScore_ShouldThrowArgumentException(decimal invalidScore)
    {
        // Act & Assert
        var action = () => AutoScalingDecision.Create(ScalingAction.ScaleUp, 100, "test", invalidScore);
        action.Should().Throw<ArgumentException>();
    }
}

