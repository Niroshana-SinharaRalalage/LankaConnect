using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.SharedKernel.Money;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E.3b â€” paid B-mode pricing tests. Architect-required (review iteration 1):
/// the pricing helper must produce the SAME <see cref="Money"/> total as Mode A's
/// <see cref="Event.CalculatePriceForAttendees"/> for an equivalent basket.
///
/// Tests exercise <see cref="Event.RegisterWithHeadCount"/> end-to-end (the public
/// surface) so the domain method's `TotalPrice` can be inspected via the resulting
/// <see cref="Registration.TotalPrice"/>.
/// </summary>
public class Phase7E3bPaidHeadCountPricingTests
{
    private static Event CreatePublishedPaidEvent(decimal singleAdultPrice = 50m, int capacity = 100)
    {
        var title = EventTitle.Create("Paid B-mode pricing test").Value;
        var description = EventDescription.Create("Phase 7E.3b").Value;
        var @event = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity).Value;
        @event.SetPricing(new Money(singleAdultPrice, Currency.USD)).IsSuccess.Should().BeTrue();
        @event.Publish().IsSuccess.Should().BeTrue();
        return @event;
    }

    private static Event CreatePublishedDualPriceEvent(decimal adultPrice = 15m, decimal childPrice = 7m, int childAgeLimit = 12, int capacity = 100)
    {
        var title = EventTitle.Create("Paid B-mode dual-price test").Value;
        var description = EventDescription.Create("Phase 7E.3b").Value;
        var @event = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity).Value;
        var pricing = TicketPricing.CreateDualPrice(
            new Money(adultPrice, Currency.USD),
            new Money(childPrice, Currency.USD),
            childAgeLimit).Value;
        @event.SetDualPricing(pricing).IsSuccess.Should().BeTrue();
        @event.Publish().IsSuccess.Should().BeTrue();
        return @event;
    }

    private static RegistrationContact CreateContact(string email = "lead@example.com") =>
        RegistrationContact.Create(email, "555-0100", null).Value;

    [Fact]
    public void RsvpToEvent_ModeB1Paid_SinglePrice_TotalPriceEquals_TotalTimesPrice()
    {
        var @event = CreatePublishedPaidEvent(singleAdultPrice: 25m);
        @event.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForTotalOnly(4).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "B1 Lead", head, CreateContact());

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        var registration = @event.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(100m, "4 Ã— $25 = $100");
        registration.TotalPrice.Currency.Should().Be(Currency.USD);
        registration.Status.Should().Be(RegistrationStatus.Preliminary,
            "paid event must land in Preliminary until Stripe webhook confirms payment");
    }

    [Fact]
    public void RsvpToEvent_ModeB2Paid_DualPrice_TotalPriceEquals_AdultsTimesAdultPrice_PlusChildrenTimesChildPrice()
    {
        var @event = CreatePublishedDualPriceEvent(adultPrice: 15m, childPrice: 7m);
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "B2 Lead", head, CreateContact());

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        var registration = @event.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(37m, "2 Ã— $15 + 1 Ã— $7 = $37");
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
    }

    [Fact]
    public void RsvpToEvent_ModeB4Paid_DualPrice_DerivesAdultsAndChildren_FromFourLeaves_AndPricesCorrectly()
    {
        var @event = CreatePublishedDualPriceEvent(adultPrice: 20m, childPrice: 10m);
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAgeAndGender).IsSuccess.Should().BeTrue();
        // 1 AM + 2 AF = 3 adults; 1 CM + 1 CF = 2 children â†’ 3Ã—20 + 2Ã—10 = $80
        var head = HeadCountBreakdown.ForByAgeAndGender(adultMales: 1, adultFemales: 2, childMales: 1, childFemales: 1).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "B4 Lead", head, CreateContact());

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        var registration = @event.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(80m, "3 adults Ã— $20 + 2 children Ã— $10 = $80");
    }

    [Fact]
    public void RsvpToEvent_ModeB3Paid_SinglePrice_TotalPriceEquals_TotalTimesPrice()
    {
        // B3 single-price parity with B1 â€” gender doesn't affect price.
        var @event = CreatePublishedPaidEvent(singleAdultPrice: 30m);
        @event.SetRegistrationMode(RegistrationMode.HeadCountByGender).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForByGender(males: 2, females: 3).Value; // total = 5

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "B3 Lead", head, CreateContact());

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        var registration = @event.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(150m, "5 Ã— $30 = $150");
    }

    [Fact]
    public void RsvpToEvent_ModeB1Paid_DualPricing_Rejected()
    {
        // Defensive â€” validator excludes this combo, but domain enforces too.
        var @event = CreatePublishedDualPriceEvent();
        @event.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForTotalOnly(3).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "B1 Lead", head, CreateContact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*HeadCountOnly cannot be used with dual pricing*");
    }

    [Fact]
    public void RsvpToEvent_ModeB3Paid_DualPricing_Rejected()
    {
        var @event = CreatePublishedDualPriceEvent();
        @event.SetRegistrationMode(RegistrationMode.HeadCountByGender).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForByGender(males: 1, females: 2).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "B3 Lead", head, CreateContact());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*HeadCountByGender cannot be used with dual pricing*");
    }

    [Fact]
    public void RsvpToEvent_ModeBPaid_TierCounts_OnNonTieredEvent_Rejected()
    {
        // Phase 7E.3c: TierCounts only valid on TicketingMode.Tiered events. Sending TierCounts
        // to a SingleTier event is rejected with a clear "tier-counts only with tiered ticketing"
        // message (the PaidHeadCountTiersDeferred gate was lifted; tiered events with TierCounts
        // succeed via Phase7E3cTierCountsPricingTests).
        var @event = CreatePublishedPaidEvent(singleAdultPrice: 50m);
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();
        var tier = TierCount.Create(Guid.NewGuid(), "VIP", 2).Value;
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 0, tierCounts: new[] { tier }).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, CreateContact());

        result.IsFailure.Should().BeTrue("TierCounts on a SingleTier event is invalid");
        result.Errors.Should().Contain(e => e.Contains("TicketingMode.Tiered"));
    }

    /// <summary>
    /// Architect-required parity test (review iteration 1, edit #1 of 7E.3b plan):
    /// same basket via Mode A's <see cref="Event.CalculatePriceForAttendees"/> and Mode B2's
    /// new pricing helper produces identical <see cref="Money"/> values. Anti-fork guard.
    /// </summary>
    [Fact]
    public void PaidB2_HasIdenticalTotalPrice_To_PaidA_WithSameAdultChildCounts()
    {
        // Build two events with identical pricing.
        var modeAEvent = CreatePublishedDualPriceEvent(adultPrice: 15m, childPrice: 7m);
        var modeBEvent = CreatePublishedDualPriceEvent(adultPrice: 15m, childPrice: 7m);
        modeBEvent.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        // Mode A: 2 adults + 1 child as discrete attendees.
        var attendees = new[]
        {
            AttendeeDetails.Create("A1", AgeCategory.Adult).Value,
            AttendeeDetails.Create("A2", AgeCategory.Adult).Value,
            AttendeeDetails.Create("C1", AgeCategory.Child).Value,
        };
        var modeAPrice = modeAEvent.CalculatePriceForAttendees(attendees);
        modeAPrice.IsSuccess.Should().BeTrue($"Mode A pricing failed: {modeAPrice.Error}");

        // Mode B2: same basket via head-count.
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value;
        var modeBResult = modeBEvent.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, CreateContact());
        modeBResult.IsSuccess.Should().BeTrue($"Mode B2 pricing failed: {string.Join("; ", modeBResult.Errors ?? Enumerable.Empty<string>())}");
        var modeBPrice = modeBEvent.Registrations.Single().TotalPrice!;

        // Architect-required: identical Money. Down-to-the-cent equality.
        modeBPrice.Amount.Should().Be(modeAPrice.Value.Amount,
            "Mode A and Mode B2 must produce the same TotalPrice for the same adult/child basket â€” anti-fork guard");
        modeBPrice.Currency.Should().Be(modeAPrice.Value.Currency);
        modeBPrice.Amount.Should().Be(37m, "sanity: 2 Ã— $15 + 1 Ã— $7 = $37");
    }

    [Fact]
    public void RsvpToEvent_FreeBMode_StillProducesZeroTotalPrice_AndConfirmedStatus()
    {
        // Regression â€” free path must remain unchanged after gate removal.
        var title = EventTitle.Create("Free B-mode regression").Value;
        var description = EventDescription.Create("Phase 7E.3b regression").Value;
        var @event = Event.Create(title, description, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), Guid.NewGuid(), 100).Value;
        @event.SetAsFreeEvent().IsSuccess.Should().BeTrue();
        @event.Publish().IsSuccess.Should().BeTrue();
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value;

        var result = @event.RegisterWithHeadCount(Guid.NewGuid(), "Free Lead", head, CreateContact());

        result.IsSuccess.Should().BeTrue();
        var registration = @event.Registrations.Single();
        registration.TotalPrice!.Amount.Should().Be(0m);
        registration.Status.Should().Be(RegistrationStatus.Confirmed,
            "free events confirm immediately (no Stripe checkout)");
    }
}
