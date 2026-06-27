using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

/// <summary>
/// Phase 7F-E.7 (architect-approved 2026-05-04): re-opens Phase 7F-C §2.2 #4
/// deferred decision. <see cref="TierCount"/> gains 4 optional fields
/// — <c>AdultMaleCount</c>, <c>AdultFemaleCount</c>, <c>ChildMaleCount</c>,
/// <c>ChildFemaleCount</c> — so a B4-mode + tiered registration can store
/// per-tier 4-leaf demographics. Operator browser-test of 7F-E.6 close-out
/// (commit f665a2b6) showed the per-tier <c>N/A</c> rendering is confusing
/// because the 7F-E.4b form captures per-tier 4-leaf but submit aggregation
/// throws it away — capture-without-storage is data loss. This slice closes
/// the storage gap.
///
/// Architect-mandated invariants:
///   - All-or-nothing per tier (any of 4 set → all 4 must be set; rejects
///     half-set just like the existing <c>AdultCount</c>/<c>ChildCount</c>
///     pair from 7F-C).
///   - Sum equals <c>Count</c> when set.
///   - Cross-axis with 7F-C age split: when both 4-leaf AND age-split set,
///     <c>AdultCount = AdultMaleCount + AdultFemaleCount</c> and
///     <c>ChildCount = ChildMaleCount + ChildFemaleCount</c> (rejected on
///     mismatch).
///   - When 4-leaf set but age-split not, age-split is auto-derived so the
///     7F-C pricing helper keeps working unchanged (back-compat).
/// </summary>
public class Phase7FE7TierCount4LeafTests
{
    private static readonly Guid TierId = Guid.NewGuid();
    private const string TierName = "VIP";

    // ─────────────────────────────────────────────────────────────────────
    //  Happy path — all 4 leaves set, sum equals Count
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithAllFourLeaves_SumEqualsCount_Succeeds()
    {
        var result = TierCount.Create(
            TierId, TierName, count: 4,
            adultMaleCount: 1, adultFemaleCount: 1,
            childMaleCount: 1, childFemaleCount: 1);

        result.IsSuccess.Should().BeTrue();
        var tc = result.Value;
        tc.AdultMaleCount.Should().Be(1);
        tc.AdultFemaleCount.Should().Be(1);
        tc.ChildMaleCount.Should().Be(1);
        tc.ChildFemaleCount.Should().Be(1);
        tc.HasFourLeafSplit.Should().BeTrue();
    }

    [Fact]
    public void Create_WithFourLeavesSet_AutoDerivesAgeSplit_ForBackCompat()
    {
        // When 4-leaf is set but age-split params are NOT passed, the factory
        // auto-derives AdultCount = AM + AF and ChildCount = CM + CF so the
        // existing 7F-C pricing helper keeps working without changes.
        var result = TierCount.Create(
            TierId, TierName, count: 4,
            adultMaleCount: 2, adultFemaleCount: 1,
            childMaleCount: 1, childFemaleCount: 0);

        result.IsSuccess.Should().BeTrue();
        var tc = result.Value;
        tc.AdultCount.Should().Be(3, "auto-derived from AM (2) + AF (1)");
        tc.ChildCount.Should().Be(1, "auto-derived from CM (1) + CF (0)");
        tc.HasAgeSplit.Should().BeTrue("4-leaf implies age split for back-compat");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  All-or-nothing invariant
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, null, null, null)]   // only AM set
    [InlineData(null, 1, null, null)]   // only AF
    [InlineData(null, null, 1, null)]   // only CM
    [InlineData(null, null, null, 1)]   // only CF
    [InlineData(1, 1, null, null)]      // 2 of 4
    [InlineData(1, 1, 1, null)]         // 3 of 4 (one missing)
    public void Create_PartialFourLeaf_FailsAllOrNothing(
        int? am, int? af, int? cm, int? cf)
    {
        var result = TierCount.Create(
            TierId, TierName, count: 2,
            adultMaleCount: am, adultFemaleCount: af,
            childMaleCount: cm, childFemaleCount: cf);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("4-leaf",
            "the error message must reference the 4-leaf invariant for clarity");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Sum-equals-Count invariant
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithFourLeavesSumNotEqualCount_Fails()
    {
        var result = TierCount.Create(
            TierId, TierName, count: 4,
            adultMaleCount: 1, adultFemaleCount: 1,
            childMaleCount: 1, childFemaleCount: 0);   // sum 3 != 4

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("equal Count");
    }

    [Fact]
    public void Create_WithNegativeLeaf_Fails()
    {
        var result = TierCount.Create(
            TierId, TierName, count: 2,
            adultMaleCount: -1, adultFemaleCount: 1,
            childMaleCount: 1, childFemaleCount: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("non-negative");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Cross-axis with 7F-C age split
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithFourLeafAndAgeSplit_Agreeing_Succeeds()
    {
        // Caller can pass both age-split AND 4-leaf if they agree.
        var result = TierCount.Create(
            TierId, TierName, count: 4,
            adultCount: 2, childCount: 2,
            adultMaleCount: 1, adultFemaleCount: 1,
            childMaleCount: 1, childFemaleCount: 1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithFourLeafAndAgeSplit_Disagreeing_Fails()
    {
        // Caller passed both, but the 4-leaf age sum (am + af = 3) doesn't match
        // the age-split AdultCount (2). Reject — ambiguous source of truth.
        var result = TierCount.Create(
            TierId, TierName, count: 4,
            adultCount: 2, childCount: 2,
            adultMaleCount: 2, adultFemaleCount: 1,
            childMaleCount: 1, childFemaleCount: 0);  // sum=4 OK but adults=3 != adultCount=2

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("4-leaf",
            "error must explain the cross-axis age mismatch clearly");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Back-compat: legacy paths unchanged
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_NoFourLeaf_NoAgeSplit_LegacySucceeds()
    {
        var result = TierCount.Create(TierId, TierName, count: 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAgeSplit.Should().BeFalse();
        result.Value.HasFourLeafSplit.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAgeSplit_NoFourLeaf_StillWorks_Phase7FCBackCompat()
    {
        var result = TierCount.Create(
            TierId, TierName, count: 5,
            adultCount: 3, childCount: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAgeSplit.Should().BeTrue();
        result.Value.HasFourLeafSplit.Should().BeFalse(
            "age-split alone doesn't mean 4-leaf is captured");
    }
}
