using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Comprehensive unit tests for AddOnPurchase entity.
/// Covers creation, payment lifecycle, domain events, and edge cases.
/// </summary>
public class AddOnPurchaseTests
{
    #region Test Helpers

    private static readonly Guid ValidEventId = Guid.NewGuid();
    private static readonly Guid ValidAddOnDefinitionId = Guid.NewGuid();
    private static readonly Guid ValidBuyerUserId = Guid.NewGuid();
    private static readonly Guid ValidRegistrationId = Guid.NewGuid();
    private const string ValidBuyerName = "  John Doe  ";
    private const string ValidBuyerEmail = "  John.Doe@Example.COM  ";
    private const string ValidBuyerPhone = " +94771234567 ";
    private const int ValidQuantity = 3;

    private static Money ValidUnitPrice => new Money(10.00m, Currency.USD);

    private static AddOnPurchase CreateValidStandalonePurchase(
        int quantity = ValidQuantity,
        Money? unitPrice = null)
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            quantity, unitPrice ?? ValidUnitPrice);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static AddOnPurchase CreateValidBundledPurchase()
    {
        var result = AddOnPurchase.CreateBundledWithRegistration(
            ValidEventId, ValidAddOnDefinitionId, ValidRegistrationId,
            ValidBuyerUserId, ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static AddOnPurchase CreateCompletedPurchase()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));
        purchase.CompletePayment("pi_test_456");
        return purchase;
    }

    #endregion

    #region Create() - Standalone Purchase

    [Fact]
    public void Create_WithValidData_ShouldSucceedWithCorrectDefaults()
    {
        // Act
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var purchase = result.Value;
        purchase.EventId.Should().Be(ValidEventId);
        purchase.AddOnDefinitionId.Should().Be(ValidAddOnDefinitionId);
        purchase.BuyerUserId.Should().Be(ValidBuyerUserId);
        purchase.RegistrationId.Should().BeNull("standalone purchase has no registration");
        purchase.IsBundled.Should().BeFalse("standalone purchase is not bundled");
        purchase.Status.Should().Be(AddOnPurchaseStatus.Pending);
        purchase.Quantity.Should().Be(ValidQuantity);
        purchase.StripeCheckoutSessionId.Should().BeNull();
        purchase.StripePaymentIntentId.Should().BeNull();
        purchase.CheckoutExpiresAt.Should().BeNull();
        purchase.PaymentCompletedAt.Should().BeNull();
        purchase.FailedAt.Should().BeNull();
        purchase.AbandonedAt.Should().BeNull();
        purchase.RefundedAt.Should().BeNull();
        purchase.StripeFeeAmount.Should().BeNull();
        purchase.PlatformCommissionAmount.Should().BeNull();
        purchase.OrganizerPayoutAmount.Should().BeNull();
        purchase.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCalculateTotalAmountCorrectly()
    {
        // Arrange: qty=3, unitPrice=$10 -> total=$30
        var unitPrice = new Money(10.00m, Currency.USD);

        // Act
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, null,
            3, unitPrice);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Amount.Should().Be(30.00m);
        result.Value.TotalAmount.Currency.Should().Be(Currency.USD);
        result.Value.UnitPrice.Amount.Should().Be(10.00m);
    }

    [Theory]
    [InlineData(1, 25.50, 25.50)]
    [InlineData(5, 12.00, 60.00)]
    [InlineData(10, 99.99, 999.90)]
    public void Create_ShouldCalculateTotalAmount_ForVariousQuantitiesAndPrices(
        int quantity, decimal unitPriceAmount, decimal expectedTotal)
    {
        var unitPrice = new Money(unitPriceAmount, Currency.USD);

        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            "Buyer", "buyer@test.com", null,
            quantity, unitPrice);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Amount.Should().Be(expectedTotal);
    }

    [Fact]
    public void Create_ShouldTrimNameAndLowercaseEmail()
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            "  John Doe  ", "  John.Doe@Example.COM  ", "  +123  ",
            1, ValidUnitPrice);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuyerName.Should().Be("John Doe");
        result.Value.BuyerEmail.Should().Be("john.doe@example.com");
        result.Value.BuyerPhone.Should().Be("+123");
    }

    [Fact]
    public void Create_WithNullBuyerUserId_ShouldSucceed()
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, buyerUserId: null,
            "Anonymous", "anon@test.com", null,
            1, ValidUnitPrice);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuyerUserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        var result = AddOnPurchase.Create(
            Guid.Empty, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID");
    }

    [Fact]
    public void Create_WithEmptyAddOnDefinitionId_ShouldFail()
    {
        var result = AddOnPurchase.Create(
            ValidEventId, Guid.Empty, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Add-on definition ID");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidBuyerName_ShouldFail(string? buyerName)
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            buyerName!, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Buyer name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidBuyerEmail_ShouldFail(string? buyerEmail)
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, buyerEmail!, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Buyer email");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithZeroOrNegativeQuantity_ShouldFail(int quantity)
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            quantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity");
    }

    [Fact]
    public void Create_WithNullUnitPrice_ShouldFail()
    {
        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unit price");
    }

    [Fact]
    public void Create_WithZeroUnitPrice_ShouldSucceed()
    {
        // Free add-ons ($0 price) are a legitimate business case
        var unitPrice = new Money(0m, Currency.USD);

        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, unitPrice);

        result.IsSuccess.Should().BeTrue();
        result.Value.UnitPrice.Amount.Should().Be(0m);
        result.Value.TotalAmount.Amount.Should().Be(0m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public void Create_WithNegativeUnitPrice_ShouldFail(decimal priceAmount)
    {
        var unitPrice = new Money(priceAmount, Currency.USD);

        var result = AddOnPurchase.Create(
            ValidEventId, ValidAddOnDefinitionId, ValidBuyerUserId,
            ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, unitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unit price cannot be negative");
    }

    #endregion

    #region CreateBundledWithRegistration()

    [Fact]
    public void CreateBundledWithRegistration_WithValidData_ShouldSucceedWithRegistrationLinked()
    {
        var result = AddOnPurchase.CreateBundledWithRegistration(
            ValidEventId, ValidAddOnDefinitionId, ValidRegistrationId,
            ValidBuyerUserId, ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsSuccess.Should().BeTrue();
        var purchase = result.Value;
        purchase.RegistrationId.Should().Be(ValidRegistrationId);
        purchase.IsBundled.Should().BeTrue();
        purchase.Status.Should().Be(AddOnPurchaseStatus.Pending);
    }

    [Fact]
    public void CreateBundledWithRegistration_WithEmptyRegistrationId_ShouldFail()
    {
        var result = AddOnPurchase.CreateBundledWithRegistration(
            ValidEventId, ValidAddOnDefinitionId, Guid.Empty,
            ValidBuyerUserId, ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Registration ID");
    }

    [Fact]
    public void CreateBundledWithRegistration_WithEmptyEventId_ShouldFail()
    {
        var result = AddOnPurchase.CreateBundledWithRegistration(
            Guid.Empty, ValidAddOnDefinitionId, ValidRegistrationId,
            ValidBuyerUserId, ValidBuyerName, ValidBuyerEmail, ValidBuyerPhone,
            ValidQuantity, ValidUnitPrice);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID");
    }

    #endregion

    #region SetStripeCheckoutSession()

    [Fact]
    public void SetStripeCheckoutSession_WhenPending_ShouldSucceed()
    {
        var purchase = CreateValidStandalonePurchase();
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var result = purchase.SetStripeCheckoutSession("cs_test_session_123", expiresAt);

        result.IsSuccess.Should().BeTrue();
        purchase.StripeCheckoutSessionId.Should().Be("cs_test_session_123");
        purchase.CheckoutExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void SetStripeCheckoutSession_WhenNotPending_ShouldFail()
    {
        var purchase = CreateCompletedPurchase();

        var result = purchase.SetStripeCheckoutSession("cs_test_new", DateTime.UtcNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("status");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetStripeCheckoutSession_WithInvalidSessionId_ShouldFail(string? sessionId)
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.SetStripeCheckoutSession(sessionId!, DateTime.UtcNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("session ID");
    }

    [Fact]
    public void SetStripeCheckoutSession_WithPastExpiration_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddMinutes(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("future");
    }

    #endregion

    #region CompletePayment()

    [Fact]
    public void CompletePayment_WhenPending_ShouldSetCompletedStatusAndTimestamp()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));

        var result = purchase.CompletePayment("pi_test_intent_456");

        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(AddOnPurchaseStatus.Completed);
        purchase.StripePaymentIntentId.Should().Be("pi_test_intent_456");
        purchase.PaymentCompletedAt.Should().NotBeNull();
        purchase.PaymentCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void CompletePayment_ShouldRaiseAddOnPurchaseCompletedEvent()
    {
        var purchase = CreateValidStandalonePurchase(quantity: 3);
        purchase.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));

        purchase.CompletePayment("pi_test_intent_789");

        purchase.DomainEvents.Should().ContainSingle();
        var domainEvent = purchase.DomainEvents.Single().Should().BeOfType<AddOnPurchaseCompletedEvent>().Subject;
        domainEvent.EventId.Should().Be(ValidEventId);
        domainEvent.AddOnPurchaseId.Should().Be(purchase.Id);
        domainEvent.AddOnDefinitionId.Should().Be(ValidAddOnDefinitionId);
        domainEvent.BuyerUserId.Should().Be(ValidBuyerUserId);
        domainEvent.BuyerName.Should().Be("John Doe");
        domainEvent.BuyerEmail.Should().Be("john.doe@example.com");
        domainEvent.PaymentIntentId.Should().Be("pi_test_intent_789");
        domainEvent.Quantity.Should().Be(3);
        domainEvent.UnitPrice.Should().Be(10.00m);
        domainEvent.TotalAmount.Should().Be(30.00m);
        domainEvent.Currency.Should().Be("USD");
    }

    [Fact]
    public void CompletePayment_WhenNotPending_ShouldFail()
    {
        var purchase = CreateCompletedPurchase();

        var result = purchase.CompletePayment("pi_test_another");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("status");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CompletePayment_WithInvalidPaymentIntentId_ShouldFail(string? paymentIntentId)
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.CompletePayment(paymentIntentId!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment intent ID");
    }

    #endregion

    #region MarkAsFailed()

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldSucceed()
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.MarkAsFailed();

        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(AddOnPurchaseStatus.Failed);
        purchase.FailedAt.Should().NotBeNull();
        purchase.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void MarkAsFailed_WhenCompleted_ShouldFail()
    {
        var purchase = CreateCompletedPurchase();

        var result = purchase.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyFailed_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsFailed();

        var result = purchase.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region MarkAsAbandoned()

    [Fact]
    public void MarkAsAbandoned_WhenPending_ShouldSucceed()
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.MarkAsAbandoned();

        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(AddOnPurchaseStatus.Abandoned);
        purchase.AbandonedAt.Should().NotBeNull();
        purchase.AbandonedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void MarkAsAbandoned_WhenCompleted_ShouldFail()
    {
        var purchase = CreateCompletedPurchase();

        var result = purchase.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    #endregion

    #region MarkAsRefunded()

    [Fact]
    public void MarkAsRefunded_WhenCompleted_ShouldSucceed()
    {
        var purchase = CreateCompletedPurchase();

        var result = purchase.MarkAsRefunded();

        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        purchase.RefundedAt.Should().NotBeNull();
        purchase.RefundedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRefunded_WhenPending_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public void MarkAsRefunded_WhenFailed_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsFailed();

        var result = purchase.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public void MarkAsRefunded_WhenAbandoned_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsAbandoned();

        var result = purchase.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRefunded_WhenAlreadyRefunded_ShouldFail()
    {
        var purchase = CreateCompletedPurchase();
        purchase.MarkAsRefunded();

        var result = purchase.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region SetRevenueBreakdown()

    [Fact]
    public void SetRevenueBreakdown_WhenPending_ShouldSucceed()
    {
        var purchase = CreateValidStandalonePurchase();
        var stripeFee = new Money(0.88m, Currency.USD);
        var platformCommission = new Money(1.50m, Currency.USD);
        var organizerPayout = new Money(27.62m, Currency.USD);

        var result = purchase.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);

        result.IsSuccess.Should().BeTrue();
        purchase.StripeFeeAmount.Should().Be(stripeFee);
        purchase.PlatformCommissionAmount.Should().Be(platformCommission);
        purchase.OrganizerPayoutAmount.Should().Be(organizerPayout);
    }

    [Fact]
    public void SetRevenueBreakdown_WhenCompleted_ShouldSucceed()
    {
        var purchase = CreateCompletedPurchase();
        var stripeFee = new Money(0.88m, Currency.USD);
        var platformCommission = new Money(1.50m, Currency.USD);
        var organizerPayout = new Money(27.62m, Currency.USD);

        var result = purchase.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SetRevenueBreakdown_WhenFailed_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsFailed();

        var result = purchase.SetRevenueBreakdown(
            new Money(1m, Currency.USD),
            new Money(1m, Currency.USD),
            new Money(1m, Currency.USD));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("status");
    }

    [Fact]
    public void SetRevenueBreakdown_WithNullComponents_ShouldFail()
    {
        var purchase = CreateValidStandalonePurchase();

        var result = purchase.SetRevenueBreakdown(null!, new Money(1m, Currency.USD), new Money(1m, Currency.USD));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    #endregion

    #region IsTerminal Property

    [Fact]
    public void IsTerminal_WhenPending_ShouldBeFalse()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void IsTerminal_WhenCompleted_ShouldBeTrue()
    {
        var purchase = CreateCompletedPurchase();
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenFailed_ShouldBeTrue()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsFailed();
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenAbandoned_ShouldBeTrue()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsAbandoned();
        purchase.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenRefunded_ShouldBeTrue()
    {
        var purchase = CreateCompletedPurchase();
        purchase.MarkAsRefunded();
        purchase.IsTerminal.Should().BeTrue();
    }

    #endregion

    #region IsCheckoutExpired Property

    [Fact]
    public void IsCheckoutExpired_WithNoCheckoutSession_ShouldBeFalse()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.IsCheckoutExpired.Should().BeFalse();
    }

    [Fact]
    public void IsCheckoutExpired_WithFutureExpiration_ShouldBeFalse()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddHours(24));

        purchase.IsCheckoutExpired.Should().BeFalse();
    }

    #endregion

    #region IsBundled Property

    [Fact]
    public void IsBundled_ForStandalonePurchase_ShouldBeFalse()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.IsBundled.Should().BeFalse();
    }

    [Fact]
    public void IsBundled_ForBundledPurchase_ShouldBeTrue()
    {
        var purchase = CreateValidBundledPurchase();
        purchase.IsBundled.Should().BeTrue();
    }

    #endregion

    #region Full Lifecycle Scenarios

    [Fact]
    public void FullLifecycle_PendingToCompleted_ShouldTrackAllTimestamps()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddHours(24));
        purchase.CompletePayment("pi_test");

        purchase.Status.Should().Be(AddOnPurchaseStatus.Completed);
        purchase.PaymentCompletedAt.Should().NotBeNull();
        purchase.FailedAt.Should().BeNull();
        purchase.AbandonedAt.Should().BeNull();
        purchase.RefundedAt.Should().BeNull();
    }

    [Fact]
    public void FullLifecycle_CompletedToRefunded_ShouldTrackAllTimestamps()
    {
        var purchase = CreateCompletedPurchase();
        purchase.MarkAsRefunded();

        purchase.Status.Should().Be(AddOnPurchaseStatus.Refunded);
        purchase.PaymentCompletedAt.Should().NotBeNull();
        purchase.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullLifecycle_PendingToFailed_ShouldTrackTimestamp()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsFailed();

        purchase.Status.Should().Be(AddOnPurchaseStatus.Failed);
        purchase.FailedAt.Should().NotBeNull();
        purchase.PaymentCompletedAt.Should().BeNull();
    }

    [Fact]
    public void FullLifecycle_PendingToAbandoned_ShouldTrackTimestamp()
    {
        var purchase = CreateValidStandalonePurchase();
        purchase.MarkAsAbandoned();

        purchase.Status.Should().Be(AddOnPurchaseStatus.Abandoned);
        purchase.AbandonedAt.Should().NotBeNull();
        purchase.PaymentCompletedAt.Should().BeNull();
    }

    #endregion
}
