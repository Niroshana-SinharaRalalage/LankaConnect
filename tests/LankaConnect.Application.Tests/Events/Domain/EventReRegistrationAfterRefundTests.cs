using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 6A.93: Unit tests for re-registration after refund scenarios
/// Verifies that users can re-register for events after their registration is RefundRequested or Refunded
///
/// Root Cause: Event.RegisterWithAttendees() was not excluding RefundRequested status,
/// causing "already registered" error for users who cancelled and are waiting for refund.
/// </summary>
public class EventReRegistrationAfterRefundTests
{
    private readonly DateTime _startDate = DateTime.UtcNow.AddDays(30);
    private readonly DateTime _endDate = DateTime.UtcNow.AddDays(30).AddHours(3);
    private readonly Guid _organizerId = Guid.NewGuid();

    #region Helper Methods

    private Event CreatePaidEvent(int capacity = 100, decimal price = 50m)
    {
        var ticketPrice = Money.Create(price, Currency.USD).Value;
        var eventResult = Event.Create(
            EventTitle.Create("Paid Community Event").Value,
            EventDescription.Create("A paid event for the community").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: capacity,
            ticketPrice: ticketPrice);

        eventResult.IsSuccess.Should().BeTrue();
        var @event = eventResult.Value;

        // Event must be Published before registration is allowed
        @event.Publish();
        @event.Status.Should().Be(EventStatus.Published);

        return @event;
    }

    private Event CreateFreeEvent(int capacity = 100)
    {
        var ticketPrice = Money.Create(0, Currency.USD).Value;
        var eventResult = Event.Create(
            EventTitle.Create("Free Community Event").Value,
            EventDescription.Create("A free event for the community").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: capacity,
            ticketPrice: ticketPrice);

        eventResult.IsSuccess.Should().BeTrue();
        var @event = eventResult.Value;

        // Event must be Published before registration is allowed
        @event.Publish();
        @event.Status.Should().Be(EventStatus.Published);

        return @event;
    }

    private static AttendeeDetails CreateAttendee(string name = "John Doe", AgeCategory ageCategory = AgeCategory.Adult)
    {
        return AttendeeDetails.Create(name, ageCategory).Value;
    }

    private static RegistrationContact CreateContact(string email = "test@example.com", string phone = "555-1234")
    {
        return RegistrationContact.Create(email, phone, null).Value;
    }

    /// <summary>
    /// Register a user and complete payment to get Confirmed status
    /// </summary>
    private Registration RegisterAndConfirmPayment(Event @event, Guid userId, string email = "test@example.com")
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = RegistrationContact.Create(email, "555-1234", null).Value;

        var result = @event.RegisterWithAttendees(userId, attendees, contact);
        result.IsSuccess.Should().BeTrue("Initial registration should succeed");

        var registration = @event.Registrations.First(r => r.UserId == userId);
        registration.SetStripeCheckoutSession("cs_test_123");
        registration.CompletePayment("pi_test_123");

        registration.Status.Should().Be(RegistrationStatus.Confirmed);
        return registration;
    }

    #endregion

    #region Re-registration After RefundRequested (THE BUG FIX)

    [Fact]
    public void RegisterWithAttendees_UserWithRefundRequestedStatus_ShouldSucceed()
    {
        // Arrange: User registered, confirmed, then requested refund
        var @event = CreatePaidEvent();
        var userId = Guid.NewGuid();
        var registration = RegisterAndConfirmPayment(@event, userId);

        // Request refund - status becomes RefundRequested
        var refundResult = registration.RequestRefund();
        refundResult.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);

        // Clear events for clean test
        @event.ClearDomainEvents();

        // Act: User tries to register again while RefundRequested
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = CreateContact("newemail@example.com");
        var reRegisterResult = @event.RegisterWithAttendees(userId, newAttendees, newContact);

        // Assert: Should succeed - user can re-register while refund is pending
        reRegisterResult.IsSuccess.Should().BeTrue(
            "User should be able to re-register when their previous registration is in RefundRequested status");

        // Verify new registration was created
        var newRegistration = @event.Registrations
            .Where(r => r.UserId == userId && r.Status != RegistrationStatus.RefundRequested)
            .FirstOrDefault();
        newRegistration.Should().NotBeNull("A new registration should be created");
    }

    [Fact]
    public void RegisterWithAttendees_UserWithRefundedStatus_ShouldSucceed()
    {
        // Arrange: User registered, confirmed, requested refund, and refund completed
        var @event = CreatePaidEvent();
        var userId = Guid.NewGuid();
        var registration = RegisterAndConfirmPayment(@event, userId);

        // Request and complete refund
        registration.RequestRefund();
        registration.CompleteRefund("re_test_123");
        registration.Status.Should().Be(RegistrationStatus.Refunded);

        // Clear events for clean test
        @event.ClearDomainEvents();

        // Act: User tries to register again after refund completed
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = CreateContact("newemail@example.com");
        var reRegisterResult = @event.RegisterWithAttendees(userId, newAttendees, newContact);

        // Assert: Should succeed
        reRegisterResult.IsSuccess.Should().BeTrue(
            "User should be able to re-register after their previous registration was refunded");
    }

    [Fact]
    public void RegisterWithAttendees_UserWithCancelledStatus_ShouldSucceed()
    {
        // Arrange: User registered for free event and cancelled
        var @event = CreateFreeEvent();
        var userId = Guid.NewGuid();
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = CreateContact();

        var result = @event.RegisterWithAttendees(userId, attendees, contact);
        result.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.UserId == userId);
        registration.Cancel();
        registration.Status.Should().Be(RegistrationStatus.Cancelled);

        // Clear events for clean test
        @event.ClearDomainEvents();

        // Act: User tries to register again after cancellation
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = CreateContact("newemail@example.com");
        var reRegisterResult = @event.RegisterWithAttendees(userId, newAttendees, newContact);

        // Assert: Should succeed
        reRegisterResult.IsSuccess.Should().BeTrue(
            "User should be able to re-register after their previous registration was cancelled");
    }

    #endregion

    #region Blocked Re-registration (Should Still Fail)

    [Fact]
    public void RegisterWithAttendees_UserWithConfirmedStatus_ShouldFail()
    {
        // Arrange: User has confirmed registration
        var @event = CreatePaidEvent();
        var userId = Guid.NewGuid();
        RegisterAndConfirmPayment(@event, userId);

        // Act: User tries to register again while still confirmed
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = CreateContact("newemail@example.com");
        var reRegisterResult = @event.RegisterWithAttendees(userId, newAttendees, newContact);

        // Assert: Should fail - user is already registered
        reRegisterResult.IsFailure.Should().BeTrue(
            "User should NOT be able to register again when they have an active Confirmed registration");
        reRegisterResult.Error.Should().Contain("already registered");
    }

    // NOTE: Waitlist test removed - Waitlist is a separate domain concern that uses
    // WaitingListEntry, not Registration status. The RegisterWithAttendees method
    // doesn't check waitlist status, it only checks for existing registrations.

    [Fact]
    public void RegisterWithAttendees_UserWithCheckedInStatus_ShouldFail()
    {
        // Arrange: User registered and checked in
        var @event = CreatePaidEvent();
        var userId = Guid.NewGuid();
        var registration = RegisterAndConfirmPayment(@event, userId);

        // Check in
        registration.CheckIn();
        registration.Status.Should().Be(RegistrationStatus.CheckedIn);

        // Act: User tries to register again
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = CreateContact("newemail@example.com");
        var reRegisterResult = @event.RegisterWithAttendees(userId, newAttendees, newContact);

        // Assert: Should fail
        reRegisterResult.IsFailure.Should().BeTrue(
            "User should NOT be able to register when they have already checked in");
    }

    #endregion

    #region Anonymous User Re-registration (By Email)

    [Fact]
    public void RegisterWithAttendees_AnonymousUserWithRefundRequestedEmail_ShouldSucceed()
    {
        // Arrange: Anonymous user registered with email, then requested refund
        var @event = CreatePaidEvent();
        var email = "anonymous@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = RegistrationContact.Create(email, "555-1234", null).Value;

        // Register as anonymous (no userId)
        var result = @event.RegisterWithAttendees(null, attendees, contact);
        result.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.Contact?.Email == email);
        registration.SetStripeCheckoutSession("cs_test_123");
        registration.CompletePayment("pi_test_123");

        // Request refund
        registration.RequestRefund();
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);

        // Clear events
        @event.ClearDomainEvents();

        // Act: Same email tries to register again
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = RegistrationContact.Create(email, "555-9999", null).Value;
        var reRegisterResult = @event.RegisterWithAttendees(null, newAttendees, newContact);

        // Assert: Should succeed
        reRegisterResult.IsSuccess.Should().BeTrue(
            "Anonymous user should be able to re-register with same email when previous registration is RefundRequested");
    }

    [Fact]
    public void RegisterWithAttendees_AnonymousUserWithConfirmedEmail_ShouldFail()
    {
        // Arrange: Anonymous user has confirmed registration
        var @event = CreatePaidEvent();
        var email = "anonymous@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = RegistrationContact.Create(email, "555-1234", null).Value;

        var result = @event.RegisterWithAttendees(null, attendees, contact);
        result.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.Contact?.Email == email);
        registration.SetStripeCheckoutSession("cs_test_123");
        registration.CompletePayment("pi_test_123");
        registration.Status.Should().Be(RegistrationStatus.Confirmed);

        // Act: Same email tries to register again
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe") };
        var newContact = RegistrationContact.Create(email, "555-9999", null).Value;
        var reRegisterResult = @event.RegisterWithAttendees(null, newAttendees, newContact);

        // Assert: Should fail
        reRegisterResult.IsFailure.Should().BeTrue(
            "Anonymous user should NOT be able to register with same email when already confirmed");
        reRegisterResult.Error.Should().Contain("already registered");
    }

    #endregion
}
