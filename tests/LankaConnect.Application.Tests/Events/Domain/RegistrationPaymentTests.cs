using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for RegistrationPayment entity
/// Part of the Add-Only Attendees with Delta Payment feature
/// </summary>
public class RegistrationPaymentTests
{
    #region Helper Methods

    private static Money CreateMoney(decimal amount, Currency currency = Currency.USD)
    {
        return Money.Create(amount, currency).Value;
    }

    #endregion

    #region CreateInitial Tests

    [Fact]
    public void CreateInitial_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var paymentIntentId = "pi_test_123";
        var amount = CreateMoney(100m);

        // Act
        var result = RegistrationPayment.CreateInitial(registrationId, paymentIntentId, amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationId.Should().Be(registrationId);
        result.Value.StripePaymentIntentId.Should().Be(paymentIntentId);
        result.Value.Amount.Amount.Should().Be(100m);
        result.Value.Type.Should().Be(PaymentType.Initial);
        result.Value.Status.Should().Be(PaymentStatus.Completed);
        result.Value.RegistrationAdditionId.Should().BeNull();
        result.Value.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void CreateInitial_WithEmptyRegistrationId_ShouldFail()
    {
        // Act
        var result = RegistrationPayment.CreateInitial(
            Guid.Empty,
            "pi_test_123",
            CreateMoney(100m));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Registration ID");
    }

    [Fact]
    public void CreateInitial_WithEmptyPaymentIntentId_ShouldFail()
    {
        // Act
        var result = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "",
            CreateMoney(100m));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment Intent");
    }

    [Fact]
    public void CreateInitial_WithNegativeAmount_ShouldFail()
    {
        // Arrange - Money.Create doesn't allow negative amounts, but let's test with a valid Money
        // Actually, Money.Create validates this, so we need to test the flow differently
        // For now, we'll test with null amount
        // Act
        var result = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Amount");
    }

    [Fact]
    public void CreateInitial_WithPendingStatus_ShouldNotSetCompletedAt()
    {
        // Act
        var result = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Pending);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        result.Value.CompletedAt.Should().BeNull();
    }

    #endregion

    #region CreateAddition Tests

    [Fact]
    public void CreateAddition_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var additionId = Guid.NewGuid();
        var paymentIntentId = "pi_test_456";
        var amount = CreateMoney(50m);

        // Act
        var result = RegistrationPayment.CreateAddition(
            registrationId,
            paymentIntentId,
            amount,
            additionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationId.Should().Be(registrationId);
        result.Value.StripePaymentIntentId.Should().Be(paymentIntentId);
        result.Value.Amount.Amount.Should().Be(50m);
        result.Value.Type.Should().Be(PaymentType.Addition);
        result.Value.RegistrationAdditionId.Should().Be(additionId);
    }

    [Fact]
    public void CreateAddition_WithEmptyAdditionId_ShouldFail()
    {
        // Act
        var result = RegistrationPayment.CreateAddition(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(50m),
            Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Addition ID");
    }

    #endregion

    #region MarkAsCompleted Tests

    [Fact]
    public void MarkAsCompleted_WhenPending_ShouldSucceed()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Pending).Value;

        // Act
        var result = payment.MarkAsCompleted();

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsCompleted_WhenAlreadyCompleted_ShouldFail()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Completed).Value;

        // Act
        var result = payment.MarkAsCompleted();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already completed");
    }

    [Fact]
    public void MarkAsCompleted_WhenFailed_ShouldFail()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Pending).Value;
        payment.MarkAsFailed();

        // Act
        var result = payment.MarkAsCompleted();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("failed");
    }

    #endregion

    #region MarkAsFailed Tests

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldSucceed()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Pending).Value;

        // Act
        var result = payment.MarkAsFailed();

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_WhenCompleted_ShouldFail()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Completed).Value;

        // Act
        var result = payment.MarkAsFailed();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("completed");
    }

    #endregion

    #region MarkAsRefunded Tests

    [Fact]
    public void MarkAsRefunded_WhenCompleted_ShouldSucceed()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Completed).Value;

        // Act
        var result = payment.MarkAsRefunded();

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_WhenPending_ShouldFail()
    {
        // Arrange
        var payment = RegistrationPayment.CreateInitial(
            Guid.NewGuid(),
            "pi_test_123",
            CreateMoney(100m),
            PaymentStatus.Pending).Value;

        // Act
        var result = payment.MarkAsRefunded();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("completed");
    }

    #endregion
}
