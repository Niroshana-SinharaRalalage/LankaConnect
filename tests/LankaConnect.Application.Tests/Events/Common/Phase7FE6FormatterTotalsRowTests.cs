using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 7F-E.6.A (architect-approved 2026-05-04): the formatter must surface
/// registration-level demographics for multi-tier B-mode breakdowns via a new
/// <c>Totals</c> row on <see cref="RegistrationBreakdown"/>. Pre-fix the per-tier
/// rows in multi-tier B-mode marked Age + Gender as NotCaptured (per architect
/// Phase 7F-C §2.2 #4 deferred per-tier-gender storage), and the captured 4-leaf
/// at the registration level was never displayed anywhere — operator caught
/// this on staging event 616e59f3 where his B4 RSVP stored
/// <c>{adultMales:2, adultFemales:2, childMales:2, childFemales:2}</c> but the
/// event-detail card and PDF ticket both showed "N/A" for every per-tier row.
///
/// Architect-approved shape: <see cref="RegistrationBreakdownTotals"/> record
/// with only <c>Age</c> + <c>Gender</c> pairs (no TierName, no Count — the
/// existing <see cref="RegistrationBreakdown.TotalAttendees"/> carries that).
/// Populated only when <c>IsTiered &amp;&amp; Rows.Count &gt; 1 &amp;&amp;
/// (captureAge || captureGender)</c>; null otherwise.
///
/// Per-tier rows keep the existing N/A semantics so the architect §2.2 #4
/// "no per-tier gender storage" decision is preserved — the Totals row is the
/// honest read-side surface for the captured registration-level data.
/// </summary>
public class Phase7FE6FormatterTotalsRowTests
{
    private static IReadOnlyList<TierCount> Tiers(params (string name, int count)[] entries)
    {
        var result = new List<TierCount>();
        foreach (var (name, count) in entries)
            result.Add(TierCount.Create(Guid.NewGuid(), name, count).Value);
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  Multi-tier B-mode — Totals row populated when registration-level captured
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void B4_MultiTier_4LeafCaptured_TotalsRowSurfacesRegistrationLevelDemographics()
    {
        // Operator's actual staging RSVP: VIP × 4 + Standard × 4, 4-leaf = 2/2/2/2.
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 2, childMales: 2, childFemales: 2,
            tierCounts: Tiers(("VIP", 4), ("Standard", 4))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAgeAndGender);

        bd.IsTiered.Should().BeTrue();
        bd.Rows.Should().HaveCount(2);
        // Per-tier rows stay N/A — preserves architect Phase 7F-C §2.2 #4.
        foreach (var row in bd.Rows)
        {
            row.Age.Captured.Should().BeFalse(
                "B4 multi-tier without HasAgeSplit doesn't store per-tier age");
            row.Gender.Captured.Should().BeFalse(
                "B4 multi-tier doesn't store per-tier gender (architect §2.2 #4)");
        }
        // Totals row surfaces what's captured at registration level.
        bd.Totals.Should().NotBeNull();
        bd.Totals!.Age.Captured.Should().BeTrue();
        bd.Totals.Age.Left.Should().Be(4, "2 AM + 2 AF = 4 adults");
        bd.Totals.Age.Right.Should().Be(4, "2 CM + 2 CF = 4 children");
        bd.Totals.Gender.Captured.Should().BeTrue();
        bd.Totals.Gender.Left.Should().Be(4, "2 AM + 2 CM = 4 males");
        bd.Totals.Gender.Right.Should().Be(4, "2 AF + 2 CF = 4 females");
    }

    [Fact]
    public void B3_MultiTier_GenderCaptured_TotalsRowHasGenderOnly()
    {
        var hc = HeadCountBreakdown.ForByGender(
            males: 5, females: 3,
            tierCounts: Tiers(("VIP", 4), ("Standard", 4))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByGender);

        bd.Totals.Should().NotBeNull();
        bd.Totals!.Gender.Captured.Should().BeTrue();
        bd.Totals.Gender.Left.Should().Be(5);
        bd.Totals.Gender.Right.Should().Be(3);
        bd.Totals.Age.Captured.Should().BeFalse(
            "B3 doesn't capture age, so the Totals row's Age axis is NotCaptured");
    }

    [Fact]
    public void B2_MultiTier_AgeCaptured_TotalsRowHasAgeOnly()
    {
        var hc = HeadCountBreakdown.ForByAge(
            adults: 5, children: 3,
            tierCounts: Tiers(("VIP", 4), ("Standard", 4))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAge);

        bd.Totals.Should().NotBeNull();
        bd.Totals!.Age.Captured.Should().BeTrue();
        bd.Totals.Age.Left.Should().Be(5);
        bd.Totals.Age.Right.Should().Be(3);
        bd.Totals.Gender.Captured.Should().BeFalse(
            "B2 doesn't capture gender");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  Cases where Totals must be NULL
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void B1_MultiTier_NoDemographics_TotalsRowNull()
    {
        var hc = HeadCountBreakdown.ForTotalOnly(
            total: 8,
            tierCounts: Tiers(("VIP", 4), ("Standard", 4))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountOnly);

        bd.Totals.Should().BeNull(
            "B1 captures no demographic axes — there's nothing to surface in a Totals row");
    }

    [Fact]
    public void B4_SingleTier_TotalsRowNull_PerTierRowAlreadyCarriesIt()
    {
        // Single-tier B4: the existing formatter promotes registration-level demographics
        // into the sole row's Age + Gender (singleTier branch). Adding a duplicate Totals
        // row would be noise — Totals must stay null.
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 1, adultFemales: 1, childMales: 1, childFemales: 1,
            tierCounts: Tiers(("VIP", 4))).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAgeAndGender);

        bd.Rows.Should().HaveCount(1);
        bd.Rows[0].Age.Captured.Should().BeTrue("single-tier B4 promotes registration-level demographics into the sole row");
        bd.Rows[0].Gender.Captured.Should().BeTrue();
        bd.Totals.Should().BeNull(
            "single-tier breakdown's only row already carries the demographics; Totals would duplicate");
    }

    [Fact]
    public void B4_NonTiered_TotalsRowNull()
    {
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 1, adultFemales: 1, childMales: 1, childFemales: 1).Value;

        var bd = RegistrationBreakdownFormatter.FromHeadCount(
            hc, RegistrationMode.HeadCountByAgeAndGender);

        bd.IsTiered.Should().BeFalse();
        bd.Rows.Should().HaveCount(1);
        bd.Totals.Should().BeNull(
            "non-tiered breakdowns have a single row that carries the demographics");
    }

    [Fact]
    public void ModeA_MultiTier_TotalsRowNull_PerAttendeeDataIsSourceOfTruth()
    {
        // Mode A multi-tier (FromAttendees path): per-tier rows aggregate per-attendee
        // age/gender from the attendees in each tier group. The registration doesn't store
        // a separate "totals" — summing the per-tier rows IS the total. Totals stays null.
        var vipId = Guid.NewGuid();
        var generalId = Guid.NewGuid();
        var attendees = new[]
        {
            AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female, ticketTierId: vipId, ticketTierName: "VIP").Value,
            AttendeeDetails.Create("Bob",   AgeCategory.Adult, Gender.Male,   ticketTierId: vipId, ticketTierName: "VIP").Value,
            AttendeeDetails.Create("Cara",  AgeCategory.Child, Gender.Female, ticketTierId: generalId, ticketTierName: "General").Value,
        };

        var bd = RegistrationBreakdownFormatter.FromAttendees(attendees);

        bd.IsTiered.Should().BeTrue();
        bd.Rows.Should().HaveCount(2);
        bd.Totals.Should().BeNull(
            "Mode A per-tier rows are computed from per-attendee data — summing them IS the total");
    }
}
