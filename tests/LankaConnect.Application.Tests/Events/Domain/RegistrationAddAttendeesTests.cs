using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.SharedKernel.Money;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Registration.AddAttendees() method
/// Part of the Add-Only Attendees with Delta Payment feature
/// </summary>
public class RegistrationAddAttendeesTests
{
    #region Helper Methods

    private static AttendeeDetails CreateAttendee(string name = "Test Attendee", AgeCategory ageCategory = AgeCategory.Adult)
    {
        return AttendeeDetails.Create(name, ageCategory).Value;
    }

    private static RegistrationContact CreateContact(string email = "test@example.com", string phone = "555-1234")
    {
        return RegistrationContact.Create(email, phone, null).Value;
    }

    private static Money CreateMoney(decimal amount, Currency? currency = null)
    {
        return new Money(amount, currency ?? Currency.USD);
    }

    private static List<AttendeeDetails> CreateAttendeeList(int count)
    {
        var attendees = new List<AttendeeDetails>();
        for (int i = 0; i < count; i++)
        {
            attendees.Add(CreateAttendee($"Attendee {i + 1}"));
        }
        return attendees;
    }

    private static Registration CreatePaidConfirmedRegistration(
        int attendeeCount = 1,
        decimal totalPrice = 25m)
    {
        var attendees = CreateAttendeeList(attendeeCount);
        var contact = CreateContact();
        var price = CreateMoney(totalPrice);

        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: true).Value;

        // Simulate payment completion
        registration.SetStripeCheckoutSession("cs_test_123");
        registration.CompletePayment("pi_test_123");

        return registration;
    }

    private static Registration CreateFreeConfirmedRegistration(int attendeeCount = 1)
    {
        var attendees = CreateAttendeeList(attendeeCount);
        var contact = CreateContact();
        var price = CreateMoney(0m);

        return Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: false).Value;
    }

    private static RegistrationPayment CreatePayment(Guid registrationId, decimal amount)
    {
        return RegistrationPayment.CreateAddition(
            registrationId,
            "pi_test_additional",
            CreateMoney(amount),
            Guid.NewGuid()).Value;
    }

    #endregion

    #region Success Tests

    [Fact]
    public void AddAttendees_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        var newAttendees = CreateAttendeeList(2);
        var newTotalPrice = CreateMoney(75m);
        var payment = CreatePayment(registration.Id, 50m);
        var additionId = Guid.NewGuid();

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, additionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.GetAttendeeCount().Should().Be(3); // 1 original + 2 new
        registration.TotalPrice!.Amount.Should().Be(75m);
        registration.Quantity.Should().Be(3);
    }

    [Fact]
    public void AddAttendees_ShouldRaiseDomainEvent()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        registration.ClearDomainEvents(); // Clear any previous events
        var newAttendees = CreateAttendeeList(2);
        var newTotalPrice = CreateMoney(75m);
        var payment = CreatePayment(registration.Id, 50m);
        var additionId = Guid.NewGuid();

        // Act
        registration.AddAttendees(newAttendees, newTotalPrice, payment, additionId);

        // Assert
        var domainEvent = registration.DomainEvents
            .OfType<AttendeesAddedEvent>()
            .FirstOrDefault();

        domainEvent.Should().NotBeNull();
        domainEvent!.PreviousAttendeeCount.Should().Be(1);
        domainEvent.AddedAttendeeCount.Should().Be(2);
        domainEvent.NewTotalAttendeeCount.Should().Be(3);
        domainEvent.AdditionalAmountPaid.Should().Be(50m);
        domainEvent.TotalAmountPaid.Should().Be(75m);
    }

    [Fact]
    public void AddAttendees_WithGroupTierBenefit_ShouldWork()
    {
        // Arrange: 2 attendees at $30/person = $60 paid
        // Adding 2 more: 4 attendees at $25/person tier = $100 total
        // Additional charge = $100 - $60 = $40
        var registration = CreatePaidConfirmedRegistration(2, 60m);
        var newAttendees = CreateAttendeeList(2);
        var newTotalPrice = CreateMoney(100m); // New tier price
        var payment = CreatePayment(registration.Id, 40m); // Delta
        var additionId = Guid.NewGuid();

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, additionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.GetAttendeeCount().Should().Be(4);
        registration.TotalPrice!.Amount.Should().Be(100m);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void AddAttendees_WhenNotConfirmed_ShouldFail()
    {
        // Arrange: Create registration without confirming payment
        var attendees = CreateAttendeeList(1);
        var contact = CreateContact();
        var price = CreateMoney(25m);
        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: true).Value;
        // Status is Preliminary (not Confirmed)

        var newAttendees = CreateAttendeeList(1);
        var newTotalPrice = CreateMoney(50m);
        var payment = CreatePayment(registration.Id, 25m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("confirmed");
    }

    [Fact]
    public void AddAttendees_WhenPaymentNotCompleted_ShouldFail()
    {
        // Arrange: Free event registration (no payment)
        var registration = CreateFreeConfirmedRegistration(1);
        var newAttendees = CreateAttendeeList(1);
        var newTotalPrice = CreateMoney(0m);
        var payment = CreatePayment(registration.Id, 0m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("paid");
    }

    [Fact]
    public void AddAttendees_WithEmptyAttendeeList_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        var newAttendees = new List<AttendeeDetails>();
        var newTotalPrice = CreateMoney(25m);
        var payment = CreatePayment(registration.Id, 0m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("attendee");
    }

    [Fact]
    public void AddAttendees_WithNullNewTotalPrice_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        var newAttendees = CreateAttendeeList(1);
        var payment = CreatePayment(registration.Id, 25m);

        // Act
        var result = registration.AddAttendees(newAttendees, null!, payment, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("total price");
    }

    [Fact]
    public void AddAttendees_WithNullPayment_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        var newAttendees = CreateAttendeeList(1);
        var newTotalPrice = CreateMoney(50m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, null!, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("payment");
    }

    [Fact]
    public void AddAttendees_WithEmptyAdditionId_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        var newAttendees = CreateAttendeeList(1);
        var newTotalPrice = CreateMoney(50m);
        var payment = CreatePayment(registration.Id, 25m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Addition ID");
    }

    [Fact]
    public void AddAttendees_ExceedingMaxAttendees_ShouldFail()
    {
        // Arrange: Start with 8 attendees
        var registration = CreatePaidConfirmedRegistration(8, 200m);
        var newAttendees = CreateAttendeeList(5); // Would make 13, exceeding max of 10
        var newTotalPrice = CreateMoney(325m);
        var payment = CreatePayment(registration.Id, 125m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("10"); // Max attendees message
    }

    [Fact]
    public void AddAttendees_WithCustomMaxAttendees_ShouldRespectLimit()
    {
        // Arrange: Start with 3 attendees, custom max of 5
        var registration = CreatePaidConfirmedRegistration(3, 75m);
        var newAttendees = CreateAttendeeList(3); // Would make 6, exceeding custom max of 5
        var newTotalPrice = CreateMoney(150m);
        var payment = CreatePayment(registration.Id, 75m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid(), maxAttendeesPerRegistration: 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("5"); // Custom max
    }

    [Fact]
    public void AddAttendees_ToExactMaxAttendees_ShouldSucceed()
    {
        // Arrange: Start with 8 attendees
        var registration = CreatePaidConfirmedRegistration(8, 200m);
        var newAttendees = CreateAttendeeList(2); // Would make exactly 10 (the max)
        var newTotalPrice = CreateMoney(250m);
        var payment = CreatePayment(registration.Id, 50m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.GetAttendeeCount().Should().Be(10);
    }

    #endregion

    #region Cancelled/Refunded Tests

    [Fact]
    public void AddAttendees_ToCancelledRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration(1, 25m);
        registration.Cancel();

        var newAttendees = CreateAttendeeList(1);
        var newTotalPrice = CreateMoney(50m);
        var payment = CreatePayment(registration.Id, 25m);

        // Act
        var result = registration.AddAttendees(newAttendees, newTotalPrice, payment, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("confirmed");
    }

    #endregion
}
