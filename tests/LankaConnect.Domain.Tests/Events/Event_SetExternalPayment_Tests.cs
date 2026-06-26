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
/// Phase 8X.3 — domain contract for <c>Event.SetExternalPayment</c>.
/// Architect-locked rules:
/// - Free / OnPlatformPaid → ExternalPaid: requires no active regs, no AssignedSeating,
///   non-null pricing, non-null ExternalRegistration VO.
/// - Sets PaymentMode = ExternalPaid, RegistrationMode = NoRegistration, IsFreeEvent = false,
///   ExternalRegistration set.
/// - Idempotent on same-state set.
/// </summary>
public class Event_SetExternalPayment_Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Builders
    // ─────────────────────────────────────────────────────────────────────────

    private static Event CreateFreshEvent()
    {
        var title = EventTitle.Create("ExternalPayment test event").Value;
        var description = EventDescription.Create("Phase 8X.3 domain test").Value;
        return Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: 100).Value;
    }

    private static ExternalRegistration ExternalReg(
        string url = "https://eventbrite.com/e/test-12345",
        string? instructions = "Pay $25 at the door.",
        string? vendor = "Eventbrite") =>
        ExternalRegistration.Create(url, instructions, vendor).Value;

    private static TicketPricing DualPricing(decimal adult = 25m, decimal child = 10m) =>
        TicketPricing.CreateDualPrice(
            Money.Create(adult, Currency.USD).Value,
            Money.Create(child, Currency.USD).Value,
            childAgeLimit: 12).Value;

    // ─────────────────────────────────────────────────────────────────────────
    //  Happy path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetExternalPayment_WithValidVoAndPricing_SetsPaymentModeAndRegMode()
    {
        var ev = CreateFreshEvent();

        var result = ev.SetExternalPayment(ExternalReg(), DualPricing());

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
        // Phase 8X.11 — registration mode flips to External (was: NoRegistration).
        ev.RegistrationMode.Should().Be(RegistrationMode.External);
        ev.IsFreeEvent.Should().BeFalse();
        ev.ExternalRegistration.Should().NotBeNull();
        ev.ExternalRegistration!.Url.Should().Be("https://eventbrite.com/e/test-12345");
        ev.ExternalRegistration.VendorName.Should().Be("Eventbrite");
    }

    [Fact]
    public void SetExternalPayment_OnOnPlatformPaidEventWithNoRegs_Succeeds()
    {
        var ev = CreateFreshEvent();
        ev.SetDualPricing(DualPricing()).IsSuccess.Should().BeTrue();
        // SetDualPricing sets IsFreeEvent based on pricing; PaymentMode still Free.
        // Calling SetExternalPayment is the path that flips PaymentMode.

        var result = ev.SetExternalPayment(ExternalReg(), DualPricing());

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
    }

    [Fact]
    public void SetExternalPayment_Idempotent_SameStateTwice_Succeeds()
    {
        var ev = CreateFreshEvent();
        ev.SetExternalPayment(ExternalReg(), DualPricing()).IsSuccess.Should().BeTrue();

        // Second call with the same shape should also succeed (no active-regs flap, no exception).
        var result = ev.SetExternalPayment(ExternalReg(), DualPricing());

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Failure paths
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetExternalPayment_WithNullExternalRegistration_Succeeds_StoresNullVo()
    {
        // Phase 8X.11 — externalReg may be null when the organiser supplied no URL +
        // no instructions + no vendor. Domain stores it as null; public detail page
        // renders the friendly "Contact organiser for registration details" card.
        var ev = CreateFreshEvent();
        var result = ev.SetExternalPayment(externalReg: null, DualPricing());

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
        ev.RegistrationMode.Should().Be(RegistrationMode.External);
        ev.ExternalRegistration.Should().BeNull();
    }

    [Fact]
    public void SetExternalPayment_WithNullPricing_Succeeds_NoOnPlatformPricing()
    {
        // Phase 8X.12 — pricing is now optional. Organiser may publish an ExternalPaid
        // event with no on-platform price (the price lives at the external provider).
        // Public detail page renders "See external site or reach out organizer for pricing".
        var ev = CreateFreshEvent();
        var result = ev.SetExternalPayment(ExternalReg(), pricing: null);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
        ev.RegistrationMode.Should().Be(RegistrationMode.External);
        ev.Pricing.Should().BeNull();
        ev.TicketPrice.Should().BeNull();
    }

    [Fact]
    public void SetExternalPayment_WithNullPricing_AndExistingLegacyPricing_ClearsLegacyPricing()
    {
        // Phase 8X.12 — explicit null pricing on a transition INTO ExternalPaid clears
        // any stale legacy pricing (organiser intent: "no on-platform price").
        var ev = CreateFreshEvent();
        ev.SetDualPricing(DualPricing()).IsSuccess.Should().BeTrue();
        ev.Pricing.Should().NotBeNull();

        var result = ev.SetExternalPayment(ExternalReg(), pricing: null);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.Pricing.Should().BeNull();
        ev.TicketPrice.Should().BeNull();
    }

    [Fact]
    public void SetExternalPayment_WithBothNull_Succeeds_FriendlyEmptyState()
    {
        // Phase 8X.12 — null externalReg + null pricing is the most permissive ExternalPaid
        // state. Public detail page nudges the user to contact the organiser.
        var ev = CreateFreshEvent();
        var result = ev.SetExternalPayment(externalReg: null, pricing: null);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
        ev.RegistrationMode.Should().Be(RegistrationMode.External);
        ev.ExternalRegistration.Should().BeNull();
        ev.Pricing.Should().BeNull();
    }

    [Fact]
    public void SetExternalPayment_WithAssignedSeating_Fails()
    {
        // EnableAssignedSeating requires TicketingMode=Tiered + at least one tier configured
        // + RegistrationMode=DetailedAttendees (Slice S1.5 invariant). Build that event shape
        // first, then verify SetExternalPayment rejects it.
        var ev = CreateFreshEvent();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();
        ev.AddTicketTier("VIP", "VIP tier",
            Money.Create(50m, Currency.USD).Value, null, null,
            capacity: 20, maxPerUser: 10, sortOrder: 1).IsSuccess.Should().BeTrue();
        ev.EnableAssignedSeating(Guid.NewGuid()).IsSuccess.Should().BeTrue();

        var result = ev.SetExternalPayment(ExternalReg(), DualPricing());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Match(e => e.Contains("assigned seating") || e.Contains("AssignedSeating"));
    }
}
