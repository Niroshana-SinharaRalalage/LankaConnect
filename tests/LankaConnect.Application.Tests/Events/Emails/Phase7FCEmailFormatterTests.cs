using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Emails;

/// <summary>
/// Phase 7F-C — email formatter tests covering the new per-tier-by-age tier line.
///
/// Architect edit #11: when <see cref="TierCount.HasAgeSplit"/> is false (legacy null-axis,
/// B1 / B3, or B2/B4 without age opt-in), output stays in the legacy <c>"VIP × N"</c> format.
/// When the age axis IS set, output becomes <c>"VIP: 2 adults · 1 child"</c> with singular/plural
/// handling per leaf and zero-leaves suppressed (so "all adults" reads "VIP: 3 adults",
/// not "VIP: 3 adults · 0 children").
/// </summary>
public class Phase7FCEmailFormatterTests
{
    [Fact]
    public void FormatTierLine_Empty_ReturnsEmpty()
    {
        HeadCountEmailFormatter.FormatTierLine(null).Should().Be(string.Empty);
        HeadCountEmailFormatter.FormatTierLine(new List<TierCount>()).Should().Be(string.Empty);
    }

    [Fact]
    public void FormatTierLine_LegacyNullAxis_UsesXTimesNFormat()
    {
        // No age split → legacy format
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3).Value;
        var general = TierCount.Create(Guid.NewGuid(), "General", count: 5).Value;

        var line = HeadCountEmailFormatter.FormatTierLine(new[] { vip, general });

        line.Should().Be("VIP × 3, General × 5");
    }

    [Fact]
    public void FormatTierLine_AgeSplit_ShowsAdultsAndChildren()
    {
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 1).Value;

        var line = HeadCountEmailFormatter.FormatTierLine(new[] { vip });

        line.Should().Be("VIP: 2 adults · 1 child");
    }

    [Fact]
    public void FormatTierLine_AgeSplit_AllAdults_OmitsChildrenLeaf()
    {
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 3, childCount: 0).Value;

        var line = HeadCountEmailFormatter.FormatTierLine(new[] { vip });

        line.Should().Be("VIP: 3 adults", because: "zero-children leaf is suppressed");
    }

    [Fact]
    public void FormatTierLine_AgeSplit_AllChildren_OmitsAdultsLeaf()
    {
        var family = TierCount.Create(Guid.NewGuid(), "Family", count: 4, adultCount: 0, childCount: 4).Value;

        var line = HeadCountEmailFormatter.FormatTierLine(new[] { family });

        line.Should().Be("Family: 4 children", because: "zero-adults leaf is suppressed");
    }

    [Fact]
    public void FormatTierLine_AgeSplit_SingularWords()
    {
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 2, adultCount: 1, childCount: 1).Value;

        var line = HeadCountEmailFormatter.FormatTierLine(new[] { vip });

        line.Should().Be("VIP: 1 adult · 1 child", because: "singular adult/child");
    }

    [Fact]
    public void FormatTierLine_AgeSplit_MultiTier_JoinsWithComma()
    {
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 1).Value;
        var general = TierCount.Create(Guid.NewGuid(), "General", count: 5, adultCount: 5, childCount: 0).Value;

        var line = HeadCountEmailFormatter.FormatTierLine(new[] { vip, general });

        line.Should().Be("VIP: 2 adults · 1 child, General: 5 adults");
    }
}
