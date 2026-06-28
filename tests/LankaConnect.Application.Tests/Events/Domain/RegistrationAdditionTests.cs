using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for RegistrationAddition entity
/// Part of the Add-Only Attendees with Delta Payment feature
/// </summary>
public class RegistrationAdditionTests
{
    #region Helper Methods

    private static AttendeeDetails CreateAttendee(string name = "Test Attendee", AgeCategory ageCategory = AgeCategory.Adult)
    {
        return AttendeeDetails.Create(name, ageCategory).Value;
    }

    private static Money CreateMoney(decimal amount, Currency currency = Currency.USD)
    {
        return Money.Create(amount, currency).Value;
    }

    private static List<AttendeeDetails> CreateAttendeeList(int count = 1)
    {
        var attendees = new List<AttendeeDetails>();
        for (int i = 0; i < count; i++)
        {
            attendees.Add(CreateAttendee($"Attendee {i + 1}"));
        }
        return attendees;
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var newAttendees = CreateAttendeeList(2);
        var previousTotal = CreateMoney(25m);
        var newTotal = CreateMoney(75m);
        var additionalAmount = CreateMoney(50m);

        // Act
        var result = RegistrationAddition.Create(
            registrationId,
            eventId,
            newAttendees,
            previousTotal,
            newTotal,
            additionalAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationId.Should().Be(registrationId);
        result.Value.EventId.Should().Be(eventId);
        result.Value.NewAttendees.Should().HaveCount(2);
        result.Value.PreviousTotalPrice.Amount.Should().Be(25m);
        result.Value.NewTotalPrice.Amount.Should().Be(75m);
        result.Value.AdditionalAmount.Amount.Should().Be(50m);
        result.Value.Status.Should().Be(RegistrationAdditionStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptyRegistrationId_ShouldFail()
    {
        // Arrange
        var newAttendees = CreateAttendeeList(1);
        var previousTotal = CreateMoney(25m);
        var newTotal = CreateMoney(50m);
        var additionalAmount = CreateMoney(25m);

        // Act
        var result = RegistrationAddition.Create(
            Guid.Empty,
            Guid.NewGuid(),
            newAttendees,
            previousTotal,
            newTotal,
            additionalAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Registration ID");
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        // Arrange
        var newAttendees = CreateAttendeeList(1);
        var previousTotal = CreateMoney(25m);
        var newTotal = CreateMoney(50m);
        var additionalAmount = CreateMoney(25m);

        // Act
        var result = RegistrationAddition.Create(
            Guid.NewGuid(),
            Guid.Empty,
            newAttendees,
            previousTotal,
            newTotal,
            additionalAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID");
    }

    [Fact]
    public void Create_WithEmptyAttendeeList_ShouldFail()
    {
        // Arrange
        var previousTotal = CreateMoney(25m);
        var newTotal = CreateMoney(50m);
        var additionalAmount = CreateMoney(25m);

        // Act
        var result = RegistrationAddition.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new List<AttendeeDetails>(),
            previousTotal,
            newTotal,
            additionalAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("attendee");
    }

    [Fact]
    public void Create_WithMismatchedCurrencies_ShouldFail()
    {
        // Arrange
        var newAttendees = CreateAttendeeList(1);
        var previousTotal = CreateMoney(25m, Currency.USD);
        var newTotal = CreateMoney(50m, Currency.EUR);
        var additionalAmount = CreateMoney(25m, Currency.USD);

        // Act
        var result = RegistrationAddition.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            newAttendees,
            previousTotal,
            newTotal,
            additionalAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("currency");
    }

    [Fact]
    public void Create_WithIncorrectAdditionalAmount_ShouldFail()
    {
        // Arrange
        var newAttendees = CreateAttendeeList(1);
        var previousTotal = CreateMoney(25m);
        var newTotal = CreateMoney(75m);
        var additionalAmount = CreateMoney(30m); // Should be 50, not 30

        // Act
        var result = RegistrationAddition.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            newAttendees,
            previousTotal,
            newTotal,
            additionalAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Additional amount");
    }

    #endregion

    #region SetStripeCheckoutSession Tests

    [Fact]
    public void SetStripeCheckoutSession_WhenPending_ShouldSucceed()
    {
        // Arrange
        var addition = CreateValidAddition();
        var sessionId = "cs_test_123";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        var result = addition.SetStripeCheckoutSession(sessionId, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        addition.StripeCheckoutSessionId.Should().Be(sessionId);
        addition.CheckoutExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetStripeCheckoutSession_WithEmptySessionId_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Act
        var result = addition.SetStripeCheckoutSession("", DateTime.UtcNow.AddHours(24));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("session");
    }

    [Fact]
    public void SetStripeCheckoutSession_WithPastExpirationTime_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Act
        var result = addition.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(-1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("future");
    }

    #endregion

    #region CompletePayment Tests

    [Fact]
    public void CompletePayment_WhenPending_ShouldSucceed()
    {
        // Arrange
        var addition = CreateValidAddition();
        var paymentIntentId = "pi_test_123";

        // Act
        var result = addition.CompletePayment(paymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        addition.Status.Should().Be(RegistrationAdditionStatus.PaymentCompleted);
        addition.StripePaymentIntentId.Should().Be(paymentIntentId);
        addition.PaymentCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompletePayment_WhenAlreadyCompleted_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();
        addition.CompletePayment("pi_test_123");

        // Act
        var result = addition.CompletePayment("pi_test_456");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PaymentCompleted");
    }

    [Fact]
    public void CompletePayment_WithEmptyPaymentIntentId_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Act
        var result = addition.CompletePayment("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("intent");
    }

    #endregion

    #region MarkAsMerged Tests

    [Fact]
    public void MarkAsMerged_WhenPaymentCompleted_ShouldSucceed()
    {
        // Arrange
        var addition = CreateValidAddition();
        addition.CompletePayment("pi_test_123");

        // Act
        var result = addition.MarkAsMerged();

        // Assert
        result.IsSuccess.Should().BeTrue();
        addition.Status.Should().Be(RegistrationAdditionStatus.Merged);
        addition.MergedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsMerged_WhenPending_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Act
        var result = addition.MarkAsMerged();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PaymentCompleted");
    }

    #endregion

    #region MarkAsFailed Tests

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldSucceed()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Act
        var result = addition.MarkAsFailed();

        // Assert
        result.IsSuccess.Should().BeTrue();
        addition.Status.Should().Be(RegistrationAdditionStatus.Failed);
        addition.FailedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyMerged_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();
        addition.CompletePayment("pi_test_123");
        addition.MarkAsMerged();

        // Act
        var result = addition.MarkAsFailed();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("merged");
    }

    #endregion

    #region MarkAsAbandoned Tests

    [Fact]
    public void MarkAsAbandoned_WhenPending_ShouldSucceed()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Act
        var result = addition.MarkAsAbandoned();

        // Assert
        result.IsSuccess.Should().BeTrue();
        addition.Status.Should().Be(RegistrationAdditionStatus.Abandoned);
        addition.AbandonedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsAbandoned_WhenPaymentCompleted_ShouldFail()
    {
        // Arrange
        var addition = CreateValidAddition();
        addition.CompletePayment("pi_test_123");

        // Act
        var result = addition.MarkAsAbandoned();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsTerminal_WhenMerged_ShouldBeTrue()
    {
        // Arrange
        var addition = CreateValidAddition();
        addition.CompletePayment("pi_test_123");
        addition.MarkAsMerged();

        // Assert
        addition.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenPending_ShouldBeFalse()
    {
        // Arrange
        var addition = CreateValidAddition();

        // Assert
        addition.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void NewAttendeesCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var newAttendees = CreateAttendeeList(3);
        var addition = RegistrationAddition.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            newAttendees,
            CreateMoney(25m),
            CreateMoney(100m),
            CreateMoney(75m)).Value;

        // Assert
        addition.NewAttendeesCount.Should().Be(3);
    }

    #endregion

    #region Private Helper Methods

    private static RegistrationAddition CreateValidAddition()
    {
        return RegistrationAddition.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreateAttendeeList(2),
            CreateMoney(25m),
            CreateMoney(75m),
            CreateMoney(50m)).Value;
    }

    #endregion
}
