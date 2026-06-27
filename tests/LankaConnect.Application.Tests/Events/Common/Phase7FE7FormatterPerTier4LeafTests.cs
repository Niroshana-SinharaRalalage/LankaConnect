using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 7F-E.7 (architect-approved 2026-05-04, re-opens §2.2 #4): the formatter must
/// render per-tier captured 4-leaf demographics when <see cref="TierCount.HasFourLeafSplit"/>
/// is true on the tier. Architect ruling: the Totals row (added in 7F-E.6.A) becomes
/// redundant when all per-tier rows already carry captured demographics — skip it then,
/// keep it for legacy/partial coverage.
///
/// Operator UAT context: this is the slice that closes the 7F-E.6 → 6.A → now 6.B
/// bug-find loop. The capture/storage gap surfaced because the merged form let the user
/// enter per-tier 4-leaf but submit aggregation discarded it.
/// </summary>
public class Phase7FE7FormatterPerTier4LeafTests
{
    private static IReadOnlyList<TierCount> Tiers4Leaf(params (string name, int count, int am, int af, int cm, int cf)[] entries)
    {
        var result = new List<TierCount>();
        foreach (var (name, count, am, af, cm, cf) in entries)
        {
            result.Add(TierCount.Create(
                Guid.NewGuid(), name, count,
                adultMaleCount: am, adultFemaleCount: af,
                childMaleCount: cm, childFemaleCount: cf).Value);
        }
        return result;
    }

    private static IReadOnlyList<TierCount> TiersLegacy(params (string name, int count)[] entries)
    {
        var result = new List<TierCount>();
        foreach (var (name, count) in entries)
            result.Add(TierCount.Create(Guid.NewGuid(), name, count).Value);
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Per-tier 4-leaf set on ALL tiers → captured rows + no Totals
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void B4_MultiTier_AllTiers4LeafSet_PerTierRowsCaptured_TotalsSkipped()
    {
        // Operator's intended state after 7F-E.7 ships: form sends per-tier 4-leaf on
        // every tier; per-tier rows show captured 4-leaf; Totals row skipped (redundant).
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 2, childMales: 2, childFemales: 2,
            tierCounts: Tiers4Leaf(
                ("VIP",      4, 1, 1, 1, 1),
                ("Standard", 4, 1, 1, 1, 1))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAgeAndGender);

        bd.IsTiered.Should().BeTrue();
        bd.Rows.Should().HaveCount(2);
        // Per-tier rows now show captured demographics (operator's user-entered values).
        foreach (var row in bd.Rows)
        {
            row.Age.Captured.Should().BeTrue("4-leaf auto-derives age split — row shows captured age");
            row.Age.Left.Should().Be(2, "row's adult count = AM + AF (1 + 1)");
            row.Age.Right.Should().Be(2, "row's child count = CM + CF (1 + 1)");
            row.Gender.Captured.Should().BeTrue("4-leaf carries gender per tier");
            row.Gender.Left.Should().Be(2, "row's male count = AM + CM");
            row.Gender.Right.Should().Be(2, "row's female count = AF + CF");
        }
        bd.Totals.Should().BeNull(
            "all per-tier rows show captured demographics — Totals row would duplicate");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Legacy: no per-tier 4-leaf → N/A rows + Totals row populated (7F-E.6.A behaviour preserved)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void B4_MultiTier_NoPerTier4Leaf_LegacyPath_RowsNotCaptured_TotalsPopulated()
    {
        // Operator's existing registration on event 616e59f3 — pre-7F-E.7 storage shape.
        // Must remain back-compat: per-tier rows N/A, Totals row populated from registration-
        // level demographics. Regression guard for the 7F-E.6.A behaviour.
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 2, childMales: 2, childFemales: 2,
            tierCounts: TiersLegacy(("VIP", 4), ("Standard", 4))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAgeAndGender);

        bd.Rows.Should().HaveCount(2);
        foreach (var row in bd.Rows)
        {
            row.Age.Captured.Should().BeFalse("legacy multi-tier without 4-leaf or age split → N/A");
            row.Gender.Captured.Should().BeFalse("legacy multi-tier without 4-leaf → N/A");
        }
        bd.Totals.Should().NotBeNull(
            "legacy path keeps the Totals row to surface captured registration-level data");
        bd.Totals!.Age.Captured.Should().BeTrue();
        bd.Totals.Age.Left.Should().Be(4);
        bd.Totals.Age.Right.Should().Be(4);
        bd.Totals.Gender.Captured.Should().BeTrue();
        bd.Totals.Gender.Left.Should().Be(4);
        bd.Totals.Gender.Right.Should().Be(4);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Partial coverage NOT REACHABLE by design
    // ─────────────────────────────────────────────────────────────────────
    //
    // The Phase 7F-C all-or-nothing-across-basket invariant in
    // HeadCountBreakdown.ValidateTierCounts rejects mixed-coverage tier sets
    // (any tier with HasAgeSplit forces every tier to have it). Since the
    // 4-leaf auto-derives age split, partial 4-leaf coverage across tiers
    // is similarly rejected at the factory level — the formatter never
    // sees that state. Documented here so future refactors don't accidentally
    // relax this guard without updating the formatter's Totals-row gating.

    // ─────────────────────────────────────────────────────────────────────
    //  Single-tier with 4-leaf: per-tier already captures, no Totals duplicate
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void B4_SingleTier_4LeafSet_PerTierRowCaptured_TotalsSkipped()
    {
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 1, adultFemales: 1, childMales: 1, childFemales: 1,
            tierCounts: Tiers4Leaf(("VIP", 4, 1, 1, 1, 1))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAgeAndGender);

        bd.Rows.Should().ContainSingle();
        bd.Rows[0].Age.Captured.Should().BeTrue();
        bd.Rows[0].Gender.Captured.Should().BeTrue();
        bd.Totals.Should().BeNull("single-tier already carries demographics in Rows[0]");
    }
}
