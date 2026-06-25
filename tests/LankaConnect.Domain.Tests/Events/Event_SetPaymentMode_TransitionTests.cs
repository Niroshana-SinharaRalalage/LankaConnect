using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8X.3 — payment-mode transition contract for <c>Event.SetPaymentMode</c>.
/// Architect-locked transition table from MASTER_TODO_PHASE_8X_EXTERNAL_PAYMENT.md:
/// - Free → OnPlatformPaid: requires pricing.
/// - OnPlatformPaid → Free: requires no active regs; pricing cleared.
/// - ExternalPaid → OnPlatformPaid: requires no active regs; ExternalRegistration cleared;
///   RegistrationMode auto-resets to DetailedAttendees.
/// - ExternalPaid → Free: requires no active regs; ExternalRegistration cleared; pricing cleared;
///   RegistrationMode auto-resets to DetailedAttendees.
/// - * → ExternalPaid: NOT supported via this method (use SetExternalPayment).
/// - Idempotent on same-mode set.
/// </summary>
public class Event_SetPaymentMode_TransitionTests
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Builders
    // ─────────────────────────────────────────────────────────────────────────

    private static Event CreateFreshEvent()
    {
        var title = EventTitle.Create("PaymentMode transition test").Value;
        var description = EventDescription.Create("Phase 8X.3 domain test").Value;
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

    // ─────────────────────────────────────────────────────────────────────────
    //  Idempotency
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetPaymentMode_SameModeTwice_IsIdempotent()
    {
        var ev = CreateFreshEvent();
        ev.PaymentMode.Should().Be(EventPaymentMode.Free);

        var result = ev.SetPaymentMode(EventPaymentMode.Free);

        result.IsSuccess.Should().BeTrue();
        ev.PaymentMode.Should().Be(EventPaymentMode.Free);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Free → OnPlatformPaid
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetPaymentMode_FreeToOnPlatformPaid_WithPricing_Succeeds()
    {
        var ev = CreateFreshEvent();
        ev.SetDualPricing(DualPricing()).IsSuccess.Should().BeTrue();

        var result = ev.SetPaymentMode(EventPaymentMode.OnPlatformPaid);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.OnPlatformPaid);
        ev.IsFreeEvent.Should().BeFalse();
    }

    [Fact]
    public void SetPaymentMode_FreeToOnPlatformPaid_WithoutPricing_Fails()
    {
        var ev = CreateFreshEvent();
        // No pricing configured.

        var result = ev.SetPaymentMode(EventPaymentMode.OnPlatformPaid);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("pricing");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  → ExternalPaid is NOT supported via SetPaymentMode
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetPaymentMode_ToExternalPaid_RejectedDirectsToSetExternalPayment()
    {
        var ev = CreateFreshEvent();

        var result = ev.SetPaymentMode(EventPaymentMode.ExternalPaid);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SetExternalPayment");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExternalPaid → OnPlatformPaid
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetPaymentMode_ExternalPaidToOnPlatformPaid_NoRegs_Succeeds_RegistrationModeResetsToDetailedAttendees_ExternalRegistrationCleared()
    {
        var ev = CreateFreshEvent();
        ev.SetExternalPayment(ExternalReg(), DualPricing()).IsSuccess.Should().BeTrue();
        ev.PaymentMode.Should().Be(EventPaymentMode.ExternalPaid);
        // Phase 8X.11 — SetExternalPayment now sets RegistrationMode = External (was NoRegistration).
        ev.RegistrationMode.Should().Be(RegistrationMode.External);
        ev.ExternalRegistration.Should().NotBeNull();

        var result = ev.SetPaymentMode(EventPaymentMode.OnPlatformPaid);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.OnPlatformPaid);
        ev.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
        ev.ExternalRegistration.Should().BeNull();
        ev.IsFreeEvent.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExternalPaid → Free
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetPaymentMode_ExternalPaidToFree_NoRegs_Succeeds_RegistrationModeResetsToDetailedAttendees_PricingCleared()
    {
        var ev = CreateFreshEvent();
        ev.SetExternalPayment(ExternalReg(), DualPricing()).IsSuccess.Should().BeTrue();

        var result = ev.SetPaymentMode(EventPaymentMode.Free);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.Free);
        ev.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
        ev.ExternalRegistration.Should().BeNull();
        ev.IsFreeEvent.Should().BeTrue();
        ev.Pricing.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  OnPlatformPaid → Free
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetPaymentMode_OnPlatformPaidToFree_NoRegs_Succeeds_PricingCleared()
    {
        var ev = CreateFreshEvent();
        ev.SetDualPricing(DualPricing()).IsSuccess.Should().BeTrue();
        ev.SetPaymentMode(EventPaymentMode.OnPlatformPaid).IsSuccess.Should().BeTrue();
        ev.PaymentMode.Should().Be(EventPaymentMode.OnPlatformPaid);

        var result = ev.SetPaymentMode(EventPaymentMode.Free);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        ev.PaymentMode.Should().Be(EventPaymentMode.Free);
        ev.IsFreeEvent.Should().BeTrue();
        ev.Pricing.Should().BeNull();
    }
}
