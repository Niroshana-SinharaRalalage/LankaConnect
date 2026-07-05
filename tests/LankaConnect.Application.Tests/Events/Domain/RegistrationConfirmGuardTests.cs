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
/// Unit tests for Registration.Confirm() method guard
/// Phase 6A.X: Prevents the recurring Confirmed+Pending inconsistent state bug
///
/// Root Cause Analysis:
/// The Confirm() method was setting Status=Confirmed without validating PaymentStatus,
/// allowing registrations to end up in an invalid state where Status=Confirmed but
/// PaymentStatus=Pending. This caused the frontend to not display registration details.
///
/// Fix: Confirm() now validates that PaymentStatus is NOT Pending before confirming.
/// For paid events, use CompletePayment() which handles the proper state transition.
/// </summary>
public class RegistrationConfirmGuardTests
{
    #region Helper Methods

    private static AttendeeDetails CreateAttendee(string name = "John Doe", AgeCategory ageCategory = AgeCategory.Adult)
    {
        return AttendeeDetails.Create(name, ageCategory).Value;
    }

    private static RegistrationContact CreateContact(string email = "test@example.com", string phone = "555-1234")
    {
        return RegistrationContact.Create(email, phone, null).Value;
    }

    /// <summary>
    /// Creates a Preliminary registration (paid event, payment pending)
    /// This simulates a registration waiting for Stripe checkout to complete
    /// </summary>
    private static Registration CreatePreliminaryRegistration()
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
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

    /// <summary>
    /// Creates a free event registration (Confirmed, PaymentStatus=NotRequired)
    /// </summary>
    private static Registration CreateFreeConfirmedRegistration()
    {
        var attendees = new List<AttendeeDetails> { CreateAttendee() };
        var contact = CreateContact();
        var price = Money.Create(0m, Currency.USD).Value;

        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attendees,
            contact,
            price,
            isPaidEvent: false).Value;

        registration.ClearDomainEvents();
        return registration;
    }

    /// <summary>
    /// Creates a paid confirmed registration (simulates completed payment)
    /// </summary>
    private static Registration CreatePaidConfirmedRegistration()
    {
        var registration = CreatePreliminaryRegistration();
        registration.CompletePayment("pi_test_123");
        registration.ClearDomainEvents();
        return registration;
    }

    #endregion

    #region Confirm() Guard Tests - Prevent Confirmed+Pending State

    [Fact]
    public void Confirm_WithPendingPaymentStatus_ShouldFail()
    {
        // Arrange
        // Create a Preliminary registration with PaymentStatus=Pending
        var registration = CreatePreliminaryRegistration();

        // Verify preconditions
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
        registration.PaymentStatus.Should().Be(PaymentStatus.Pending);

        // Act
        var result = registration.Confirm();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot confirm registration with PaymentStatus");
        result.Error.Should().Contain("Pending");

        // Status should NOT have changed
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
    }

    [Fact]
    public void Confirm_WithCompletedPaymentStatus_ShouldSucceed()
    {
        // Arrange
        // Create a paid registration that completed payment
        var registration = CreatePaidConfirmedRegistration();

        // Cancel it first so we can test Confirm()
        registration.Cancel();
        registration.Status.Should().Be(RegistrationStatus.Cancelled);
        registration.PaymentStatus.Should().Be(PaymentStatus.Completed);

        // Act
        var result = registration.Confirm();

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WithNotRequiredPaymentStatus_ShouldSucceed()
    {
        // Arrange
        // Create a free event registration
        var registration = CreateFreeConfirmedRegistration();

        // Cancel it first so we can test Confirm()
        registration.Cancel();
        registration.Status.Should().Be(RegistrationStatus.Cancelled);
        registration.PaymentStatus.Should().Be(PaymentStatus.NotRequired);

        // Act
        var result = registration.Confirm();

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldReturnSuccessWithoutChanges()
    {
        // Arrange
        var registration = CreateFreeConfirmedRegistration();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);

        // Act
        var result = registration.Confirm();

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WithFailedPaymentStatus_ShouldSucceed()
    {
        // Arrange
        // Create preliminary and mark as abandoned (PaymentStatus=Failed)
        var registration = CreatePreliminaryRegistration();
        registration.MarkAbandoned();
        registration.PaymentStatus.Should().Be(PaymentStatus.Failed);

        // Act - try to confirm an abandoned registration
        // (This is an edge case - normally abandoned registrations should not be confirmed)
        var result = registration.Confirm();

        // Assert - should succeed since PaymentStatus is not Pending
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    #endregion

    #region Confirm() Should Log Appropriately

    [Fact]
    public void Confirm_WithPendingPayment_ErrorShouldContainRegistrationId()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act
        var result = registration.Confirm();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("RegistrationId");
    }

    #endregion

    #region Integration: Correct Way to Confirm Paid Registrations

    [Fact]
    public void PaidRegistration_ShouldUseCompletePayment_NotConfirm()
    {
        // Arrange
        var registration = CreatePreliminaryRegistration();

        // Act - Use CompletePayment() which is the correct method for paid events
        var result = registration.CompletePayment("pi_test_correct_123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Confirmed);
        registration.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void PaidRegistration_CannotBypassPaymentWithConfirm()
    {
        // Arrange
        // This test ensures that Confirm() cannot be used to bypass payment
        var registration = CreatePreliminaryRegistration();

        // Act - Try to bypass payment by calling Confirm() directly
        var result = registration.Confirm();

        // Assert - Should fail, preventing payment bypass
        result.IsFailure.Should().BeTrue();
        registration.Status.Should().Be(RegistrationStatus.Preliminary);
        registration.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    #endregion
}
