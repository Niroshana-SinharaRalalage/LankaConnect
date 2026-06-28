using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E.2 — [Theory]-driven compatibility tests over the 14-row table from the Phase 7E plan §2.
///
/// Architect-required pattern (review iteration 2): "Drive validator tests from a [Theory] data
/// table rather than hand-rolled [Fact]s, or coverage will rot." Every row of the compatibility
/// table is exercised by <see cref="CompatibilityMatrix_ReturnsExpectedAllowedSet"/> below — adding
/// a new event-shape axis means adding a new column to <see cref="CompatibilityRows"/> and the
/// table stays exhaustive by construction.
///
/// Out of the 14 plan rows, the ones we can fully exercise with axes representable on today's
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Event"/> aggregate are the ones below. The "named seating"
/// / "identity-bound add-on" / "matrix pricing" / "names required per ticket" axes require fields
/// not yet on <see cref="LankaConnect.Products.LankaEvents.Domain.Event"/>; they are tested via the
/// <see cref="RegistrationModeContext"/> fields which default to <c>false</c> in callers today
/// and will be exercised end-to-end when those fields are introduced (Phase 7F).
/// </summary>
public class Phase7E2RegistrationModeCompatibilityTests
{
    public static IEnumerable<object[]> CompatibilityRows()
    {
        // Each row = (description, context-shape-args, expected allowed modes)
        // Rows mirror the 14-row table in the Phase 7E plan §2.

        // 1. Free, no add-ons, names not required → All 6.
        yield return new object[]
        {
            "Free, no constraints — all 6 modes",
            new RegistrationModeContext { IsFreeAttendance = true },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountOnly,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByGender,
                RegistrationMode.HeadCountByAgeAndGender,
                RegistrationMode.NoRegistration,
            },
        };

        // 2. Free, names required per ticket → A only.
        yield return new object[]
        {
            "Free + names required per ticket → A only",
            new RegistrationModeContext { IsFreeAttendance = true, RequiresAttendeeNameOnTicket = true },
            new[] { RegistrationMode.DetailedAttendees },
        };

        // 3. Free + count-based add-on → A or any B (no flag for count-based here; they're allowed by default in B).
        // (Already covered by row 1 — count-based add-ons don't restrict B modes.)

        // 4. Free + identity-bound add-on → A only.
        yield return new object[]
        {
            "Free + identity-bound add-on → A only",
            new RegistrationModeContext { IsFreeAttendance = true, HasIdentityBoundAddOn = true },
            new[] { RegistrationMode.DetailedAttendees },
        };

        // 5. Paid attendance, single flat price, names not required → A + all B (no C).
        // Phase 7E.3b (2026-04-29) shipped paid B-mode + Stripe checkout, lifting the
        // PaidHeadCountDeferred gate. This row is back to its target-state plan §2 expectation.
        yield return new object[]
        {
            "Paid single price, names not required — A + all B (no C)",
            new RegistrationModeContext { IsFreeAttendance = false },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountOnly,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByGender,
                RegistrationMode.HeadCountByAgeAndGender,
            },
        };

        // 6. Paid attendance, single flat price, names required → A only.
        yield return new object[]
        {
            "Paid single price + names required → A only",
            new RegistrationModeContext { IsFreeAttendance = false, RequiresAttendeeNameOnTicket = true },
            new[] { RegistrationMode.DetailedAttendees },
        };

        // 7. Paid dual pricing (Adult/Child) → A, B2, or B4 (B1/B3 carry no age info).
        // Phase 7E.3b restored target-state.
        yield return new object[]
        {
            "Paid dual pricing → A, B2, or B4",
            new RegistrationModeContext { IsFreeAttendance = false, HasDualPricing = true },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByAgeAndGender,
            },
        };

        // 8. Paid + group-tier discount (count-based) → A + all B (no C). 7E.3b restored.
        yield return new object[]
        {
            "Paid + group-tier discount — A + all B (no C)",
            new RegistrationModeContext { IsFreeAttendance = false, HasGroupTiers = true },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountOnly,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByGender,
                RegistrationMode.HeadCountByAgeAndGender,
            },
        };

        // 9. Paid + ticket tiers (mixed-tier flat prices via TierCounts axis) → A + all B (no C).
        // Note: TierCounts itself is gated by PaidHeadCountTiersDeferred in Event.CalculateHeadCountPrice
        // until 7E.3c ships, but the COMPATIBILITY VALIDATOR allows the mode/shape combination
        // (it just rejects at registration time if tier counts are present). Mode picker can
        // safely show B options for tiered events.
        yield return new object[]
        {
            "Paid + ticket tiers (mixed flat prices) — A + all B (no C)",
            new RegistrationModeContext { IsFreeAttendance = false, HasTicketTiers = true },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountOnly,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByGender,
                RegistrationMode.HeadCountByAgeAndGender,
            },
        };

        // 10. Paid + tier × age matrix pricing → A only.
        yield return new object[]
        {
            "Paid + matrix pricing → A only (Phase 7F adds matrix axis)",
            new RegistrationModeContext { IsFreeAttendance = false, HasMatrixPricing = true },
            new[] { RegistrationMode.DetailedAttendees },
        };

        // 11. Seating, named seats → A only.
        yield return new object[]
        {
            "Seating + named seats → A only",
            new RegistrationModeContext { IsFreeAttendance = true, HasSeating = true, HasNamedSeating = true },
            new[] { RegistrationMode.DetailedAttendees },
        };

        // 12. Seating, auto-allocated block (no per-seat names) → A or any B (NOT C).
        yield return new object[]
        {
            "Seating + auto-allocated block — A + all B (no C — block needs Registration to bind)",
            new RegistrationModeContext { IsFreeAttendance = true, HasSeating = true, HasNamedSeating = false },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountOnly,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByGender,
                RegistrationMode.HeadCountByAgeAndGender,
            },
        };

        // 13. Mode C requires free + no seating; any non-free shape excludes C.
        yield return new object[]
        {
            "Free + seating (auto-block) — C excluded by seating rule",
            new RegistrationModeContext { IsFreeAttendance = true, HasSeating = true },
            new[]
            {
                RegistrationMode.DetailedAttendees,
                RegistrationMode.HeadCountOnly,
                RegistrationMode.HeadCountByAge,
                RegistrationMode.HeadCountByGender,
                RegistrationMode.HeadCountByAgeAndGender,
            },
        };
    }

    [Theory]
    [MemberData(nameof(CompatibilityRows))]
    public void CompatibilityMatrix_ReturnsExpectedAllowedSet(
        string description, RegistrationModeContext context, RegistrationMode[] expectedAllowed)
    {
        var allowed = RegistrationModeCompatibility.AllowedModes(context);

        allowed.Should().BeEquivalentTo(
            expectedAllowed,
            $"row: {description}");
    }

    [Theory]
    [MemberData(nameof(CompatibilityRows))]
    public void Check_AgreesWith_AllowedModes(
        string description, RegistrationModeContext context, RegistrationMode[] expectedAllowed)
    {
        // For each mode in the enum, Check(mode, ctx).IsSuccess must equal "mode is in expectedAllowed".
        // This is the bidirectional contract — Check and AllowedModes must never disagree.
        foreach (RegistrationMode mode in Enum.GetValues(typeof(RegistrationMode)))
        {
            var checkResult = RegistrationModeCompatibility.Check(mode, context);
            var shouldBeAllowed = expectedAllowed.Contains(mode);
            checkResult.IsSuccess.Should().Be(shouldBeAllowed,
                $"row '{description}' — mode {mode}: Check disagreed with AllowedModes");
        }
    }

    [Fact]
    public void NullContext_IsRejected_ByCheck()
    {
        var result = RegistrationModeCompatibility.Check(RegistrationMode.DetailedAttendees, null!);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void NullContext_ReturnsDetailedAttendeesOnly_FromAllowedModes()
    {
        var allowed = RegistrationModeCompatibility.AllowedModes(null!);
        allowed.Should().BeEquivalentTo(new[] { RegistrationMode.DetailedAttendees });
    }

    [Fact]
    public void DetailedAttendees_IsAlways_Allowed()
    {
        // No matter what shape, mode A must always be in the allowed set.
        // This is an architectural invariant — A is the maximum-info capture; never excluded.
        var anyShape = new RegistrationModeContext
        {
            IsFreeAttendance = false,
            HasSeating = true,
            HasNamedSeating = true,
            RequiresAttendeeNameOnTicket = true,
            HasDualPricing = true,
            HasGroupTiers = true,
            HasTicketTiers = true,
            HasIdentityBoundAddOn = true,
            HasMatrixPricing = true,
        };
        var allowed = RegistrationModeCompatibility.AllowedModes(anyShape);
        allowed.Should().Contain(RegistrationMode.DetailedAttendees);
    }

    #region Phase 7E.3b gate-removal regression guards (2026-04-29)

    /// <summary>
    /// Phase 7E.3b shipped paid B-mode + Stripe checkout. Free + any B-mode must continue to
    /// succeed (regression guard kept from the gate-era tests; the gate is gone but free must
    /// still be allowed unconditionally).
    /// </summary>
    [Theory]
    [InlineData(RegistrationMode.HeadCountOnly)]
    [InlineData(RegistrationMode.HeadCountByAge)]
    [InlineData(RegistrationMode.HeadCountByGender)]
    [InlineData(RegistrationMode.HeadCountByAgeAndGender)]
    public void Check_Succeeds_ForFree_BModes(RegistrationMode bMode)
    {
        var freeContext = new RegistrationModeContext { IsFreeAttendance = true };

        var result = RegistrationModeCompatibility.Check(bMode, freeContext);

        result.IsSuccess.Should().BeTrue($"free + {bMode} must always pass compatibility");
    }

    /// <summary>
    /// Phase 7E.3b: paid + B1/B2/B4 must now PASS compatibility (B3 single price too; only
    /// B1/B3 + dual pricing fail because they carry no age info to drive dual-price math).
    /// </summary>
    [Theory]
    [InlineData(RegistrationMode.HeadCountOnly)]
    [InlineData(RegistrationMode.HeadCountByAge)]
    [InlineData(RegistrationMode.HeadCountByGender)]
    [InlineData(RegistrationMode.HeadCountByAgeAndGender)]
    public void Check_Succeeds_ForPaid_BModes_SinglePrice(RegistrationMode bMode)
    {
        var paidSingle = new RegistrationModeContext { IsFreeAttendance = false };

        var result = RegistrationModeCompatibility.Check(bMode, paidSingle);

        result.IsSuccess.Should().BeTrue(
            $"paid + {bMode} (single price) must pass — Phase 7E.3b shipped this path");
    }

    /// <summary>
    /// AllowedModes for a paid (single-price) event must include all four B-modes after 7E.3b.
    /// Direct counter-test to the pre-7E.3b assertion (which asserted exclusion).
    /// </summary>
    [Fact]
    public void AllowedModes_IncludesAllBModes_ForPaidSinglePriceEvents()
    {
        var paidContext = new RegistrationModeContext { IsFreeAttendance = false };

        var allowed = RegistrationModeCompatibility.AllowedModes(paidContext);

        allowed.Should().Contain(RegistrationMode.HeadCountOnly);
        allowed.Should().Contain(RegistrationMode.HeadCountByAge);
        allowed.Should().Contain(RegistrationMode.HeadCountByGender);
        allowed.Should().Contain(RegistrationMode.HeadCountByAgeAndGender);
        allowed.Should().Contain(RegistrationMode.DetailedAttendees);
        allowed.Should().NotContain(RegistrationMode.NoRegistration,
            "Mode C structurally requires free attendance");
    }

    #endregion
}
