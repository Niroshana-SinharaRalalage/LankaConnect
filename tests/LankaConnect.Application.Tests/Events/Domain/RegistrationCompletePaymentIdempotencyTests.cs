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
/// Issue #56: TDD Tests for CompletePayment Idempotency
/// These tests verify that duplicate webhook calls don't raise duplicate PaymentCompletedEvents.
/// Written BEFORE implementation (RED phase of TDD).
/// </summary>
public class RegistrationCompletePaymentIdempotencyTests
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

    /// <summary>
    /// Creates a Preliminary (pending payment) registration for paid events.
    /// This simulates the state after user submits registration form but before payment.
    /// </summary>
    private static Registration CreatePreliminaryRegistration()
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee("John Doe", AgeCategory.Adult) };
        var contact = CreateContact();
        var price = new Money(100m, Currency.USD);

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

    #region Issue #56 Fix: CompletePayment Idempotency Tests

    [Fact]
    public void CompletePayment_FirstCall_ShouldSucceedAndRaiseDomainEvent()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        var result = registration.CompletePayment("pi_test_123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
        registration.PaymentStatus.Should().Be(PaymentStatus.Completed);
        registration.StripePaymentIntentId.Should().Be("pi_test_123");
        registration.DomainEvents.Should().ContainSingle(e => e is PaymentCompletedEvent);
    }

    [Fact]
    public void CompletePayment_DuplicateCallWithSamePaymentIntent_ShouldReturnSuccessWithoutRaisingEvent()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // First call - normal payment completion
        var firstResult = registration.CompletePayment("pi_test_123");
        firstResult.IsSuccess.Should().BeTrue();
        registration.ClearDomainEvents(); // Clear events from first call

        // Act - Simulate duplicate webhook (same PaymentIntentId)
        var result = registration.CompletePayment("pi_test_123");

        // Assert
        result.IsSuccess.Should().BeTrue(); // Idempotent success - no error returned
        registration.DomainEvents.Should().BeEmpty(); // NO duplicate PaymentCompletedEvent raised
        registration.Status.Should().Be(RegistrationStatus.Confirmed); // State unchanged
        registration.PaymentStatus.Should().Be(PaymentStatus.Completed); // State unchanged
    }

    [Fact]
    public void CompletePayment_DifferentPaymentIntentAfterCompletion_ShouldFail()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();
        registration.CompletePayment("pi_first_123"); // First payment completes
        registration.ClearDomainEvents();

        // Act - Try with DIFFERENT payment intent (should not be possible in normal flow)
        var result = registration.CompletePayment("pi_different_456");

        // Assert
        result.IsSuccess.Should().BeFalse(); // Should fail - not idempotent for different payment
        result.Error.Should().Contain("Cannot complete payment"); // Existing validation should catch this
    }

    [Fact]
    public void CompletePayment_WithEmptyPaymentIntentId_ShouldFail()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        var result = registration.CompletePayment("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot be empty");
    }

    [Fact]
    public void CompletePayment_WhenAlreadyConfirmed_WithSamePaymentIntent_ShouldReturnSuccess()
    {
        // Arrange - Simulate a registration that was already confirmed via payment
        var registration = CreatePreliminaryRegistration();
        registration.CompletePayment("pi_test_123");
        registration.ClearDomainEvents();

        // Verify state is Confirmed
        registration.Status.Should().Be(RegistrationStatus.Confirmed);

        // Act - Another call with same payment intent (duplicate webhook scenario)
        var result = registration.CompletePayment("pi_test_123");

        // Assert - Should succeed idempotently
        result.IsSuccess.Should().BeTrue();
        registration.DomainEvents.Should().BeEmpty(); // No new events
    }

    [Fact]
    public void CompletePayment_WhenAbandonedStatus_ShouldFail()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();
        registration.MarkAbandoned(); // Checkout session expired
        registration.ClearDomainEvents();

        // Act
        var result = registration.CompletePayment("pi_test_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot complete payment");
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void CompletePayment_CaseInsensitivePaymentIntentComparison_ShouldReturnSuccess()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();
        registration.CompletePayment("pi_TEST_123");
        registration.ClearDomainEvents();

        // Act - Same payment intent but different case
        var result = registration.CompletePayment("PI_test_123");

        // Assert - Should be idempotent (case-insensitive match)
        result.IsSuccess.Should().BeTrue();
        registration.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region PaymentCompletedEvent Content Verification

    [Fact]
    public void CompletePayment_ShouldRaiseEventWithCorrectData()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        registration.CompletePayment("pi_test_123");

        // Assert
        var domainEvent = registration.DomainEvents
            .OfType<PaymentCompletedEvent>()
            .SingleOrDefault();

        domainEvent.Should().NotBeNull();
        domainEvent!.RegistrationId.Should().Be(registration.Id);
        domainEvent.PaymentIntentId.Should().Be("pi_test_123");
        domainEvent.EventId.Should().Be(registration.EventId);
    }

    #endregion
}
