using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Registration refund workflow methods (Phase 6A.91)
/// Tests RequestRefund(), WithdrawRefundRequest(), and CompleteRefund() state transitions
/// </summary>
public class RegistrationRefundWorkflowTests
{
    #region Helper Methods

    private static AttendeeDetails CreateAttendee(string name, AgeCategory ageCategory = AgeCategory.Adult)
    {
        return AttendeeDetails.Create(name, ageCategory).Value;
    }

    private static RegistrationContact CreateContact(string email = "test@example.com", string phone = "555-1234")
    {
        return RegistrationContact.Create(email, phone, null).Value;
    }

    private static Money CreateMoney(decimal amount = 100m)
    {
        return Money.Create(amount, Currency.USD).Value;
    }

    /// <summary>
    /// Creates a paid confirmed registration (simulates completed payment)
    /// </summary>
    private static Registration CreatePaidConfirmedRegistration(decimal amount = 100m)
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee("John Doe", AgeCategory.Adult) };
        var contact = CreateContact();
        var price = Money.Create(amount, Currency.USD).Value;

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

        // Clear domain events from setup
        registration.ClearDomainEvents();

        return registration;
    }

    /// <summary>
    /// Creates a free confirmed registration
    /// </summary>
    private static Registration CreateFreeConfirmedRegistration()
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee("John Doe", AgeCategory.Adult) };
        var contact = CreateContact();
        var price = Money.Create(0m, Currency.USD).Value;

        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: false).Value;

        // Clear domain events from setup
        registration.ClearDomainEvents();

        return registration;
    }

    /// <summary>
    /// Creates a registration in RefundRequested state
    /// </summary>
    private static Registration CreateRefundRequestedRegistration()
    {
        var registration = CreatePaidConfirmedRegistration();
        registration.RequestRefund();
        registration.ClearDomainEvents();
        return registration;
    }

    /// <summary>
    /// Creates a Preliminary (pending payment) registration
    /// </summary>
    private static Registration CreatePreliminaryRegistration()
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee("John Doe", AgeCategory.Adult) };
        var contact = CreateContact();
        var price = Money.Create(100m, Currency.USD).Value;

        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: true).Value;

        // Set checkout session but don't complete payment - stays in Preliminary
        registration.SetStripeCheckoutSession("cs_test_123");
        registration.ClearDomainEvents();

        return registration;
    }

    #endregion

    #region RequestRefund Tests

    [Fact]
    public void RequestRefund_FromConfirmedPaidRegistration_ShouldSucceed()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();

        // Act
        var result = registration.RequestRefund();

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);
        registration.RefundRequestedAt.Should().NotBeNull();
        registration.RefundRequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RequestRefund_ShouldRaiseRefundRequestedEvent()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();

        // Act
        registration.RequestRefund();

        // Assert
        registration.DomainEvents.Should().ContainSingle(e => e is RefundRequestedEvent);
        var domainEvent = registration.DomainEvents.OfType<RefundRequestedEvent>().First();
        domainEvent.RegistrationId.Should().Be(registration.Id);
        domainEvent.PaymentIntentId.Should().Be("pi_test_123");
    }

    [Fact]
    public void RequestRefund_FromPreliminaryRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        var result = registration.RequestRefund();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot request refund");
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
    }

    [Fact]
    public void RequestRefund_FromRefundRequestedRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();

        // Act
        var result = registration.RequestRefund();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot request refund");
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);
    }

    [Fact]
    public void RequestRefund_FromCancelledRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();
        registration.Cancel();

        // Act
        var result = registration.RequestRefund();

        // Assert
        result.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Cancelled);
    }

    #endregion

    #region WithdrawRefundRequest Tests

    [Fact]
    public void WithdrawRefundRequest_FromRefundRequested_ShouldSucceed()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();

        // Act
        var result = registration.WithdrawRefundRequest();

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
        registration.RefundWithdrawnAt.Should().NotBeNull();
        registration.RefundWithdrawnAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void WithdrawRefundRequest_ShouldRaiseRefundWithdrawnEvent()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();

        // Act
        registration.WithdrawRefundRequest();

        // Assert
        registration.DomainEvents.Should().ContainSingle(e => e is RefundWithdrawnEvent);
        var domainEvent = registration.DomainEvents.OfType<RefundWithdrawnEvent>().First();
        domainEvent.RegistrationId.Should().Be(registration.Id);
    }

    [Fact]
    public void WithdrawRefundRequest_FromConfirmedRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();

        // Act
        var result = registration.WithdrawRefundRequest();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot withdraw refund request");
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    [Fact]
    public void WithdrawRefundRequest_FromPreliminaryRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        var result = registration.WithdrawRefundRequest();

        // Assert
        result.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
    }

    [Fact]
    public void WithdrawRefundRequest_FromRefundedRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();
        registration.CompleteRefund("re_test_123");

        // Act
        var result = registration.WithdrawRefundRequest();

        // Assert
        result.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Refunded);
    }

    #endregion

    #region CompleteRefund Tests

    [Fact]
    public void CompleteRefund_FromRefundRequested_ShouldSucceed()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();
        var stripeRefundId = "re_test_refund_123";

        // Act
        var result = registration.CompleteRefund(stripeRefundId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Refunded);
        registration.StripeRefundId.Should().Be(stripeRefundId);
        registration.RefundCompletedAt.Should().NotBeNull();
        registration.RefundCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CompleteRefund_ShouldRaiseRefundCompletedEvent()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();
        var stripeRefundId = "re_test_refund_123";

        // Act
        registration.CompleteRefund(stripeRefundId);

        // Assert
        registration.DomainEvents.Should().ContainSingle(e => e is RefundCompletedEvent);
        var domainEvent = registration.DomainEvents.OfType<RefundCompletedEvent>().First();
        domainEvent.RegistrationId.Should().Be(registration.Id);
        domainEvent.StripeRefundId.Should().Be(stripeRefundId);
    }

    [Fact]
    public void CompleteRefund_FromConfirmedRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();

        // Act
        var result = registration.CompleteRefund("re_test_123");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot complete refund");
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    [Fact]
    public void CompleteRefund_FromPreliminaryRegistration_ShouldFail()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        var result = registration.CompleteRefund("re_test_123");

        // Assert
        result.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
    }

    [Fact]
    public void CompleteRefund_WithNullRefundId_ShouldFail()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();

        // Act
        var result = registration.CompleteRefund(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Stripe Refund ID is required");
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);
    }

    [Fact]
    public void CompleteRefund_WithEmptyRefundId_ShouldFail()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();

        // Act
        var result = registration.CompleteRefund("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Stripe Refund ID is required");
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);
    }

    [Fact]
    public void CompleteRefund_AlreadyRefunded_ShouldFail()
    {
        // Arrange
        var registration = CreateRefundRequestedRegistration();
        registration.CompleteRefund("re_test_123");

        // Act
        var result = registration.CompleteRefund("re_test_456");

        // Assert
        result.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Refunded);
        registration.StripeRefundId.Should().Be("re_test_123"); // Should keep original
    }

    #endregion

    #region Full Workflow Tests

    [Fact]
    public void RefundWorkflow_FullCycle_RequestThenComplete_ShouldSucceed()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();

        // Act - Request refund
        var requestResult = registration.RequestRefund();
        requestResult.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);

        // Act - Complete refund
        var completeResult = registration.CompleteRefund("re_test_final_123");
        completeResult.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Refunded);

        // Assert - Final state
        registration.RefundRequestedAt.Should().NotBeNull();
        registration.RefundCompletedAt.Should().NotBeNull();
        registration.StripeRefundId.Should().Be("re_test_final_123");
    }

    [Fact]
    public void RefundWorkflow_RequestThenWithdraw_ShouldRestoreConfirmed()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();

        // Act - Request refund
        var requestResult = registration.RequestRefund();
        requestResult.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);

        // Act - Withdraw refund request
        var withdrawResult = registration.WithdrawRefundRequest();
        withdrawResult.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);

        // Assert - Can still use registration
        registration.RefundRequestedAt.Should().NotBeNull(); // Keeps audit trail
        registration.RefundWithdrawnAt.Should().NotBeNull();
        registration.RefundCompletedAt.Should().BeNull(); // Never completed
        registration.StripeRefundId.Should().BeNull();
    }

    [Fact]
    public void RefundWorkflow_CannotWithdrawAfterComplete_ShouldFail()
    {
        // Arrange
        var registration = CreatePaidConfirmedRegistration();
        registration.RequestRefund();
        registration.CompleteRefund("re_test_123");

        // Act - Try to withdraw after completion
        var withdrawResult = registration.WithdrawRefundRequest();

        // Assert
        withdrawResult.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Refunded);
    }

    [Fact]
    public void RefundWorkflow_CannotRequestRefundAfterWithdrawal_ShouldSucceed()
    {
        // Arrange - User can request refund again after withdrawal
        var registration = CreatePaidConfirmedRegistration();
        registration.RequestRefund();
        registration.WithdrawRefundRequest();

        // Act - Request refund again
        var result = registration.RequestRefund();

        // Assert - Should succeed (user changed their mind again)
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.RefundRequested);
    }

    #endregion
}
