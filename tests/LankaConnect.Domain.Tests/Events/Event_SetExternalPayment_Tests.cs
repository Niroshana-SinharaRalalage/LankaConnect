using FluentAssertions;
using LankaConnect.Domain.Events;
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
        ev.RegistrationMode.Should().Be(RegistrationMode.NoRegistration);
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
    public void SetExternalPayment_WithNullExternalRegistration_Fails()
    {
        var ev = CreateFreshEvent();
        var result = ev.SetExternalPayment(externalReg: null!, DualPricing());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("ExternalRegistration");
    }

    [Fact]
    public void SetExternalPayment_WithNullPricing_Fails()
    {
        var ev = CreateFreshEvent();
        var result = ev.SetExternalPayment(ExternalReg(), pricing: null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pricing");
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
