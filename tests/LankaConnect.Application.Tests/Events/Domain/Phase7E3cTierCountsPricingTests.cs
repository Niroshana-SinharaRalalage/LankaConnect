using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E.3c — paid B-mode RSVP with TierCounts axis pricing.
/// Architect-required (review iteration 1): TierCounts pricing must produce identical
/// <see cref="Money"/> totals to Mode A's <see cref="Event.CalculateTieredPriceForAttendees"/>
/// for an equivalent basket. Per-tier capacity reservation must happen BEFORE pricing —
/// applies to free + paid tiered events (architect edit #2).
/// </summary>
public class Phase7E3cTierCountsPricingTests
{
    private static (Event @event, TicketTier vip, TicketTier general) CreateTieredEvent(
        decimal vipPrice = 50m, int vipCapacity = 10,
        decimal generalPrice = 30m, int generalCapacity = 40)
    {
        var title = EventTitle.Create("7E.3c tiered test").Value;
        var description = EventDescription.Create("TierCounts pricing").Value;
        // Capacity must be ≥ tier capacities so AddTicketTier doesn't reject.
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: vipCapacity + generalCapacity + 50).Value;
        ev.SetPricing(Money.Create(vipPrice, Currency.USD).Value).IsSuccess.Should().BeTrue();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();

        var vipResult = ev.AddTicketTier(
            "VIP", "VIP tier",
            Money.Create(vipPrice, Currency.USD).Value, null, null,
            capacity: vipCapacity, maxPerUser: vipCapacity, sortOrder: 1);
        vipResult.IsSuccess.Should().BeTrue($"AddTicketTier VIP failed: {vipResult.Error}");

        var generalResult = ev.AddTicketTier(
            "General", "General tier",
            Money.Create(generalPrice, Currency.USD).Value, null, null,
            capacity: generalCapacity, maxPerUser: generalCapacity, sortOrder: 2);
        generalResult.IsSuccess.Should().BeTrue();

        ev.Publish().IsSuccess.Should().BeTrue();
        return (ev, vipResult.Value, generalResult.Value);
    }

    private static RegistrationContact Contact(string email = "tier@example.com") =>
        RegistrationContact.Create(email, "555-0100", null).Value;

    private static IReadOnlyList<TierCount> Tiers(params (TicketTier tier, int count)[] entries)
        => entries.Select(e => TierCount.Create(e.tier.Id, e.tier.Name, e.count).Value).ToList();

    [Fact]
    public void RsvpToEvent_ModeB1Paid_TierCounts_PricesCorrectly()
    {
        var (ev, vip, general) = CreateTieredEvent(vipPrice: 50m, generalPrice: 30m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var tiers = Tiers((vip, 2), (general, 3));
        var head = HeadCountBreakdown.ForTotalOnly(5, tiers).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        var registration = ev.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(190m, "VIP × 2 ($100) + General × 3 ($90) = $190");
        registration.Status.Should().Be(RegistrationStatus.Preliminary,
            "tiered paid event awaits Stripe webhook before Confirmed");
    }

    [Fact]
    public void RsvpToEvent_ModeB3Paid_TierCounts_PricesIdenticallyToB1()
    {
        var (ev, vip, general) = CreateTieredEvent(vipPrice: 50m, generalPrice: 30m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByGender).IsSuccess.Should().BeTrue();
        var tiers = Tiers((vip, 2), (general, 3));
        var head = HeadCountBreakdown.ForByGender(males: 3, females: 2, tiers).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());

        result.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(190m, "B3 + tiers — gender ignored, tier prices used");
    }

    [Fact]
    public void RsvpToEvent_ModeBPaid_TierCounts_RejectsUnknownTierId()
    {
        var (ev, _, _) = CreateTieredEvent();
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var bogusTier = TierCount.Create(Guid.NewGuid(), "Phantom", 2).Value;
        var head = HeadCountBreakdown.ForTotalOnly(2, new[] { bogusTier }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public void RsvpToEvent_ModeBPaid_TierCounts_RejectsWhenTierOversold()
    {
        // VIP capacity = 2 only; request 5 VIP seats
        var (ev, vip, general) = CreateTieredEvent(vipCapacity: 2, generalCapacity: 100);
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var tiers = Tiers((vip, 5));
        var head = HeadCountBreakdown.ForTotalOnly(5, tiers).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());

        result.IsFailure.Should().BeTrue("VIP tier oversold — should reject before pricing");
        ev.Registrations.Should().BeEmpty("no registration created when tier reservation fails");
    }

    [Fact]
    public void RsvpToEvent_ModeBPaid_TierCounts_TwoConcurrentRsvps_OnlyOneSucceeds()
    {
        // Single VIP capacity = 1. Two RSVPs each requesting 1 VIP — second must fail.
        var (ev, vip, _) = CreateTieredEvent(vipCapacity: 1);
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();

        var firstHead = HeadCountBreakdown.ForTotalOnly(1, Tiers((vip, 1))).Value;
        var firstResult = ev.RegisterWithHeadCount(Guid.NewGuid(), "First", firstHead, Contact("first@example.com"));
        firstResult.IsSuccess.Should().BeTrue();

        var secondHead = HeadCountBreakdown.ForTotalOnly(1, Tiers((vip, 1))).Value;
        var secondResult = ev.RegisterWithHeadCount(Guid.NewGuid(), "Second", secondHead, Contact("second@example.com"));

        secondResult.IsFailure.Should().BeTrue("VIP tier already at capacity (1/1)");
        ev.Registrations.Should().HaveCount(1, "only the first RSVP should land");
    }

    [Fact]
    public void RsvpToEvent_FreeTieredBMode_StillReservesTierCapacity()
    {
        // Architect edit #2: free + tiered events MUST reserve per-tier capacity to prevent
        // over-selling. Move tier.Reserve OUT of pricing path INTO RegisterWithHeadCount
        // pre-pricing — applies to both free and paid.
        var title = EventTitle.Create("Free tiered").Value;
        var description = EventDescription.Create("Phase 7E.3c free + tiered").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), 100).Value;
        ev.SetAsFreeEvent().IsSuccess.Should().BeTrue();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();
        var vipResult = ev.AddTicketTier("VIP", "VIP", Money.Create(0m, Currency.USD).Value, null, null, capacity: 2, maxPerUser: 2, sortOrder: 1);
        vipResult.IsSuccess.Should().BeTrue();
        var vip = vipResult.Value;
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();

        // First RSVP claims both VIP seats
        var firstHead = HeadCountBreakdown.ForTotalOnly(2, Tiers((vip, 2))).Value;
        var first = ev.RegisterWithHeadCount(Guid.NewGuid(), "First", firstHead, Contact("a@e.com"));
        first.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(0m, "free event TotalPrice = 0");
        ev.Registrations.Single().Status.Should().Be(RegistrationStatus.Confirmed,
            "free events confirm immediately");

        // Second RSVP requesting another VIP seat must fail — capacity reservation works for free events too
        var secondHead = HeadCountBreakdown.ForTotalOnly(1, Tiers((vip, 1))).Value;
        var second = ev.RegisterWithHeadCount(Guid.NewGuid(), "Second", secondHead, Contact("b@e.com"));
        second.IsFailure.Should().BeTrue("VIP tier oversold even though event is free");
    }

    /// <summary>
    /// Architect-required parity test (carryover from 7E.3b plan): same basket via Mode A's
    /// CalculateTieredPriceForAttendees and Mode B's TierCounts pricing must produce identical
    /// Money values. Anti-fork guard.
    /// </summary>
    [Fact]
    public void PaidB_TierCounts_HasIdenticalTotalPrice_To_PaidA_WithSameBasket()
    {
        // Mode A event
        var (modeAEvent, modeAVip, modeAGeneral) = CreateTieredEvent(vipPrice: 50m, generalPrice: 30m);
        var attendees = new List<AttendeeDetails>();
        for (var i = 0; i < 2; i++)
            attendees.Add(AttendeeDetails.Create($"VIP{i}", AgeCategory.Adult, ticketTierId: modeAVip.Id, ticketTierName: modeAVip.Name).Value);
        for (var i = 0; i < 3; i++)
            attendees.Add(AttendeeDetails.Create($"Gen{i}", AgeCategory.Adult, ticketTierId: modeAGeneral.Id, ticketTierName: modeAGeneral.Name).Value);

        var modeAPriceResult = modeAEvent.CalculateTieredPriceForAttendees(attendees);
        modeAPriceResult.IsSuccess.Should().BeTrue($"Mode A pricing: {modeAPriceResult.Error}");

        // Mode B event with same tier prices + counts
        var (modeBEvent, modeBVip, modeBGeneral) = CreateTieredEvent(vipPrice: 50m, generalPrice: 30m);
        modeBEvent.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForTotalOnly(5, Tiers((modeBVip, 2), (modeBGeneral, 3))).Value;

        var modeBResult = modeBEvent.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());
        modeBResult.IsSuccess.Should().BeTrue($"Mode B pricing: {string.Join("; ", modeBResult.Errors ?? Enumerable.Empty<string>())}");
        var modeBPrice = modeBEvent.Registrations.Single().TotalPrice!;

        modeBPrice.Amount.Should().Be(modeAPriceResult.Value!.Amount,
            "Mode A vs Mode B + TierCounts must produce identical TotalPrice — anti-fork guard");
        modeBPrice.Currency.Should().Be(modeAPriceResult.Value.Currency);
        modeBPrice.Amount.Should().Be(190m, "sanity: 2 × $50 + 3 × $30 = $190");
    }

    [Fact]
    public void RsvpToEvent_TieredEvent_RejectsRsvp_WithoutTierCounts()
    {
        // Defensive — tiered events MUST receive TierCounts. The architect edit #2 guard
        // is in RegisterWithHeadCount BEFORE pricing.
        var (ev, _, _) = CreateTieredEvent();
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForTotalOnly(3).Value; // No TierCounts

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*Tiered events require TierCounts*");
    }
}
