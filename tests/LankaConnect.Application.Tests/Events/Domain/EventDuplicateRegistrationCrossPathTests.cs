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
/// Tests for cross-path duplicate registration prevention.
///
/// Root Cause: The system has two registration paths (authenticated RSVP + anonymous registration)
/// with no cross-path duplicate detection. An authenticated user could register, then the same
/// email could register via the anonymous path (or vice versa) because:
/// 1. Authenticated path checks by UserId only, not email
/// 2. Anonymous path checks by email but only where UserId == null
/// 3. No database unique constraint exists on (EventId, email)
///
/// These tests verify that the domain entity prevents duplicate registrations across both paths.
/// </summary>
public class EventDuplicateRegistrationCrossPathTests
{
    private readonly DateTime _startDate = DateTime.UtcNow.AddDays(30);
    private readonly DateTime _endDate = DateTime.UtcNow.AddDays(30).AddHours(3);
    private readonly Guid _organizerId = Guid.NewGuid();

    #region Helper Methods

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
        @event.Publish();
        @event.Status.Should().Be(EventStatus.Published);
        return @event;
    }

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

    #endregion

    #region Cross-Path Duplicate Detection: Authenticated then Anonymous

    [Fact]
    public void RegisterWithAttendees_AuthenticatedUserRegistered_AnonymousSameEmail_ShouldFail()
    {
        // Arrange: Authenticated user registers with email
        var @event = CreateFreeEvent();
        var userId = Guid.NewGuid();
        var email = "shared@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee("Auth User") };
        var contact = CreateContact(email);

        var authResult = @event.RegisterWithAttendees(userId, attendees, contact);
        authResult.IsSuccess.Should().BeTrue("Authenticated registration should succeed");

        @event.ClearDomainEvents();

        // Act: Anonymous user tries to register with same email
        var anonAttendees = new List<AttendeeDetails> { CreateAttendee("Anon User") };
        var anonContact = CreateContact(email);
        var anonResult = @event.RegisterWithAttendees(null, anonAttendees, anonContact);

        // Assert: Should fail - email already registered via authenticated path
        anonResult.IsFailure.Should().BeTrue(
            "Anonymous registration with same email as existing authenticated registration should be blocked");
        anonResult.Error.Should().Contain("already registered");
    }

    [Fact]
    public void RegisterWithAttendees_AuthenticatedUserRegistered_AnonymousSameEmailCaseInsensitive_ShouldFail()
    {
        // Arrange: Authenticated user registers with lowercase email
        var @event = CreateFreeEvent();
        var userId = Guid.NewGuid();
        var attendees = new List<AttendeeDetails> { CreateAttendee("Auth User") };
        var contact = CreateContact("shared@example.com");

        var authResult = @event.RegisterWithAttendees(userId, attendees, contact);
        authResult.IsSuccess.Should().BeTrue();

        @event.ClearDomainEvents();

        // Act: Anonymous user tries with uppercase email
        var anonAttendees = new List<AttendeeDetails> { CreateAttendee("Anon User") };
        var anonContact = CreateContact("SHARED@EXAMPLE.COM");
        var anonResult = @event.RegisterWithAttendees(null, anonAttendees, anonContact);

        // Assert: Should fail - case-insensitive match
        anonResult.IsFailure.Should().BeTrue(
            "Email duplicate detection should be case-insensitive");
        anonResult.Error.Should().Contain("already registered");
    }

    #endregion

    #region Cross-Path Duplicate Detection: Anonymous then Authenticated

    [Fact]
    public void RegisterWithAttendees_AnonymousRegistered_AuthenticatedSameEmail_ShouldFail()
    {
        // Arrange: Anonymous user registers with email
        var @event = CreateFreeEvent();
        var email = "shared@example.com";
        var anonAttendees = new List<AttendeeDetails> { CreateAttendee("Anon User") };
        var anonContact = CreateContact(email);

        var anonResult = @event.RegisterWithAttendees(null, anonAttendees, anonContact);
        anonResult.IsSuccess.Should().BeTrue("Anonymous registration should succeed");

        @event.ClearDomainEvents();

        // Act: Authenticated user tries to register with same email
        var userId = Guid.NewGuid();
        var authAttendees = new List<AttendeeDetails> { CreateAttendee("Auth User") };
        var authContact = CreateContact(email);
        var authResult = @event.RegisterWithAttendees(userId, authAttendees, authContact);

        // Assert: Should fail - email already registered via anonymous path
        authResult.IsFailure.Should().BeTrue(
            "Authenticated registration with same email as existing anonymous registration should be blocked");
        authResult.Error.Should().Contain("already registered");
    }

    #endregion

    #region Cross-Path: Terminal Statuses Should Allow Re-registration

    [Fact]
    public void RegisterWithAttendees_AuthenticatedCancelled_AnonymousSameEmail_ShouldSucceed()
    {
        // Arrange: Authenticated user registered then cancelled
        var @event = CreateFreeEvent();
        var userId = Guid.NewGuid();
        var email = "shared@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee("Auth User") };
        var contact = CreateContact(email);

        var authResult = @event.RegisterWithAttendees(userId, attendees, contact);
        authResult.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.UserId == userId);
        registration.Cancel();
        registration.Status.Should().Be(RegistrationStatus.Cancelled);

        @event.ClearDomainEvents();

        // Act: Anonymous user registers with same email
        var anonAttendees = new List<AttendeeDetails> { CreateAttendee("Anon User") };
        var anonContact = CreateContact(email);
        var anonResult = @event.RegisterWithAttendees(null, anonAttendees, anonContact);

        // Assert: Should succeed - previous registration was cancelled
        anonResult.IsSuccess.Should().BeTrue(
            "Should allow registration when previous registration with same email was cancelled");
    }

    #endregion

    #region IsUserRegistered Should Check All Active Statuses

    [Fact]
    public void IsUserRegistered_CheckedInUser_ShouldReturnTrue()
    {
        // Arrange: User registered for paid event, confirmed, then checked in
        var @event = CreatePaidEvent();
        var userId = Guid.NewGuid();
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = CreateContact();

        var result = @event.RegisterWithAttendees(userId, attendees, contact);
        result.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.UserId == userId);
        registration.SetStripeCheckoutSession("cs_test");
        registration.CompletePayment("pi_test");
        registration.CheckIn();
        registration.Status.Should().Be(RegistrationStatus.CheckedIn);

        // Act & Assert
        @event.IsUserRegistered(userId).Should().BeTrue(
            "IsUserRegistered should return true for CheckedIn status, not just Confirmed");
    }

    [Fact]
    public void IsUserRegistered_CancelledUser_ShouldReturnFalse()
    {
        // Arrange: User registered then cancelled
        var @event = CreateFreeEvent();
        var userId = Guid.NewGuid();
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = CreateContact();

        var result = @event.RegisterWithAttendees(userId, attendees, contact);
        result.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.UserId == userId);
        registration.Cancel();

        // Act & Assert
        @event.IsUserRegistered(userId).Should().BeFalse(
            "IsUserRegistered should return false for Cancelled status");
    }

    [Fact]
    public void IsUserRegistered_AbandonedUser_ShouldReturnFalse()
    {
        // Arrange: User registered for paid event, abandoned checkout
        var @event = CreatePaidEvent();
        var userId = Guid.NewGuid();
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = CreateContact();

        var result = @event.RegisterWithAttendees(userId, attendees, contact);
        result.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.UserId == userId);
        registration.MarkAbandoned();
        registration.Status.Should().Be(RegistrationStatus.Abandoned);

        // Act & Assert
        @event.IsUserRegistered(userId).Should().BeFalse(
            "IsUserRegistered should return false for Abandoned status");
    }

    #endregion

    #region Legacy RegisterAnonymous Duplicate Detection

    [Fact]
    public void RegisterAnonymous_WhenEmailAlreadyRegisteredAuthenticated_ShouldFail()
    {
        // Arrange: Authenticated user registered with email
        var @event = CreateFreeEvent();
        var userId = Guid.NewGuid();
        var email = "shared@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee("Auth User") };
        var contact = CreateContact(email);

        var authResult = @event.RegisterWithAttendees(userId, attendees, contact);
        authResult.IsSuccess.Should().BeTrue();

        @event.ClearDomainEvents();

        // Act: Legacy anonymous registration with same email
        var attendeeInfoResult = AttendeeInfo.Create("Anon User", 30, "123 St", email, "555-9999");
        attendeeInfoResult.IsSuccess.Should().BeTrue();

        var anonResult = @event.RegisterAnonymous(attendeeInfoResult.Value, 1);

        // Assert: Should fail
        anonResult.IsFailure.Should().BeTrue(
            "Legacy RegisterAnonymous should detect duplicate email from authenticated registration");
        anonResult.Error.Should().Contain("already registered");
    }

    [Fact]
    public void RegisterAnonymous_WhenEmailAlreadyRegisteredAnonymous_ShouldFail()
    {
        // Arrange: Anonymous user registered with same email (via multi-attendee path)
        var @event = CreateFreeEvent();
        var email = "anon@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee("First Anon") };
        var contact = CreateContact(email);

        var firstResult = @event.RegisterWithAttendees(null, attendees, contact);
        firstResult.IsSuccess.Should().BeTrue();

        @event.ClearDomainEvents();

        // Act: Legacy anonymous registration with same email
        var attendeeInfoResult = AttendeeInfo.Create("Second Anon", 25, "456 St", email, "555-8888");
        attendeeInfoResult.IsSuccess.Should().BeTrue();

        var secondResult = @event.RegisterAnonymous(attendeeInfoResult.Value, 1);

        // Assert: Should fail
        secondResult.IsFailure.Should().BeTrue(
            "Legacy RegisterAnonymous should detect duplicate email from previous anonymous registration");
        secondResult.Error.Should().Contain("already registered");
    }

    [Fact]
    public void RegisterAnonymous_WhenPreviousRegistrationCancelled_ShouldSucceed()
    {
        // Arrange: Anonymous user registered then cancelled
        var @event = CreateFreeEvent();
        var email = "anon@example.com";
        var attendees = new List<AttendeeDetails> { CreateAttendee("First Anon") };
        var contact = CreateContact(email);

        var firstResult = @event.RegisterWithAttendees(null, attendees, contact);
        firstResult.IsSuccess.Should().BeTrue();

        var registration = @event.Registrations.First(r => r.Contact?.Email == email);
        registration.Cancel();

        @event.ClearDomainEvents();

        // Act: Same email registers again via legacy path
        var attendeeInfoResult = AttendeeInfo.Create("Second Anon", 25, "456 St", email, "555-8888");
        attendeeInfoResult.IsSuccess.Should().BeTrue();

        var secondResult = @event.RegisterAnonymous(attendeeInfoResult.Value, 1);

        // Assert: Should succeed - previous registration was cancelled
        secondResult.IsSuccess.Should().BeTrue(
            "Should allow re-registration when previous registration with same email was cancelled");
    }

    #endregion
}
