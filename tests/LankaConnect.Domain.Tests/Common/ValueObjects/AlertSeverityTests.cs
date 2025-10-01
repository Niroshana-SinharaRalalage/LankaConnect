using FluentAssertions;
using LankaConnect.Domain.Common.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Common.ValueObjects;

/// <summary>
/// TDD RED PHASE: Comprehensive failing tests for consolidated AlertSeverity
/// Following London School TDD approach with mock-driven development
/// </summary>
public class AlertSeverityTests
{
    [Fact]
    public void AlertSeverity_ShouldHaveCorrectStaticInstances()
    {
        // RED: This will fail until we implement the consolidated AlertSeverity
        AlertSeverity.Low.Should().NotBeNull();
        AlertSeverity.Medium.Should().NotBeNull();
        AlertSeverity.High.Should().NotBeNull();
        AlertSeverity.Critical.Should().NotBeNull();
        AlertSeverity.Sacred.Should().NotBeNull();

        AlertSeverity.Low.Value.Should().Be(1);
        AlertSeverity.Medium.Value.Should().Be(2);
        AlertSeverity.High.Value.Should().Be(3);
        AlertSeverity.Critical.Value.Should().Be(4);
        AlertSeverity.Sacred.Value.Should().Be(5);
    }

    [Fact]
    public void AlertSeverity_ShouldHaveCorrectNames()
    {
        // RED: Testing cultural intelligence naming
        AlertSeverity.Low.Name.Should().Be("Low");
        AlertSeverity.Medium.Name.Should().Be("Medium");
        AlertSeverity.High.Name.Should().Be("High");
        AlertSeverity.Critical.Name.Should().Be("Critical");
        AlertSeverity.Sacred.Name.Should().Be("Sacred");
    }

    [Fact]
    public void AlertSeverity_ShouldImplementValueObjectEquality()
    {
        // RED: Testing ValueObject behavior
        var severity1 = AlertSeverity.High;
        var severity2 = AlertSeverity.High;
        var severity3 = AlertSeverity.Critical;

        severity1.Should().Be(severity2);
        severity1.Should().NotBe(severity3);
        severity1.Equals(severity2).Should().BeTrue();
        severity1.Equals(severity3).Should().BeFalse();
    }

    [Fact]
    public void AlertSeverity_ShouldSupportComparison()
    {
        // RED: Testing severity comparison for cultural event prioritization
        AlertSeverity.Low.CompareTo(AlertSeverity.Medium).Should().BeLessThan(0);
        AlertSeverity.Medium.CompareTo(AlertSeverity.High).Should().BeLessThan(0);
        AlertSeverity.High.CompareTo(AlertSeverity.Critical).Should().BeLessThan(0);
        AlertSeverity.Critical.CompareTo(AlertSeverity.Sacred).Should().BeLessThan(0);

        AlertSeverity.Sacred.CompareTo(AlertSeverity.Critical).Should().BeGreaterThan(0);
        AlertSeverity.Critical.CompareTo(AlertSeverity.Critical).Should().Be(0);
    }

    [Fact]
    public void AlertSeverity_ShouldSupportCulturalIntelligenceRequirements()
    {
        // RED: Testing cultural intelligence specific requirements
        AlertSeverity.Sacred.IsSacredEvent().Should().BeTrue();
        AlertSeverity.Critical.IsSacredEvent().Should().BeFalse();
        AlertSeverity.High.IsSacredEvent().Should().BeFalse();

        AlertSeverity.Sacred.RequiresImmediateAttention().Should().BeTrue();
        AlertSeverity.Critical.RequiresImmediateAttention().Should().BeTrue();
        AlertSeverity.High.RequiresImmediateAttention().Should().BeFalse();
    }

    [Fact]
    public void AlertSeverity_ShouldHaveCorrectHashCodes()
    {
        // RED: Testing hash code behavior for collections
        var severity1 = AlertSeverity.High;
        var severity2 = AlertSeverity.High;
        var severity3 = AlertSeverity.Critical;

        severity1.GetHashCode().Should().Be(severity2.GetHashCode());
        severity1.GetHashCode().Should().NotBe(severity3.GetHashCode());
    }

    [Fact]
    public void AlertSeverity_ShouldConvertToString()
    {
        // RED: Testing string representation
        AlertSeverity.Low.ToString().Should().Be("Low");
        AlertSeverity.Medium.ToString().Should().Be("Medium");
        AlertSeverity.High.ToString().Should().Be("High");
        AlertSeverity.Critical.ToString().Should().Be("Critical");
        AlertSeverity.Sacred.ToString().Should().Be("Sacred");
    }

    [Theory]
    [InlineData(1, "Low")]
    [InlineData(2, "Medium")]
    [InlineData(3, "High")]
    [InlineData(4, "Critical")]
    [InlineData(5, "Sacred")]
    public void AlertSeverity_ShouldCreateFromValue(int value, string expectedName)
    {
        // RED: Testing factory method creation
        var severity = AlertSeverity.FromValue(value);

        severity.Value.Should().Be(value);
        severity.Name.Should().Be(expectedName);
    }

    [Fact]
    public void AlertSeverity_ShouldThrowForInvalidValue()
    {
        // RED: Testing validation
        var action = () => AlertSeverity.FromValue(99);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid AlertSeverity value: 99");
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Critical")]
    [InlineData("Sacred")]
    public void AlertSeverity_ShouldCreateFromName(string name)
    {
        // RED: Testing factory method creation by name
        var severity = AlertSeverity.FromName(name);

        severity.Name.Should().Be(name);
    }

    [Fact]
    public void AlertSeverity_ShouldThrowForInvalidName()
    {
        // RED: Testing validation
        var action = () => AlertSeverity.FromName("Invalid");

        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid AlertSeverity name: Invalid");
    }

    [Fact]
    public void AlertSeverity_ShouldSupportImplicitConversionToInt()
    {
        // RED: Testing implicit conversion for backward compatibility
        int lowValue = AlertSeverity.Low;
        int sacredValue = AlertSeverity.Sacred;

        lowValue.Should().Be(1);
        sacredValue.Should().Be(5);
    }

    [Fact]
    public void AlertSeverity_ShouldSupportExplicitConversionFromInt()
    {
        // RED: Testing explicit conversion
        var severity = (AlertSeverity)3;

        severity.Should().Be(AlertSeverity.High);
    }

    [Fact]
    public void AlertSeverity_ShouldSupportDiasporaCommunityNotificationPriority()
    {
        // RED: Testing diaspora community specific requirements
        AlertSeverity.Sacred.GetNotificationPriority().Should().Be(NotificationPriority.Emergency);
        AlertSeverity.Critical.GetNotificationPriority().Should().Be(NotificationPriority.High);
        AlertSeverity.High.GetNotificationPriority().Should().Be(NotificationPriority.Medium);
        AlertSeverity.Medium.GetNotificationPriority().Should().Be(NotificationPriority.Low);
        AlertSeverity.Low.GetNotificationPriority().Should().Be(NotificationPriority.Info);
    }

    [Fact]
    public void AlertSeverity_ShouldProvideAllSeverityLevels()
    {
        // RED: Testing that we have all required severity levels
        var allSeverities = AlertSeverity.GetAll();

        allSeverities.Should().HaveCount(5);
        allSeverities.Should().Contain(AlertSeverity.Low);
        allSeverities.Should().Contain(AlertSeverity.Medium);
        allSeverities.Should().Contain(AlertSeverity.High);
        allSeverities.Should().Contain(AlertSeverity.Critical);
        allSeverities.Should().Contain(AlertSeverity.Sacred);
    }
}

/// <summary>
/// Supporting enum for testing notification priority mapping
/// This will be integrated with existing notification system
/// </summary>
public enum NotificationPriority
{
    Info = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Emergency = 5
}