using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Domain.Tests.Common.Enums;

/// <summary>
/// TDD tests for CulturalEventType enum consolidation and canonical usage
/// Zero Tolerance approach for 57 compilation error elimination
/// </summary>
public class CulturalEventTypeConsolidationTests
{
    [Fact]
    public void CanonicalCulturalEventType_ShouldHaveCorrectValues()
    {
        // RED Phase: Test canonical enum values for cultural authenticity

        // Buddhist events
        ((int)CulturalEventType.VesakDayBuddhist).Should().Be(1);

        // Hindu events
        ((int)CulturalEventType.DiwaliHindu).Should().Be(2);

        // Islamic events
        ((int)CulturalEventType.EidAlFitrIslamic).Should().Be(3);
    }

    [Fact]
    public void AutoScalingDecision_WithVesakContext_ShouldUseCanonicalEnum()
    {
        // RED Phase: This will fail because MissingTypeStubs uses wrong enum values
        var decision = AutoScalingDecision.Create(ScalingAction.ScaleUp, 1000, "Vesak celebration preparation", 0.95m);

        // Assert canonical enum value
        decision.CulturalEventType.Should().Be(CulturalEventType.VesakDayBuddhist);
        decision.IsCulturalEvent.Should().BeTrue();
    }

    [Fact]
    public void AutoScalingDecision_WithDiwaliContext_ShouldUseCanonicalEnum()
    {
        // RED Phase: Will fail due to enum mismatch
        var decision = AutoScalingDecision.Create(ScalingAction.ScaleUp, 800, "Diwali festival of lights", 0.90m);

        decision.CulturalEventType.Should().Be(CulturalEventType.DiwaliHindu);
        decision.SacredPriorityLevel.Should().Be(SacredPriorityLevel.High);
    }

    [Fact]
    public void AutoScalingDecision_WithEidContext_ShouldUseCanonicalEnum()
    {
        // RED Phase: Will fail due to enum mismatch
        var decision = AutoScalingDecision.Create(ScalingAction.ScaleUp, 750, "Eid al-Fitr celebration", 0.88m);

        decision.CulturalEventType.Should().Be(CulturalEventType.EidAlFitrIslamic);
        decision.SacredPriorityLevel.Should().Be(SacredPriorityLevel.High);
    }

    [Theory]
    [InlineData("Vesak", CulturalEventType.VesakDayBuddhist)]
    [InlineData("Diwali", CulturalEventType.DiwaliHindu)]
    [InlineData("Eid", CulturalEventType.EidAlFitrIslamic)]
    public void CulturalEventDetection_ShouldMapToCanonicalEnum(string culturalContext, CulturalEventType expectedEnum)
    {
        // RED Phase: Test cultural context to canonical enum mapping
        var decision = AutoScalingDecision.Create(ScalingAction.ScaleUp, 500, culturalContext, 0.85m);

        decision.CulturalEventType.Should().Be(expectedEnum);
    }

    [Fact]
    public void AllCanonicalEnumValues_ShouldBeDefined()
    {
        // RED Phase: Validate all canonical values exist and are accessible
        var canonicalValues = new[]
        {
            CulturalEventType.VesakDayBuddhist,    // = 1 (Buddhist)
            CulturalEventType.DiwaliHindu,         // = 2 (Hindu)
            CulturalEventType.EidAlFitrIslamic,    // = 3 (Islamic)
            CulturalEventType.EidAlAdhaIslamic,    // = 4 (Islamic)
            CulturalEventType.GuruNanakJayanti,    // = 5 (Sikh)
            CulturalEventType.ThaipusamTamil       // = 6 (Tamil Hindu)
        };

        foreach (var value in canonicalValues)
        {
            Enum.IsDefined(typeof(CulturalEventType), value).Should().BeTrue($"Canonical enum value {value} should be defined");
        }
    }

    [Fact]
    public void CulturalEventType_ShouldHaveValidTrafficMultipliers()
    {
        // RED Phase: Test that enum values align with cultural significance

        // Most sacred Buddhist event should have highest priority
        var vesakValue = (int)CulturalEventType.VesakDayBuddhist;
        vesakValue.Should().Be(1, "Vesak should be highest priority Buddhist event");

        // Major Hindu festival should have high priority
        var diwaliValue = (int)CulturalEventType.DiwaliHindu;
        diwaliValue.Should().Be(2, "Diwali should be major Hindu festival priority");

        // Sacred Islamic celebration should have high priority
        var eidValue = (int)CulturalEventType.EidAlFitrIslamic;
        eidValue.Should().Be(3, "Eid al-Fitr should be major Islamic celebration priority");
    }
}