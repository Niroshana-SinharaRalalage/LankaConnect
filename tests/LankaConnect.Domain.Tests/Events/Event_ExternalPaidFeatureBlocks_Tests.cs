using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8X.3.5 — domain rules for features that don't compose with ExternalPaid.
/// Architect-locked compatibility matrix (master TODO 8X):
/// - Add-ons: BLOCKED (require internal Registration row to attach to).
/// - Waitlist: BLOCKED (requires internal promotion path off the list).
/// - Check-in QR: BLOCKED (no internal Registration → naturally unreachable).
/// - Ticket tiers: ALLOWED for display, but Reserve() naturally unreachable
///   (RegisterWith* paths blocked per Slice 8X.3).
/// </summary>
public class Event_ExternalPaidFeatureBlocks_Tests
{
    private static Event CreateFreshEvent()
    {
        var title = EventTitle.Create("ExternalPaid feature-block test").Value;
        var description = EventDescription.Create("Phase 8X.3.5 domain test").Value;
        return Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: 100).Value;
    }

    private static TicketPricing DualPricing(decimal adult = 25m) =>
        TicketPricing.CreateDualPrice(
            Money.Create(adult, Currency.USD).Value,
            Money.Create(10m, Currency.USD).Value,
            childAgeLimit: 12).Value;

    private static ExternalRegistration ExternalReg() =>
        ExternalRegistration.Create("https://eventbrite.com/e/test-12345", "Pay at door", "Eventbrite").Value;

    private static Event CreateExternalPaidEvent()
    {
        var ev = CreateFreshEvent();
        ev.SetExternalPayment(ExternalReg(), DualPricing()).IsSuccess.Should().BeTrue();
        return ev;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Add-ons
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Event_AddOnsBlockedForExternalPaid()
    {
        var ev = CreateExternalPaidEvent();
        var addOnConfig = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: null);

        var result = ev.SetAddOnConfiguration(addOnConfig.Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("ExternalPaid");
    }

    [Fact]
    public void Event_AddOnsDisabledIsAllowedForExternalPaid()
    {
        // Operator may have add-ons set to disabled (default). Allowing the disabled
        // shape is fine — it's a no-op surface.
        var ev = CreateExternalPaidEvent();
        var addOnConfig = AddOnConfiguration.Disabled();

        var result = ev.SetAddOnConfiguration(addOnConfig);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Event_SetAsExternalPaid_FailsIfAddOnsAlreadyConfigured()
    {
        var ev = CreateFreshEvent();
        var addOnConfig = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: null).Value;
        ev.SetAddOnConfiguration(addOnConfig).IsSuccess.Should().BeTrue();

        var result = ev.SetExternalPayment(ExternalReg(), DualPricing());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("add-ons");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Waitlist
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Event_WaitlistBlockedForExternalPaid()
    {
        var ev = CreateExternalPaidEvent();

        var result = ev.AddToWaitingList(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Waitlist");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Ticket tiers — defining is allowed, but Reserve is unreachable via the
    //  RegisterWith* paths (already blocked in Slice 8X.3).
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Event_TicketTiersDisplayOnly_ForExternalPaid()
    {
        // Setup tiered ticketing BEFORE going ExternalPaid so the tiers exist.
        var ev = CreateFreshEvent();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();
        var tier = ev.AddTicketTier(
            "VIP", "VIP tier",
            Money.Create(50m, Currency.USD).Value, null, null,
            capacity: 20, maxPerUser: 10, sortOrder: 1).Value;

        // Now flip to ExternalPaid — ticket tiers persist as display data.
        ev.SetExternalPayment(ExternalReg(), DualPricing()).IsSuccess.Should().BeTrue();

        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
        ev.TicketTiers.Should().HaveCount(1);
        ev.TicketTiers.First().Name.Should().Be("VIP");

        // Reserve path is unreachable: RegisterWithAttendees / RegisterWithHeadCount
        // both reject ExternalPaid events at the domain boundary (Slice 8X.3).
        // No additional Reserve guard needed at the tier level — defence-in-depth
        // sits at the registration entry point, not on the tier itself.
    }
}
