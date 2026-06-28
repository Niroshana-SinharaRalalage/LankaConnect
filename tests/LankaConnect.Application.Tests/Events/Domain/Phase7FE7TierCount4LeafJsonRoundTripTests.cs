using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7F-E.7 round-trip regression: verifies that the new per-tier 4-leaf fields
/// (AdultMaleCount/AdultFemaleCount/ChildMaleCount/ChildFemaleCount) survive
/// serialisation through the same JSON config the EF ValueConverter uses
/// (camelCase + IgnoreNullsOnSerialize). Mirrors the 7F-C round-trip pattern in
/// Phase7FCTierAgeMatrixPricingTests.HeadCountBreakdown_JsonRoundTrip_PreservesAgeSplit.
///
/// Per memory 6A.129/6A.130: when the EF Core ValueConverter is JSON-roundtrip-based,
/// new fields on a value object are picked up automatically — but a regression test
/// is the only way to confirm the JsonConstructor signature actually deserialises the
/// new fields and that GetEqualityComponents includes them so structural equality
/// detects changes.
/// </summary>
public class Phase7FE7TierCount4LeafJsonRoundTripTests
{
    private static readonly System.Text.Json.JsonSerializerOptions ProductionOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void HeadCountBreakdown_JsonRoundTrip_Preserves4LeafSplit()
    {
        var vipId = Guid.NewGuid();
        var standardId = Guid.NewGuid();

        var original = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 2, childMales: 2, childFemales: 2,
            new[]
            {
                TierCount.Create(vipId, "VIP", count: 4,
                    adultMaleCount: 1, adultFemaleCount: 1,
                    childMaleCount: 1, childFemaleCount: 1).Value,
                TierCount.Create(standardId, "Standard", count: 4,
                    adultMaleCount: 1, adultFemaleCount: 1,
                    childMaleCount: 1, childFemaleCount: 1).Value,
            }).Value;

        var json = System.Text.Json.JsonSerializer.Serialize(original, ProductionOptions);

        json.Should().Contain("\"adultMaleCount\":1",
            "the 4-leaf must serialise into JSONB; legacy rows are unaffected because " +
            "the field is nullable on the JsonConstructor");
        json.Should().Contain("\"childFemaleCount\":1");

        var rehydrated = System.Text.Json.JsonSerializer.Deserialize<HeadCountBreakdown>(json, ProductionOptions);

        rehydrated.Should().NotBeNull();
        rehydrated!.TierCounts.Should().HaveCount(2);

        var vip = rehydrated.TierCounts!.Single(t => t.TierId == vipId);
        vip.HasFourLeafSplit.Should().BeTrue();
        vip.AdultMaleCount.Should().Be(1);
        vip.AdultFemaleCount.Should().Be(1);
        vip.ChildMaleCount.Should().Be(1);
        vip.ChildFemaleCount.Should().Be(1);
        vip.HasAgeSplit.Should().BeTrue("4-leaf auto-derives age split for back-compat");
        vip.AdultCount.Should().Be(2);
        vip.ChildCount.Should().Be(2);
    }

    [Fact]
    public void TierCount_LegacyJsonWithout4Leaf_DeserialisesCleanly_BackCompat()
    {
        // A legacy registration row in JSONB has no 4-leaf fields. Deserialisation must
        // produce a TierCount with HasFourLeafSplit=false; the EF ValueComparer must
        // still treat it as structurally equal to itself across snapshots.
        const string legacyJson = @"{""tierId"":""00000000-0000-0000-0000-000000000001"",""tierName"":""VIP"",""count"":3,""adultCount"":2,""childCount"":1}";

        var rehydrated = System.Text.Json.JsonSerializer.Deserialize<TierCount>(legacyJson, ProductionOptions);

        rehydrated.Should().NotBeNull();
        rehydrated!.HasAgeSplit.Should().BeTrue();
        rehydrated.HasFourLeafSplit.Should().BeFalse(
            "legacy rows have no 4-leaf — must not trip into the new render path");
        rehydrated.AdultMaleCount.Should().BeNull();
    }
}
