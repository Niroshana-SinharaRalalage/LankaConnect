using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.SharedKernel.Money;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8 S8.3 â€” tests that <see cref="SeatReservationsReleasedEvent"/> is
/// raised on every Registration lifecycle transition that releases the
/// "owns the seats" claim: Cancel, ForceCancelStuckRefund, FailPayment,
/// MarkAbandoned, CompleteRefund. The matching event handler in the
/// Application layer calls <c>DeleteByRegistrationIdAsync</c> idempotently.
/// </summary>
public class RegistrationSeatReservationsReleasedTests
{
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private Registration BuildPaidConfirmedRegistration()
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = new Money(50m, Currency.USD);

        var reg = Registration.CreateWithAttendees(
            _eventId, _userId, new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.CompletePayment("pi_test_seat_release").IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();
        return reg;
    }

    private Registration BuildPreliminaryPaidRegistration()
    {
        var alice = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female).Value;
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = new Money(50m, Currency.USD);
        var reg = Registration.CreateWithAttendees(
            _eventId, _userId, new[] { alice }, contact, price, isPaidEvent: true).Value;
        reg.ClearDomainEvents();
        return reg;
    }

    [Fact]
    public void Cancel_RaisesSeatReservationsReleasedEvent_WithReason_registration_cancelled()
    {
        var reg = BuildPaidConfirmedRegistration();

        reg.Cancel();

        var raised = reg.DomainEvents.OfType<SeatReservationsReleasedEvent>().Single();
        raised.EventId.Should().Be(_eventId);
        raised.RegistrationId.Should().Be(reg.Id);
        raised.Reason.Should().Be("registration_cancelled");
    }

    [Fact]
    public void Cancel_AlreadyCancelled_DoesNotRaiseEventAgain()
    {
        var reg = BuildPaidConfirmedRegistration();
        reg.Cancel();
        reg.ClearDomainEvents();

        reg.Cancel();

        reg.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void FailPayment_RaisesSeatReservationsReleasedEvent_WithReason_payment_failed()
    {
        var reg = BuildPreliminaryPaidRegistration();

        var result = reg.FailPayment();

        result.IsSuccess.Should().BeTrue();
        var raised = reg.DomainEvents.OfType<SeatReservationsReleasedEvent>().Single();
        raised.EventId.Should().Be(_eventId);
        raised.RegistrationId.Should().Be(reg.Id);
        raised.Reason.Should().Be("payment_failed");
    }

    [Fact]
    public void MarkAbandoned_RaisesSeatReservationsReleasedEvent_WithReason_checkout_abandoned()
    {
        var reg = BuildPreliminaryPaidRegistration();

        var result = reg.MarkAbandoned();

        result.IsSuccess.Should().BeTrue();
        var raised = reg.DomainEvents.OfType<SeatReservationsReleasedEvent>().Single();
        raised.EventId.Should().Be(_eventId);
        raised.RegistrationId.Should().Be(reg.Id);
        raised.Reason.Should().Be("checkout_abandoned");
    }

    [Fact]
    public void CompleteRefund_RaisesSeatReservationsReleasedEvent_WithReason_refund_completed()
    {
        var reg = BuildPaidConfirmedRegistration();
        reg.RequestRefund().IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();

        var result = reg.CompleteRefund("re_test_seat_release");

        result.IsSuccess.Should().BeTrue();
        var raised = reg.DomainEvents.OfType<SeatReservationsReleasedEvent>().Single();
        raised.EventId.Should().Be(_eventId);
        raised.RegistrationId.Should().Be(reg.Id);
        raised.Reason.Should().Be("refund_completed");
    }

    [Fact]
    public void ForceCancelStuckRefund_RaisesSeatReservationsReleasedEvent_WithReason_force_cancelled()
    {
        var reg = BuildPaidConfirmedRegistration();
        reg.RequestRefund().IsSuccess.Should().BeTrue();
        reg.ClearDomainEvents();

        var result = reg.ForceCancelStuckRefund();

        result.IsSuccess.Should().BeTrue();
        var raised = reg.DomainEvents.OfType<SeatReservationsReleasedEvent>().Single();
        raised.EventId.Should().Be(_eventId);
        raised.RegistrationId.Should().Be(reg.Id);
        raised.Reason.Should().Be("force_cancelled_stuck_refund");
    }
}
