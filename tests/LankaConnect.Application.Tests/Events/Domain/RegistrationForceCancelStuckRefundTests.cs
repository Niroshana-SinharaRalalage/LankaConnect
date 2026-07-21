using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.SharedKernel.Money;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E follow-up: Registration.ForceCancelStuckRefund() unit tests.
///
/// The method exists because RefundRequested rows consume capacity until Stripe confirms
/// the refund â€” but old events whose Stripe webhook never fired leave rows permanently
/// stuck, blocking Event.SetRegistrationMode and cluttering the dashboard. Only the
/// organiser (verified at the application layer) can invoke this; the domain method itself
/// just enforces the status-transition invariant: only RefundRequested â†’ Cancelled.
/// </summary>
public class RegistrationForceCancelStuckRefundTests
{
    private static AttendeeDetails CreateAttendee() =>
        AttendeeDetails.Create("Test User", AgeCategory.Adult).Value;

    private static RegistrationContact CreateContact() =>
        RegistrationContact.Create("test@example.com", "555-1234", null).Value;

    /// <summary>
    /// Mirrors RegistrationRefundWorkflowTests' helper: paid event, completed payment,
    /// Stripe IDs set so RequestRefund's preconditions are satisfied.
    /// </summary>
    private static Registration CreatePaidConfirmedRegistration()
    {
        var price = new Money(100m, Currency.USD);
        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(),
            new List<AttendeeDetails> { CreateAttendee() },
            CreateContact(),
            price,
            isPaidEvent: true).Value;

        registration.SetStripeCheckoutSession("cs_test_force_cancel");
        registration.CompletePayment("pi_test_force_cancel");
        registration.ClearDomainEvents();
        return registration;
    }

    [Fact]
    public void ForceCancelStuckRefund_TransitionsRefundRequestedToCancelled()
    {
        var registration = CreatePaidConfirmedRegistration();
        registration.RequestRefund().IsSuccess.Should().BeTrue("RequestRefund preconditions met");
        registration.Status.Should().Be(RegistrationStatus.RefundRequested,
            "sanity check â€” RequestRefund should land in RefundRequested");

        var result = registration.ForceCancelStuckRefund();

        result.IsSuccess.Should().BeTrue(
            $"a RefundRequested row is the exact case this method exists for. " +
            $"Errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        registration.Status.Should().Be(RegistrationStatus.Cancelled);
    }

    [Fact]
    public void ForceCancelStuckRefund_Fails_WhenAlreadyCancelled()
    {
        var registration = CreatePaidConfirmedRegistration();
        registration.Cancel();

        var result = registration.ForceCancelStuckRefund();

        result.IsSuccess.Should().BeFalse(
            "Force-cancel must NOT be a backdoor for cancelling Confirmed/Cancelled rows; only RefundRequested.");
        result.Errors.Should().Contain(e => e.Contains("RefundRequested"));
    }

    [Fact]
    public void ForceCancelStuckRefund_Fails_WhenStillConfirmed()
    {
        var registration = CreatePaidConfirmedRegistration();

        var result = registration.ForceCancelStuckRefund();

        result.IsSuccess.Should().BeFalse(
            "A Confirmed row should be cancelled via the proper Cancel() path, not this method.");
        result.Errors.Should().Contain(e => e.Contains("RefundRequested"));
        registration.Status.Should().Be(RegistrationStatus.Confirmed,
            "the failed call must not mutate state");
    }
}
