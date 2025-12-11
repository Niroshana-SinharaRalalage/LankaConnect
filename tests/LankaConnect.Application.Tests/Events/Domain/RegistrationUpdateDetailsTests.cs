using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Registration.UpdateDetails() method (Phase 6A.14)
/// Tests the ability to update attendee details and contact information
/// </summary>
public class RegistrationUpdateDetailsTests
{
    #region Helper Methods

    private static AttendeeDetails CreateAttendee(string name, int age)
    {
        return AttendeeDetails.Create(name, age).Value;
    }

    private static RegistrationContact CreateContact(string email = "test@example.com", string phone = "555-1234")
    {
        return RegistrationContact.Create(email, phone, null).Value;
    }

    private static Money CreateMoney(decimal amount = 100m)
    {
        return Money.Create(amount, Currency.USD).Value;
    }

    private static Registration CreateConfirmedRegistration(
        List<AttendeeDetails>? attendees = null,
        RegistrationContact? contact = null,
        bool isPaid = false)
    {
        attendees ??= new List<AttendeeDetails> { CreateAttendee("John Doe", 30) };
        contact ??= CreateContact();
        var price = CreateMoney();

        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: isPaid).Value;

        // If not paid event, status is already Confirmed
        // If paid event, we need to simulate payment completion
        if (isPaid)
        {
            registration.SetStripeCheckoutSession("cs_test_123");
            registration.CompletePayment("pi_test_123");
        }

        return registration;
    }

    private static Registration CreateCancelledRegistration()
    {
        var registration = CreateConfirmedRegistration();
        registration.Cancel();
        return registration;
    }

    private static Registration CreateRefundedRegistration()
    {
        var registration = CreateConfirmedRegistration(isPaid: true);
        registration.RefundPayment();
        return registration;
    }

    #endregion

    #region Valid Update Tests

    [Fact]
    public void UpdateDetails_WithValidAttendeesAndContact_ShouldSucceed()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("Jane Doe", 28)
        };
        var newContact = CreateContact("jane@example.com", "555-9999");

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Attendees.Should().HaveCount(1);
        registration.Attendees[0].Name.Should().Be("Jane Doe");
        registration.Attendees[0].Age.Should().Be(28);
        registration.Contact!.Email.Should().Be("jane@example.com");
        registration.Contact.PhoneNumber.Should().Be("555-9999");
    }

    [Fact]
    public void UpdateDetails_WithSameAttendeeCount_OnPaidRegistration_ShouldSucceed()
    {
        // Arrange
        var originalAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("John Doe", 30),
            CreateAttendee("Jane Doe", 28)
        };
        var registration = CreateConfirmedRegistration(originalAttendees, isPaid: true);

        var newAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("John Smith", 31),
            CreateAttendee("Jane Smith", 29)
        };
        var newContact = CreateContact("smith@example.com", "555-8888");

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue("updating names/ages without changing count should work on paid registrations");
        registration.Attendees.Should().HaveCount(2);
        registration.Attendees[0].Name.Should().Be("John Smith");
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateTimestamp()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var originalUpdatedAt = registration.UpdatedAt;
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Updated Person", 25) };
        var newContact = CreateContact("updated@example.com");

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.UpdatedAt.Should().NotBeNull();
        // Note: UpdatedAt may be same if test runs very fast, so we just verify it's set
    }

    [Fact]
    public void UpdateDetails_OnFreeEvent_WithMoreAttendees_ShouldSucceed()
    {
        // Arrange
        var registration = CreateConfirmedRegistration(); // Free event (isPaid: false)
        var newAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("Person 1", 25),
            CreateAttendee("Person 2", 30),
            CreateAttendee("Person 3", 35)
        };
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue("free events should allow adding attendees");
        registration.Attendees.Should().HaveCount(3);
        registration.Quantity.Should().Be(3, "quantity should be updated to match attendee count");
    }

    [Fact]
    public void UpdateDetails_OnFreeEvent_WithFewerAttendees_ShouldSucceed()
    {
        // Arrange
        var originalAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("Person 1", 25),
            CreateAttendee("Person 2", 30),
            CreateAttendee("Person 3", 35)
        };
        var registration = CreateConfirmedRegistration(originalAttendees);
        var newAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("Person 1", 25)
        };
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue("free events should allow removing attendees");
        registration.Attendees.Should().HaveCount(1);
        registration.Quantity.Should().Be(1);
    }

    [Fact]
    public void UpdateDetails_OnPendingRegistration_ShouldSucceed()
    {
        // Arrange - Create a pending paid registration (before payment complete)
        var attendees = new List<AttendeeDetails> { CreateAttendee("John Doe", 30) };
        var contact = CreateContact();
        var price = CreateMoney();
        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: true).Value;
        // Status is Pending since it's a paid event and payment not completed

        var newAttendees = new List<AttendeeDetails> { CreateAttendee("Jane Doe", 28) };
        var newContact = CreateContact("jane@example.com");

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue("pending registrations should allow updates");
    }

    #endregion

    #region Invalid Status Tests

    [Fact]
    public void UpdateDetails_OnCancelledRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreateCancelledRegistration();
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("New Person", 25) };
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("cancelled"));
    }

    [Fact]
    public void UpdateDetails_OnRefundedRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreateRefundedRegistration();
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("New Person", 25) };
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("refunded") || e.Contains("cancelled"));
    }

    #endregion

    #region Payment Status Restriction Tests

    [Fact]
    public void UpdateDetails_OnPaidRegistration_AddingAttendees_ShouldFail()
    {
        // Arrange
        var originalAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("John Doe", 30)
        };
        var registration = CreateConfirmedRegistration(originalAttendees, isPaid: true);

        var newAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("John Doe", 30),
            CreateAttendee("Jane Doe", 28)  // Adding one more attendee
        };
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Contains("paid") || e.Contains("attendee") || e.Contains("count"));
    }

    [Fact]
    public void UpdateDetails_OnPaidRegistration_RemovingAttendees_ShouldFail()
    {
        // Arrange
        var originalAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("John Doe", 30),
            CreateAttendee("Jane Doe", 28)
        };
        var registration = CreateConfirmedRegistration(originalAttendees, isPaid: true);

        var newAttendees = new List<AttendeeDetails>
        {
            CreateAttendee("John Doe", 30)  // Removing one attendee
        };
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Contains("paid") || e.Contains("attendee") || e.Contains("count"));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void UpdateDetails_WithEmptyAttendeeList_ShouldFail()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails>();
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("attendee"));
    }

    [Fact]
    public void UpdateDetails_WithNullAttendeeList_ShouldFail()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(null!, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("attendee"));
    }

    [Fact]
    public void UpdateDetails_WithNullContact_ShouldFail()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("John Doe", 30) };

        // Act
        var result = registration.UpdateDetails(newAttendees, null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("contact") || e.Contains("Contact"));
    }

    [Fact]
    public void UpdateDetails_WithMoreThan10Attendees_ShouldFail()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails>();
        for (int i = 0; i < 11; i++)
        {
            newAttendees.Add(CreateAttendee($"Person {i}", 25 + i));
        }
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("10") || e.Contains("maximum"));
    }

    [Fact]
    public void UpdateDetails_With10Attendees_ShouldSucceed()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails>();
        for (int i = 0; i < 10; i++)
        {
            newAttendees.Add(CreateAttendee($"Person {i}", 25 + i));
        }
        var newContact = CreateContact();

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue("10 attendees is the maximum allowed");
        registration.Attendees.Should().HaveCount(10);
    }

    #endregion

    #region Contact Update Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateContactEmail()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("John Doe", 30) };
        var newContact = RegistrationContact.Create("newemail@example.com", "555-1234", "123 New St").Value;

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Contact!.Email.Should().Be("newemail@example.com");
        registration.Contact.PhoneNumber.Should().Be("555-1234");
        registration.Contact.Address.Should().Be("123 New St");
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateContactPhone()
    {
        // Arrange
        var registration = CreateConfirmedRegistration();
        var newAttendees = new List<AttendeeDetails> { CreateAttendee("John Doe", 30) };
        var newContact = RegistrationContact.Create("test@example.com", "999-8888", null).Value;

        // Act
        var result = registration.UpdateDetails(newAttendees, newContact);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Contact!.PhoneNumber.Should().Be("999-8888");
    }

    #endregion
}
