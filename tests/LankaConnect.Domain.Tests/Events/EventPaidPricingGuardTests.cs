using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Pricing-guard fix (architect-approved 2026-05-04). Pre-existing latent bug surfaced
/// during 7F-E.4b browser verification: <c>Event.RegistrationMode.cs:740</c> and
/// <c>Event.cs:1130</c> both check the legacy invariant
/// (<c>Pricing == null &amp;&amp; TicketPrice == null</c>) BEFORE falling through to the
/// Tiered branch. A paid <c>TicketingMode = Tiered</c> event with active tiers IS
/// pricing-configured (each tier carries its own AdultPrice/ChildPrice), but the
/// guards reject it.
///
/// Why prod hadn't hit it: the FE event-create flow always calls SetDualPricing
/// alongside SetTicketingMode, redundantly populating Pricing. Operator's API-only
/// event creation on staging exposed the bug.
///
/// Fix: extract a private <c>HasPaidPricingConfigured()</c> helper on Event that
/// recognises three valid pricing shapes — legacy Pricing, legacy TicketPrice, OR
/// (TicketingMode == Tiered AND at least one active tier).
///
/// 5 tests below pin both the success path (regression-proof against future bugs that
/// re-introduce a legacy-only invariant) and the failure paths (regression guard for
/// the legitimate "paid event with no pricing configured" case).
/// </summary>
public class EventPaidPricingGuardTests
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  Builders
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a paid + Tiered event with active VIP + General tiers but
    /// deliberately does NOT call SetDualPricing — hits the previously-buggy guard.</summary>
    private static (Event @event, TicketTier vip, TicketTier general) CreateTieredEventWithoutLegacyPricing(
        decimal vipPrice = 50m, decimal generalPrice = 30m)
    {
        var title = EventTitle.Create("Pricing-guard test (paid + tiered)").Value;
        var description = EventDescription.Create("TDD coverage for the tiered-pricing guard").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: 100).Value;

        // Pricing INTENTIONALLY NOT SET — that's the bug repro.
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();

        var vipResult = ev.AddTicketTier(
            "VIP", "VIP tier",
            Money.Create(vipPrice, Currency.USD).Value, null, null,
            capacity: 20, maxPerUser: 10, sortOrder: 1);
        vipResult.IsSuccess.Should().BeTrue($"AddTicketTier VIP failed: {vipResult.Error}");

        var generalResult = ev.AddTicketTier(
            "General", "General tier",
            Money.Create(generalPrice, Currency.USD).Value, null, null,
            capacity: 30, maxPerUser: 10, sortOrder: 2);
        generalResult.IsSuccess.Should().BeTrue();

        ev.Publish().IsSuccess.Should().BeTrue();
        return (ev, vipResult.Value, generalResult.Value);
    }

    /// <summary>Creates a paid + Tiered event but adds NO tiers — domain should still
    /// reject with a sanitized message because no pricing of any kind is wired up.</summary>
    private static Event CreateTieredEventWithoutAnyPricingOrTiers()
    {
        var title = EventTitle.Create("Pricing-guard negative test").Value;
        var description = EventDescription.Create("Tiered mode set but no tiers added").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: 100).Value;
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();
        // Deliberately no SetDualPricing, no AddTicketTier.
        return ev;
    }

    /// <summary>Creates a paid event in Standard ticketing mode with NO pricing —
    /// the legacy guard should still fire (regression coverage).</summary>
    private static Event CreateStandardEventWithoutPricing()
    {
        var title = EventTitle.Create("Pricing-guard legacy regression").Value;
        var description = EventDescription.Create("Standard mode + no pricing — must still fail").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: 100).Value;
        // No SetTicketingMode → defaults to Standard. No SetDualPricing.
        return ev;
    }

    private static RegistrationContact Contact() =>
        RegistrationContact.Create("guard-test@example.com", "555-0100", null).Value;

    private static IReadOnlyList<TierCount> Tiers(params (TicketTier tier, int count)[] entries)
        => entries.Select(e => TierCount.Create(e.tier.Id, e.tier.Name, e.count).Value).ToList();

    private static AttendeeDetails Attendee(string name, AgeCategory age, Guid? tierId, string? tierName) =>
        AttendeeDetails.Create(name, age, gender: null, ticketTierId: tierId, ticketTierName: tierName).Value;

    // ─────────────────────────────────────────────────────────────────────────────
    //  Test cases
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Architect plan, test #1 — the staging repro.
    /// Paid + Tiered + B4 + active tiers + NO legacy Pricing → must succeed.</summary>
    [Fact]
    public void CalculatePriceForHeadCount_PaidTieredB4_NoLegacyPricing_Succeeds()
    {
        var (ev, vip, general) = CreateTieredEventWithoutLegacyPricing(vipPrice: 50m, generalPrice: 30m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAgeAndGender).IsSuccess.Should().BeTrue();

        var tiers = Tiers((vip, 2), (general, 1));
        var head = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 1, childMales: 0, childFemales: 0, tiers).Value;

        var result = ev.CalculatePriceForHeadCount(head);

        result.IsSuccess.Should().BeTrue($"errors: {result.Error}");
        // VIP × 2 = $100, General × 1 = $30, total = $130
        result.Value.Amount.Should().Be(130m);
        result.Value.Currency.Should().Be(Currency.USD);
    }

    /// <summary>Architect plan, test #2 — paid + Tiered + NO tiers should still fail
    /// because no pricing of any kind is configured. Sanitised user-facing message
    /// (no SetPricing()/SetDualPricing()/SetGroupPricing() leak).</summary>
    [Fact]
    public void CalculatePriceForHeadCount_PaidTiered_NoTiersConfigured_FailsWithSanitisedMessage()
    {
        var ev = CreateTieredEventWithoutAnyPricingOrTiers();
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value;

        var result = ev.CalculatePriceForHeadCount(head);

        result.IsFailure.Should().BeTrue();
        // Sanitised user-facing message — no domain method names.
        result.Error.Should().NotContain("SetPricing(", "user-facing error must not leak domain method names");
        result.Error.Should().NotContain("SetDualPricing(");
        result.Error.Should().NotContain("SetGroupPricing(");
        result.Error.Should().Contain("paid", "message should explain the cause clearly");
    }

    /// <summary>Architect plan, test #3 — Mode A path, paid + Tiered + active tiers +
    /// NO legacy Pricing. <c>CalculatePriceForAttendees</c> on the registration happy
    /// path is short-circuited to <c>CalculateTieredPriceForAttendees</c> at line 430,
    /// so the throw is dead code there. But the public method must not throw if called
    /// directly (e.g. by a future preview API) when the event is well-formed.</summary>
    [Fact]
    public void CalculatePriceForAttendees_PaidTieredModeA_NoLegacyPricing_DoesNotThrow()
    {
        var (ev, vip, general) = CreateTieredEventWithoutLegacyPricing(vipPrice: 50m, generalPrice: 30m);
        var attendees = new[]
        {
            Attendee("Alice", AgeCategory.Adult, vip.Id, "VIP"),
            Attendee("Bob",   AgeCategory.Adult, general.Id, "General"),
        };

        // CalculatePriceForAttendees is the public method holding the throw guard at line 1130.
        // After the fix, calling it directly on a tiered event with active tiers (no legacy
        // Pricing) must not throw "Paid event pricing is not configured" — the new
        // HasPaidPricingConfigured() helper recognises Tiered+active tiers as a valid
        // pricing shape.
        var act = () => ev.CalculatePriceForAttendees(attendees);

        act.Should().NotThrow<InvalidOperationException>(
            "tiered events with active tiers ARE pricing-configured");
    }

    /// <summary>Architect plan, test #4 — Standard mode + no pricing must still throw.
    /// Regression guard for the legacy invariant. Domain method calls (not user input)
    /// can rely on the throw to signal "caller is buggy".</summary>
    [Fact]
    public void CalculatePriceForAttendees_StandardMode_NoPricing_StillThrows()
    {
        var ev = CreateStandardEventWithoutPricing();
        var attendees = new[]
        {
            Attendee("Alice", AgeCategory.Adult, tierId: null, tierName: null),
        };

        var act = () => ev.CalculatePriceForAttendees(attendees);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*paid*", "the legacy guard must still fire when no pricing of any shape is configured");
    }

    /// <summary>Architect plan, test #5 — B-mode + Standard + no pricing fails with
    /// sanitised message. Regression guard for the legacy invariant on the head-count path.</summary>
    [Fact]
    public void CalculatePriceForHeadCount_StandardMode_NoPricing_FailsWithSanitisedMessage()
    {
        var ev = CreateStandardEventWithoutPricing();
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForTotalOnly(3).Value;

        var result = ev.CalculatePriceForHeadCount(head);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotContain("SetPricing(");
        result.Error.Should().NotContain("SetDualPricing(");
        result.Error.Should().NotContain("SetGroupPricing(");
        result.Error.Should().Contain("paid", "message should be a clean user-facing explanation");
    }
}
