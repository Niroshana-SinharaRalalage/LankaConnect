using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Comprehensive unit tests for the Sponsor entity (dual-mode: Money + Item).
/// Covers creation, validation, state transitions, domain events, and type guards.
/// </summary>
public class SponsorTests
{
    #region Test Helpers

    private static readonly Guid ValidEventId = Guid.NewGuid();
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidName = "  John Doe  ";
    private const string ValidEmail = "  John.Doe@Example.COM  ";
    private const string ValidPhone = "  +1-555-1234  ";
    private const string ValidOrganization = "  Acme Corp  ";
    private const string ValidNotes = "  Happy to help  ";
    private const string ValidItemName = "  Projector  ";
    private const string ValidItemDescription = "  HD projector for presentations  ";

    private static Money CreateMoney(decimal amount = 100m, Currency currency = Currency.USD)
    {
        return Money.Create(amount, currency).Value;
    }

    private static Sponsor CreateValidMoneySponsor(
        Guid? eventId = null,
        Money? amount = null,
        SponsorStatus? desiredStatus = null)
    {
        var result = Sponsor.CreateMoneySponsor(
            eventId ?? ValidEventId,
            ValidUserId,
            ValidName,
            ValidEmail,
            ValidPhone,
            ValidOrganization,
            ValidNotes,
            amount ?? CreateMoney());

        result.IsSuccess.Should().BeTrue();
        var sponsor = result.Value;

        // Advance to desired status if needed
        if (desiredStatus == SponsorStatus.Completed)
        {
            sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));
            sponsor.CompletePayment("pi_test_456");
        }

        return sponsor;
    }

    private static Sponsor CreateValidItemSponsor(
        Guid? eventId = null,
        decimal? estimatedValue = null)
    {
        var result = Sponsor.CreateItemSponsor(
            eventId ?? ValidEventId,
            ValidUserId,
            ValidName,
            ValidEmail,
            ValidPhone,
            ValidOrganization,
            ValidNotes,
            ValidItemName,
            ValidItemDescription,
            estimatedValue ?? 500m);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    #endregion

    #region Money Sponsor Creation Tests

    [Fact]
    public void CreateMoneySponsor_WithValidData_ShouldSucceed()
    {
        // Arrange
        var amount = CreateMoney(250m, Currency.USD);

        // Act
        var result = Sponsor.CreateMoneySponsor(
            ValidEventId,
            ValidUserId,
            ValidName,
            ValidEmail,
            ValidPhone,
            ValidOrganization,
            ValidNotes,
            amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sponsor = result.Value;
        sponsor.EventId.Should().Be(ValidEventId);
        sponsor.SponsorUserId.Should().Be(ValidUserId);
        sponsor.Type.Should().Be(SponsorType.Money);
        sponsor.Status.Should().Be(SponsorStatus.Pending);
        sponsor.Amount.Should().Be(amount);
        sponsor.DomainEvents.Should().BeEmpty("money sponsors do not raise events on creation");
    }

    [Fact]
    public void CreateMoneySponsor_ShouldTrimName_AndLowercaseEmail()
    {
        // Act
        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            CreateMoney());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sponsor = result.Value;
        sponsor.SponsorName.Should().Be("John Doe");
        sponsor.SponsorEmail.Should().Be("john.doe@example.com");
        sponsor.SponsorPhone.Should().Be("+1-555-1234");
        sponsor.SponsorOrganization.Should().Be("Acme Corp");
        sponsor.SponsorNotes.Should().Be("Happy to help");
    }

    [Fact]
    public void CreateMoneySponsor_WithNullOptionalFields_ShouldSucceed()
    {
        // Act
        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, null,
            "Jane", "jane@test.com", null, null, null,
            CreateMoney());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sponsor = result.Value;
        sponsor.SponsorUserId.Should().BeNull();
        sponsor.SponsorPhone.Should().BeNull();
        sponsor.SponsorOrganization.Should().BeNull();
        sponsor.SponsorNotes.Should().BeNull();
    }

    [Fact]
    public void CreateMoneySponsor_WithEmptyEventId_ShouldFail()
    {
        var result = Sponsor.CreateMoneySponsor(
            Guid.Empty, ValidUserId,
            ValidName, ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            CreateMoney());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    [Fact]
    public void CreateMoneySponsor_WithEmptyName_ShouldFail()
    {
        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, ValidUserId,
            "", ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            CreateMoney());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsor name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateMoneySponsor_WithEmptyOrWhitespaceEmail_ShouldFail(string? email)
    {
        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, ValidUserId,
            ValidName, email!, ValidPhone, ValidOrganization, ValidNotes,
            CreateMoney());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsor email is required");
    }

    [Fact]
    public void CreateMoneySponsor_WithNullAmount_ShouldFail()
    {
        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsorship amount is required");
    }

    [Fact]
    public void CreateMoneySponsor_WithZeroAmount_ShouldFail()
    {
        var money = CreateMoney(0m);

        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            money);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsorship amount must be greater than zero");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void CreateMoneySponsor_WithNegativeAmount_ShouldFail(decimal amount)
    {
        // Money.Create rejects negative amounts, so use constructor directly
        var money = new Money(amount, Currency.USD);

        var result = Sponsor.CreateMoneySponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            money);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsorship amount must be greater than zero");
    }

    #endregion

    #region Item Sponsor Creation Tests

    [Fact]
    public void CreateItemSponsor_WithValidData_ShouldSucceed()
    {
        // Act
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, ValidPhone, ValidOrganization, ValidNotes,
            ValidItemName, ValidItemDescription, 500m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sponsor = result.Value;
        sponsor.EventId.Should().Be(ValidEventId);
        sponsor.Type.Should().Be(SponsorType.Item);
        sponsor.Status.Should().Be(SponsorStatus.RecordedItem);
        sponsor.ItemName.Should().Be("Projector");
        sponsor.ItemDescription.Should().Be("HD projector for presentations");
        sponsor.EstimatedValue.Should().Be(500m);
        sponsor.RecordedAt.Should().NotBeNull();
        sponsor.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateItemSponsor_ShouldRaiseItemSponsorRecordedEvent()
    {
        // Act
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            "Jane Doe", "jane@test.com", null, "ACME", null,
            "Projector", "HD model", 750m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sponsor = result.Value;
        sponsor.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ItemSponsorRecordedEvent>();

        var evt = (ItemSponsorRecordedEvent)sponsor.DomainEvents.Single();
        evt.EventId.Should().Be(ValidEventId);
        evt.SponsorId.Should().Be(sponsor.Id);
        evt.SponsorUserId.Should().Be(ValidUserId);
        evt.SponsorName.Should().Be("Jane Doe");
        evt.SponsorEmail.Should().Be("jane@test.com");
        evt.SponsorOrganization.Should().Be("ACME");
        evt.ItemName.Should().Be("Projector");
        evt.ItemDescription.Should().Be("HD model");
        evt.EstimatedValue.Should().Be(750m);
        evt.RecordedAt.Should().Be(sponsor.RecordedAt!.Value);
    }

    [Fact]
    public void CreateItemSponsor_WithEmptyItemName_ShouldFail()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, null, null, null,
            "", null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Item name is required");
    }

    [Fact]
    public void CreateItemSponsor_WithWhitespaceItemName_ShouldFail()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, null, null, null,
            "   ", null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Item name is required");
    }

    [Fact]
    public void CreateItemSponsor_WithNegativeEstimatedValue_ShouldFail()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, null, null, null,
            "Projector", null, -1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Estimated value cannot be negative");
    }

    [Fact]
    public void CreateItemSponsor_WithZeroEstimatedValue_ShouldSucceed()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, null, null, null,
            "Free venue", null, 0m);

        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedValue.Should().Be(0m);
    }

    [Fact]
    public void CreateItemSponsor_WithNullEstimatedValue_ShouldSucceed()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, ValidEmail, null, null, null,
            "Some item", null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedValue.Should().BeNull();
    }

    [Fact]
    public void CreateItemSponsor_WithEmptyEventId_ShouldFail()
    {
        var result = Sponsor.CreateItemSponsor(
            Guid.Empty, ValidUserId,
            ValidName, ValidEmail, null, null, null,
            ValidItemName, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    [Fact]
    public void CreateItemSponsor_WithEmptyName_ShouldFail()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            "", ValidEmail, null, null, null,
            ValidItemName, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsor name is required");
    }

    [Fact]
    public void CreateItemSponsor_WithEmptyEmail_ShouldFail()
    {
        var result = Sponsor.CreateItemSponsor(
            ValidEventId, ValidUserId,
            ValidName, "", null, null, null,
            ValidItemName, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sponsor email is required");
    }

    #endregion

    #region SetStripeCheckoutSession Tests

    [Fact]
    public void SetStripeCheckoutSession_OnPendingMoneySponsor_ShouldSucceed()
    {
        // Arrange
        var sponsor = CreateValidMoneySponsor();
        var sessionId = "cs_test_abc123";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        var result = sponsor.SetStripeCheckoutSession(sessionId, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sponsor.StripeCheckoutSessionId.Should().Be(sessionId);
        sponsor.CheckoutExpiresAt.Should().Be(expiresAt);
        sponsor.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetStripeCheckoutSession_OnItemSponsor_ShouldFail()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot set checkout session for item-based sponsors");
    }

    [Fact]
    public void SetStripeCheckoutSession_WithEmptySessionId_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetStripeCheckoutSession("", DateTime.UtcNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Checkout session ID is required");
    }

    [Fact]
    public void SetStripeCheckoutSession_WithPastExpiration_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddMinutes(-5));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Expiration time must be in the future");
    }

    #endregion

    #region CompletePayment Tests

    [Fact]
    public void CompletePayment_OnPendingMoneySponsor_ShouldSucceed()
    {
        // Arrange
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));

        // Act
        var result = sponsor.CompletePayment("pi_test_intent_456");

        // Assert
        result.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Completed);
        sponsor.StripePaymentIntentId.Should().Be("pi_test_intent_456");
        sponsor.PaymentCompletedAt.Should().NotBeNull();
        sponsor.PaymentCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CompletePayment_ShouldRaiseSponsorPaymentCompletedEvent()
    {
        // Arrange
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(1));

        // Act
        sponsor.CompletePayment("pi_test_intent_456");

        // Assert
        sponsor.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SponsorPaymentCompletedEvent>();

        var evt = (SponsorPaymentCompletedEvent)sponsor.DomainEvents.Single();
        evt.EventId.Should().Be(sponsor.EventId);
        evt.SponsorId.Should().Be(sponsor.Id);
        evt.PaymentIntentId.Should().Be("pi_test_intent_456");
        evt.Amount.Should().Be(100m);
        evt.Currency.Should().Be("USD");
        evt.PaymentCompletedAt.Should().Be(sponsor.PaymentCompletedAt!.Value);
    }

    [Fact]
    public void CompletePayment_OnItemSponsor_ShouldFail()
    {
        var sponsor = CreateValidItemSponsor();
        sponsor.ClearDomainEvents(); // clear creation event

        var result = sponsor.CompletePayment("pi_test_123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot complete payment for item-based sponsors");
    }

    [Fact]
    public void CompletePayment_WithEmptyPaymentIntentId_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.CompletePayment("");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment intent ID is required");
    }

    [Fact]
    public void CompletePayment_OnCompletedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);
        sponsor.ClearDomainEvents();

        var result = sponsor.CompletePayment("pi_another");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot complete payment when status is Completed");
    }

    #endregion

    #region MarkAsFailed Tests

    [Fact]
    public void MarkAsFailed_OnPendingMoneySponsor_ShouldSucceed()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.MarkAsFailed();

        result.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Failed);
        sponsor.FailedAt.Should().NotBeNull();
        sponsor.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsFailed_OnItemSponsor_ShouldFail()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot mark item-based sponsors as failed");
    }

    [Fact]
    public void MarkAsFailed_OnCompletedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot mark as failed when status is Completed");
    }

    #endregion

    #region MarkAsAbandoned Tests

    [Fact]
    public void MarkAsAbandoned_OnPendingMoneySponsor_ShouldSucceed()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.MarkAsAbandoned();

        result.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Abandoned);
        sponsor.AbandonedAt.Should().NotBeNull();
        sponsor.AbandonedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsAbandoned_OnItemSponsor_ShouldFail()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot mark item-based sponsors as abandoned");
    }

    [Fact]
    public void MarkAsAbandoned_OnCompletedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot mark as abandoned when status is Completed");
    }

    #endregion

    #region MarkAsRefunded Tests

    [Fact]
    public void MarkAsRefunded_OnCompletedMoneySponsor_ShouldSucceed()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.MarkAsRefunded();

        result.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Refunded);
        sponsor.RefundedAt.Should().NotBeNull();
        sponsor.RefundedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsRefunded_OnItemSponsor_ShouldFail()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot refund item-based sponsors");
    }

    [Fact]
    public void MarkAsRefunded_OnPendingSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot refund when status is Pending");
    }

    #endregion

    #region SetRevenueBreakdown Tests

    [Fact]
    public void SetRevenueBreakdown_OnPendingMoneySponsor_ShouldSucceed()
    {
        var sponsor = CreateValidMoneySponsor();
        var stripeFee = CreateMoney(2.90m);
        var platformCommission = CreateMoney(5m);
        var organizerPayout = CreateMoney(92.10m);

        var result = sponsor.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);

        result.IsSuccess.Should().BeTrue();
        sponsor.StripeFeeAmount.Should().Be(stripeFee);
        sponsor.PlatformCommissionAmount.Should().Be(platformCommission);
        sponsor.OrganizerPayoutAmount.Should().Be(organizerPayout);
    }

    [Fact]
    public void SetRevenueBreakdown_OnCompletedMoneySponsor_ShouldSucceed()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);
        var stripeFee = CreateMoney(2.90m);
        var platformCommission = CreateMoney(5m);
        var organizerPayout = CreateMoney(92.10m);

        var result = sponsor.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SetRevenueBreakdown_OnItemSponsor_ShouldFail()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.SetRevenueBreakdown(
            CreateMoney(1m), CreateMoney(2m), CreateMoney(3m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot set revenue breakdown for item-based sponsors");
    }

    [Fact]
    public void SetRevenueBreakdown_OnFailedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.MarkAsFailed();

        var result = sponsor.SetRevenueBreakdown(
            CreateMoney(1m), CreateMoney(2m), CreateMoney(3m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot set revenue breakdown when status is Failed");
    }

    [Fact]
    public void SetRevenueBreakdown_WithNullComponents_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetRevenueBreakdown(null!, CreateMoney(2m), CreateMoney(3m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("All revenue breakdown components are required");
    }

    #endregion

    #region IsTerminal Property Tests

    [Theory]
    [InlineData(SponsorStatus.Completed, true)]
    [InlineData(SponsorStatus.Failed, true)]
    [InlineData(SponsorStatus.Abandoned, true)]
    [InlineData(SponsorStatus.Refunded, true)]
    [InlineData(SponsorStatus.Pending, false)]
    public void IsTerminal_MoneySponsor_ShouldReturnCorrectValue(SponsorStatus targetStatus, bool expectedTerminal)
    {
        // Arrange
        var sponsor = CreateValidMoneySponsor();

        // Transition to target status
        switch (targetStatus)
        {
            case SponsorStatus.Completed:
                sponsor.SetStripeCheckoutSession("cs_123", DateTime.UtcNow.AddHours(1));
                sponsor.CompletePayment("pi_123");
                break;
            case SponsorStatus.Failed:
                sponsor.MarkAsFailed();
                break;
            case SponsorStatus.Abandoned:
                sponsor.MarkAsAbandoned();
                break;
            case SponsorStatus.Refunded:
                sponsor.SetStripeCheckoutSession("cs_123", DateTime.UtcNow.AddHours(1));
                sponsor.CompletePayment("pi_123");
                sponsor.MarkAsRefunded();
                break;
            case SponsorStatus.Pending:
                // Already pending
                break;
        }

        // Assert
        sponsor.IsTerminal.Should().Be(expectedTerminal);
    }

    [Fact]
    public void IsTerminal_RecordedItemSponsor_ShouldBeTrue()
    {
        var sponsor = CreateValidItemSponsor();

        sponsor.IsTerminal.Should().BeTrue("RecordedItem is a terminal state");
    }

    #endregion

    #region IsMoneyBased / IsItemBased Property Tests

    [Fact]
    public void IsMoneyBased_OnMoneySponsor_ShouldBeTrue()
    {
        var sponsor = CreateValidMoneySponsor();

        sponsor.IsMoneyBased.Should().BeTrue();
        sponsor.IsItemBased.Should().BeFalse();
    }

    [Fact]
    public void IsItemBased_OnItemSponsor_ShouldBeTrue()
    {
        var sponsor = CreateValidItemSponsor();

        sponsor.IsItemBased.Should().BeTrue();
        sponsor.IsMoneyBased.Should().BeFalse();
    }

    #endregion

    #region IsCheckoutExpired Property Tests

    [Fact]
    public void IsCheckoutExpired_WithNoCheckoutSession_ShouldBeFalse()
    {
        var sponsor = CreateValidMoneySponsor();

        sponsor.IsCheckoutExpired.Should().BeFalse();
    }

    [Fact]
    public void IsCheckoutExpired_WithFutureExpiration_ShouldBeFalse()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(24));

        sponsor.IsCheckoutExpired.Should().BeFalse();
    }

    [Fact]
    public void IsCheckoutExpired_WithPastExpiration_ShouldBeTrue()
    {
        var sponsor = CreateValidMoneySponsor();
        // Set session with a future time first (required by validation)
        sponsor.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddMilliseconds(1));

        // Wait briefly so the expiration is in the past
        System.Threading.Thread.Sleep(10);

        sponsor.IsCheckoutExpired.Should().BeTrue();
    }

    #endregion

    #region Type Guard Tests - All Stripe Methods Reject Item Sponsors

    [Fact]
    public void AllStripeMethods_OnItemSponsor_ShouldFail()
    {
        // Arrange
        var sponsor = CreateValidItemSponsor();
        sponsor.ClearDomainEvents();

        // Act & Assert - SetStripeCheckoutSession
        var checkoutResult = sponsor.SetStripeCheckoutSession("cs_123", DateTime.UtcNow.AddHours(1));
        checkoutResult.IsFailure.Should().BeTrue();
        checkoutResult.Error.Should().Contain("Cannot set checkout session for item-based sponsors");

        // Act & Assert - CompletePayment
        var paymentResult = sponsor.CompletePayment("pi_123");
        paymentResult.IsFailure.Should().BeTrue();
        paymentResult.Error.Should().Contain("Cannot complete payment for item-based sponsors");

        // Act & Assert - MarkAsFailed
        var failedResult = sponsor.MarkAsFailed();
        failedResult.IsFailure.Should().BeTrue();
        failedResult.Error.Should().Contain("Cannot mark item-based sponsors as failed");

        // Act & Assert - MarkAsAbandoned
        var abandonedResult = sponsor.MarkAsAbandoned();
        abandonedResult.IsFailure.Should().BeTrue();
        abandonedResult.Error.Should().Contain("Cannot mark item-based sponsors as abandoned");

        // Act & Assert - MarkAsRefunded
        var refundedResult = sponsor.MarkAsRefunded();
        refundedResult.IsFailure.Should().BeTrue();
        refundedResult.Error.Should().Contain("Cannot refund item-based sponsors");

        // Act & Assert - SetRevenueBreakdown
        var revenueResult = sponsor.SetRevenueBreakdown(
            CreateMoney(1m), CreateMoney(2m), CreateMoney(3m));
        revenueResult.IsFailure.Should().BeTrue();
        revenueResult.Error.Should().Contain("Cannot set revenue breakdown for item-based sponsors");
    }

    #endregion

    #region State Transition Guards - Invalid Transitions

    [Fact]
    public void SetStripeCheckoutSession_OnCompletedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.SetStripeCheckoutSession("cs_new", DateTime.UtcNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot set checkout session when status is Completed");
    }

    [Fact]
    public void SetStripeCheckoutSession_OnFailedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.MarkAsFailed();

        var result = sponsor.SetStripeCheckoutSession("cs_new", DateTime.UtcNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot set checkout session when status is Failed");
    }

    [Fact]
    public void CompletePayment_OnFailedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.MarkAsFailed();

        var result = sponsor.CompletePayment("pi_123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot complete payment when status is Failed");
    }

    [Fact]
    public void MarkAsFailed_OnAbandonedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.MarkAsAbandoned();

        var result = sponsor.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot mark as failed when status is Abandoned");
    }

    [Fact]
    public void MarkAsRefunded_OnAbandonedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.MarkAsAbandoned();

        var result = sponsor.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot refund when status is Abandoned");
    }

    [Fact]
    public void MarkAsRefunded_OnFailedSponsor_ShouldFail()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.MarkAsFailed();

        var result = sponsor.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot refund when status is Failed");
    }

    #endregion

    #region LegacyBaseEntity Properties Tests

    [Fact]
    public void CreateMoneySponsor_ShouldSetIdAndCreatedAt()
    {
        var sponsor = CreateValidMoneySponsor();

        sponsor.Id.Should().NotBe(Guid.Empty);
        sponsor.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        sponsor.UpdatedAt.Should().BeNull("no mutations yet");
    }

    [Fact]
    public void CreateItemSponsor_ShouldSetIdAndCreatedAt()
    {
        var sponsor = CreateValidItemSponsor();

        sponsor.Id.Should().NotBe(Guid.Empty);
        sponsor.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MoneySponsor_AmountFieldsNullForNewSponsor()
    {
        var sponsor = CreateValidMoneySponsor();

        sponsor.StripeCheckoutSessionId.Should().BeNull();
        sponsor.StripePaymentIntentId.Should().BeNull();
        sponsor.CheckoutExpiresAt.Should().BeNull();
        sponsor.StripeFeeAmount.Should().BeNull();
        sponsor.PlatformCommissionAmount.Should().BeNull();
        sponsor.OrganizerPayoutAmount.Should().BeNull();
        sponsor.PaymentCompletedAt.Should().BeNull();
        sponsor.FailedAt.Should().BeNull();
        sponsor.AbandonedAt.Should().BeNull();
        sponsor.RefundedAt.Should().BeNull();
    }

    [Fact]
    public void ItemSponsor_MoneyFieldsShouldBeNull()
    {
        var sponsor = CreateValidItemSponsor();

        sponsor.Amount.Should().BeNull();
        sponsor.StripeCheckoutSessionId.Should().BeNull();
        sponsor.StripePaymentIntentId.Should().BeNull();
        sponsor.CheckoutExpiresAt.Should().BeNull();
    }

    #endregion

    #region Full Lifecycle Integration Tests

    [Fact]
    public void MoneySponsor_FullLifecycle_PendingToCompletedToRefunded()
    {
        // Create
        var sponsor = CreateValidMoneySponsor();
        sponsor.Status.Should().Be(SponsorStatus.Pending);
        sponsor.IsTerminal.Should().BeFalse();

        // Set checkout session
        var checkoutResult = sponsor.SetStripeCheckoutSession("cs_lifecycle", DateTime.UtcNow.AddHours(24));
        checkoutResult.IsSuccess.Should().BeTrue();

        // Complete payment
        var paymentResult = sponsor.CompletePayment("pi_lifecycle");
        paymentResult.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Completed);
        sponsor.IsTerminal.Should().BeTrue();

        // Set revenue breakdown
        var revenueResult = sponsor.SetRevenueBreakdown(
            CreateMoney(2.90m), CreateMoney(5m), CreateMoney(92.10m));
        revenueResult.IsSuccess.Should().BeTrue();

        // Refund
        var refundResult = sponsor.MarkAsRefunded();
        refundResult.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Refunded);
        sponsor.IsTerminal.Should().BeTrue();

        // Verify domain events (CompletePayment raised one)
        sponsor.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SponsorPaymentCompletedEvent>();
    }

    [Fact]
    public void MoneySponsor_FullLifecycle_PendingToFailed()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_fail", DateTime.UtcNow.AddHours(1));

        var result = sponsor.MarkAsFailed();

        result.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Failed);
        sponsor.IsTerminal.Should().BeTrue();
        sponsor.FailedAt.Should().NotBeNull();
        sponsor.DomainEvents.Should().BeEmpty("no domain events for failure");
    }

    [Fact]
    public void MoneySponsor_FullLifecycle_PendingToAbandoned()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_abandon", DateTime.UtcNow.AddHours(1));

        var result = sponsor.MarkAsAbandoned();

        result.IsSuccess.Should().BeTrue();
        sponsor.Status.Should().Be(SponsorStatus.Abandoned);
        sponsor.IsTerminal.Should().BeTrue();
        sponsor.AbandonedAt.Should().NotBeNull();
        sponsor.DomainEvents.Should().BeEmpty("no domain events for abandonment");
    }

    [Fact]
    public void ItemSponsor_FullLifecycle_CreatedAsRecordedItem()
    {
        var sponsor = CreateValidItemSponsor();

        // Immediately terminal
        sponsor.Status.Should().Be(SponsorStatus.RecordedItem);
        sponsor.IsTerminal.Should().BeTrue();
        sponsor.RecordedAt.Should().NotBeNull();

        // One domain event on creation
        sponsor.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ItemSponsorRecordedEvent>();

        // All Stripe transitions blocked
        sponsor.CompletePayment("pi_x").IsFailure.Should().BeTrue();
        sponsor.MarkAsFailed().IsFailure.Should().BeTrue();
        sponsor.MarkAsAbandoned().IsFailure.Should().BeTrue();
        sponsor.MarkAsRefunded().IsFailure.Should().BeTrue();
    }

    #endregion

    #region Phase 6A.145 — Image methods (SetImage / ClearImage)

    [Fact]
    public void NewSponsor_HasNullImageFields()
    {
        var moneySponsor = CreateValidMoneySponsor();
        moneySponsor.ImageUrl.Should().BeNull();
        moneySponsor.ImageBlobName.Should().BeNull();

        var itemSponsor = CreateValidItemSponsor();
        itemSponsor.ImageUrl.Should().BeNull();
        itemSponsor.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void SetImage_WithValidUrlAndBlobName_Succeeds_OnMoneySponsor()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetImage(
            "https://blob.example.com/sponsors/abc.png",
            "abc_papa-johns-logo.png");

        result.IsSuccess.Should().BeTrue();
        sponsor.ImageUrl.Should().Be("https://blob.example.com/sponsors/abc.png");
        sponsor.ImageBlobName.Should().Be("abc_papa-johns-logo.png");
    }

    [Fact]
    public void SetImage_WithValidUrlAndBlobName_Succeeds_OnItemSponsor()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.SetImage(
            "https://blob.example.com/sponsors/item.png",
            "item_logo.png");

        result.IsSuccess.Should().BeTrue();
        sponsor.ImageUrl.Should().Be("https://blob.example.com/sponsors/item.png");
        sponsor.ImageBlobName.Should().Be("item_logo.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetImage_WithEmptyUrl_Fails(string? badUrl)
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetImage(badUrl!, "blob.png");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("URL");
        sponsor.ImageUrl.Should().BeNull("rejected SetImage must NOT mutate the entity");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetImage_WithEmptyBlobName_Fails(string? badBlobName)
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetImage("https://blob.example.com/x.png", badBlobName!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("blob");
        sponsor.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void SetImage_ReplacingExisting_OverwritesBoth()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetImage("https://blob.example.com/old.png", "old_pic.png");

        sponsor.SetImage("https://blob.example.com/new.png", "new_pic.png");

        sponsor.ImageUrl.Should().Be("https://blob.example.com/new.png");
        sponsor.ImageBlobName.Should().Be("new_pic.png");
    }

    [Fact]
    public void SetImage_TrimsWhitespaceFromBothFields()
    {
        var sponsor = CreateValidMoneySponsor();

        sponsor.SetImage("  https://blob.example.com/x.png  ", "  x_blob.png  ");

        sponsor.ImageUrl.Should().Be("https://blob.example.com/x.png");
        sponsor.ImageBlobName.Should().Be("x_blob.png");
    }

    [Fact]
    public void SetImage_PreservesOtherFields_OnMoneySponsor()
    {
        var sponsor = CreateValidMoneySponsor();
        var originalAmount = sponsor.Amount;
        var originalName = sponsor.SponsorName;
        var originalStatus = sponsor.Status;

        sponsor.SetImage("https://blob.example.com/x.png", "x.png");

        sponsor.Amount.Should().Be(originalAmount);
        sponsor.SponsorName.Should().Be(originalName);
        sponsor.Status.Should().Be(originalStatus);
    }

    [Fact]
    public void ClearImage_RemovesBothFields()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetImage("https://blob.example.com/x.png", "x.png");

        var result = sponsor.ClearImage();

        result.IsSuccess.Should().BeTrue();
        sponsor.ImageUrl.Should().BeNull();
        sponsor.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void ClearImage_WhenNoImage_IsIdempotent()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.ImageUrl.Should().BeNull();

        var result = sponsor.ClearImage();

        result.IsSuccess.Should().BeTrue();
        sponsor.ImageUrl.Should().BeNull();
        sponsor.ImageBlobName.Should().BeNull();
    }

    [Fact]
    public void SetImage_OnPendingMoneySponsor_Succeeds()
    {
        // Architect F-decision: image upload is allowed BEFORE Stripe checkout for
        // money sponsors. Orphan-blob cleanup hooks fire on MarkAsFailed/Abandoned.
        var sponsor = CreateValidMoneySponsor();
        sponsor.Status.Should().Be(SponsorStatus.Pending);

        var result = sponsor.SetImage("https://blob.example.com/x.png", "x.png");

        result.IsSuccess.Should().BeTrue();
        sponsor.ImageUrl.Should().NotBeNull();
    }

    [Fact]
    public void SetImage_OnCompletedMoneySponsor_Succeeds()
    {
        // Organizer override: image can be set even after Stripe completion.
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.SetImage("https://blob.example.com/x.png", "x.png");

        result.IsSuccess.Should().BeTrue();
        sponsor.ImageUrl.Should().NotBeNull();
    }

    #endregion

    #region Phase 6A.162 — Brochure methods (SetBrochure / ClearBrochure)

    /// <summary>
    /// Phase 6A.162 — Sponsor gains a SECOND optional image slot for a
    /// brochure/flyer alongside the existing logo. Architect Option C: keep
    /// existing SetImage/ClearImage verbatim (semantically the "logo"), add
    /// SIBLING SetBrochure/ClearBrochure methods that mirror line-for-line.
    /// Independence invariant: each slot is orthogonal — touching one MUST
    /// NOT mutate the other.
    /// </summary>

    [Fact]
    public void SetBrochure_WithValidUrlAndBlobName_Succeeds()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetBrochure(
            "https://blob.example.com/brochure.png",
            "brochure_blob.png");

        result.IsSuccess.Should().BeTrue();
        sponsor.BrochureUrl.Should().Be("https://blob.example.com/brochure.png");
        sponsor.BrochureBlobName.Should().Be("brochure_blob.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetBrochure_WithEmptyUrl_Fails(string? badUrl)
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetBrochure(badUrl!, "brochure.png");

        result.IsSuccess.Should().BeFalse();
        sponsor.BrochureUrl.Should().BeNull("rejected SetBrochure must NOT mutate the entity");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetBrochure_WithEmptyBlobName_Fails(string? badBlobName)
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.SetBrochure("https://blob.example.com/x.png", badBlobName!);

        result.IsSuccess.Should().BeFalse();
        sponsor.BrochureUrl.Should().BeNull();
    }

    [Fact]
    public void SetBrochure_ReplacingExisting_OverwritesBoth()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetBrochure("https://blob.example.com/old.png", "old_brochure.png");

        sponsor.SetBrochure("https://blob.example.com/new.png", "new_brochure.png");

        sponsor.BrochureUrl.Should().Be("https://blob.example.com/new.png");
        sponsor.BrochureBlobName.Should().Be("new_brochure.png");
    }

    [Fact]
    public void ClearBrochure_RemovesBothFields()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetBrochure("https://blob.example.com/x.png", "x.png");

        var result = sponsor.ClearBrochure();

        result.IsSuccess.Should().BeTrue();
        sponsor.BrochureUrl.Should().BeNull();
        sponsor.BrochureBlobName.Should().BeNull();
    }

    [Fact]
    public void ClearBrochure_WhenNoBrochure_IsIdempotent()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.BrochureUrl.Should().BeNull();

        var result = sponsor.ClearBrochure();

        result.IsSuccess.Should().BeTrue();
        sponsor.BrochureUrl.Should().BeNull();
        sponsor.BrochureBlobName.Should().BeNull();
    }

    /// <summary>
    /// Phase 6A.162 — independence invariant #1: SetBrochure MUST NOT touch
    /// the logo slot (ImageUrl/ImageBlobName). Catches the bug class where a
    /// future refactor collapses the two slots into one method by accident.
    /// </summary>
    [Fact]
    public void SetBrochure_DoesNotMutateImageSlot()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetImage("https://blob.example.com/logo.png", "logo.png");

        sponsor.SetBrochure("https://blob.example.com/brochure.png", "brochure.png");

        sponsor.ImageUrl.Should().Be("https://blob.example.com/logo.png", "logo must survive brochure set");
        sponsor.ImageBlobName.Should().Be("logo.png");
    }

    /// <summary>
    /// Phase 6A.162 — independence invariant #2: ClearImage MUST NOT touch
    /// the brochure slot. Mirror of the SetBrochure-doesnt-touch-image
    /// invariant. Same bug class.
    /// </summary>
    [Fact]
    public void ClearImage_DoesNotMutateBrochureSlot()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetImage("https://blob.example.com/logo.png", "logo.png");
        sponsor.SetBrochure("https://blob.example.com/brochure.png", "brochure.png");

        sponsor.ClearImage();

        sponsor.ImageUrl.Should().BeNull();
        sponsor.BrochureUrl.Should().Be("https://blob.example.com/brochure.png", "brochure must survive logo clear");
        sponsor.BrochureBlobName.Should().Be("brochure.png");
    }

    #endregion

    #region Phase 6A.151 — Edit Existing Sponsorship (RED tests)

    /// <summary>
    /// Phase 6A.151 — granular content-edit mutators on the Sponsor aggregate.
    /// State-matrix enforcement lives inside each mutator. Audit field
    /// `LastEditedBy` is set on every successful mutation (distinct from
    /// `UpdatedAt` which fires on every state change including lifecycle).
    ///
    /// State matrix (final, post-architect-stress-test):
    ///                            | Notes | Org | Amount | Item* | Image | Name |
    ///   Pending Money            |  ✅   |  ✅ |   🚫   |  n/a  |  ✅   |  ✅  |
    ///   Completed Stripe         |  ✅   |  ✅ |   🚫   |  n/a  |  ✅   |  ✅  |
    ///   Completed off-platform   |  ✅   |  ✅ |   👤   |  n/a  |  ✅   |  ✅  |
    ///   RecordedItem             |  ✅   |  ✅ |  n/a   |  ✅   |  ✅   |  ✅  |
    ///   Failed/Abandoned/Refunded|👤notes|  🚫 |   🚫   |  🚫   |  🚫   |  🚫  |
    ///   (✅ both / 👤 organizer-only / 🚫 disallowed)
    /// </summary>

    private static readonly Guid SponsorActorId = Guid.NewGuid();
    private static readonly Guid OrganizerActorId = Guid.NewGuid();

    private static Sponsor CreateOffPlatformCompletedSponsor()
    {
        // Off-platform money sponsor: Pending → CompleteAsOrganizerCash → Completed
        // with no StripeCheckoutSessionId (the implicit discriminator).
        var sponsor = CreateValidMoneySponsor();
        var result = sponsor.CompleteAsOrganizerCash();
        result.IsSuccess.Should().BeTrue();
        return sponsor;
    }

    private static Sponsor CreateFailedSponsor()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddHours(1));
        sponsor.MarkAsFailed();
        return sponsor;
    }

    private static Sponsor CreateAbandonedSponsor()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddMinutes(-5));
        sponsor.MarkAsAbandoned();
        return sponsor;
    }

    private static Sponsor CreateRefundedSponsor()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);
        sponsor.MarkAsRefunded();
        return sponsor;
    }

    // ---------- UpdateContactFields ----------

    [Fact]
    public void UpdateContactFields_OnPendingMoney_BothFields_Succeeds()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.UpdateContactFields(
            notes: "  Updated note  ",
            organization: "  NewCo  ",
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
        sponsor.SponsorNotes.Should().Be("Updated note");
        sponsor.SponsorOrganization.Should().Be("NewCo");
        sponsor.LastEditedBy.Should().Be(SponsorActorId);
        sponsor.LastEditedAt.Should().NotBeNull();
        sponsor.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContactFields_OnPendingMoney_NotesOnly_OrganizationUnchanged()
    {
        var sponsor = CreateValidMoneySponsor();
        var originalOrg = sponsor.SponsorOrganization;

        var result = sponsor.UpdateContactFields(
            notes: "Only notes change",
            organization: null,
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
        sponsor.SponsorNotes.Should().Be("Only notes change");
        sponsor.SponsorOrganization.Should().Be(originalOrg);
    }

    [Fact]
    public void UpdateContactFields_OnCompletedStripe_Succeeds()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.UpdateContactFields(
            notes: "After payment",
            organization: "PostPayCo",
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_OnCompletedOffPlatform_Succeeds()
    {
        var sponsor = CreateOffPlatformCompletedSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Off-platform note",
            organization: null,
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_OnRecordedItem_Succeeds()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Bringing two",
            organization: null,
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_OnFailed_OrganizerNotesOnly_Succeeds()
    {
        var sponsor = CreateFailedSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Card declined; sponsor moved to off-platform",
            organization: null,
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_OnFailed_OrganizerWithOrganization_Fails()
    {
        var sponsor = CreateFailedSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Reason",
            organization: "Cannot change on failed",
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void UpdateContactFields_OnFailed_NonOrganizer_Fails()
    {
        var sponsor = CreateFailedSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Not allowed",
            organization: null,
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_OnAbandoned_OrganizerNotesOnly_Succeeds()
    {
        var sponsor = CreateAbandonedSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Checkout expired",
            organization: null,
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_OnRefunded_OrganizerNotesOnly_Succeeds()
    {
        var sponsor = CreateRefundedSponsor();

        var result = sponsor.UpdateContactFields(
            notes: "Refunded after event cancel",
            organization: null,
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactFields_BothNull_NoChange_Succeeds()
    {
        // PATCH semantics: null/null = no-op, still sets audit fields.
        var sponsor = CreateValidMoneySponsor();
        var originalNotes = sponsor.SponsorNotes;
        var originalOrg = sponsor.SponsorOrganization;

        var result = sponsor.UpdateContactFields(
            notes: null,
            organization: null,
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
        sponsor.SponsorNotes.Should().Be(originalNotes);
        sponsor.SponsorOrganization.Should().Be(originalOrg);
        sponsor.LastEditedBy.Should().Be(SponsorActorId);
    }

    // ---------- UpdateAmount ----------

    [Fact]
    public void UpdateAmount_OnPendingMoney_Fails_StripeSessionStale()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(200m),
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void UpdateAmount_OnCompletedStripe_Fails_DeferredToV2()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(150m),
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Stripe");
    }

    [Fact]
    public void UpdateAmount_OnCompletedOffPlatform_Organizer_Succeeds()
    {
        var sponsor = CreateOffPlatformCompletedSponsor();

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(250m),
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsSuccess.Should().BeTrue();
        sponsor.Amount!.Amount.Should().Be(250m);
        sponsor.LastEditedBy.Should().Be(OrganizerActorId);
    }

    [Fact]
    public void UpdateAmount_OnCompletedOffPlatform_NonOrganizer_Fails()
    {
        var sponsor = CreateOffPlatformCompletedSponsor();

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(250m),
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("organizer");
    }

    [Fact]
    public void UpdateAmount_OnRecordedItem_Fails()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(100m),
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("item");
    }

    [Fact]
    public void UpdateAmount_WithZero_Fails()
    {
        // Negative amounts are rejected by Money.Create upstream; only zero
        // reaches the mutator, where its own guard rejects it.
        var sponsor = CreateOffPlatformCompletedSponsor();

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(0m),
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateAmount_OnFailed_Fails()
    {
        var sponsor = CreateFailedSponsor();

        var result = sponsor.UpdateAmount(
            newAmount: CreateMoney(100m),
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsFailure.Should().BeTrue();
    }

    // ---------- UpdateItemDetails ----------

    [Fact]
    public void UpdateItemDetails_OnRecordedItem_AllFields_Succeeds()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.UpdateItemDetails(
            itemName: "  Updated item  ",
            itemDescription: "  New desc  ",
            estimatedValue: 750m,
            actorUserId: SponsorActorId);

        result.IsSuccess.Should().BeTrue();
        sponsor.ItemName.Should().Be("Updated item");
        sponsor.ItemDescription.Should().Be("New desc");
        sponsor.EstimatedValue.Should().Be(750m);
        sponsor.LastEditedBy.Should().Be(SponsorActorId);
    }

    [Fact]
    public void UpdateItemDetails_OnRecordedItem_SubsetFields_Succeeds()
    {
        var sponsor = CreateValidItemSponsor();
        var originalDesc = sponsor.ItemDescription;
        var originalValue = sponsor.EstimatedValue;

        var result = sponsor.UpdateItemDetails(
            itemName: "New name only",
            itemDescription: null,
            estimatedValue: null,
            actorUserId: SponsorActorId);

        result.IsSuccess.Should().BeTrue();
        sponsor.ItemName.Should().Be("New name only");
        sponsor.ItemDescription.Should().Be(originalDesc);
        sponsor.EstimatedValue.Should().Be(originalValue);
    }

    [Fact]
    public void UpdateItemDetails_OnMoneySponsor_Fails()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.UpdateItemDetails(
            itemName: "Try item on money",
            itemDescription: null,
            estimatedValue: null,
            actorUserId: SponsorActorId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("item");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateItemDetails_WithEmptyOrWhitespaceItemName_Fails(string emptyName)
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.UpdateItemDetails(
            itemName: emptyName,
            itemDescription: null,
            estimatedValue: null,
            actorUserId: SponsorActorId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateItemDetails_WithNegativeEstimatedValue_Fails()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.UpdateItemDetails(
            itemName: null,
            itemDescription: null,
            estimatedValue: -1m,
            actorUserId: SponsorActorId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateItemDetails_OnRefundedItem_Fails()
    {
        // Item sponsors don't have a "refunded" state in normal flow, but test the
        // generic terminal-state guard via a hypothetical state. Here we use the
        // money refunded sponsor and the guard should fire on type mismatch first.
        var sponsor = CreateRefundedSponsor();

        var result = sponsor.UpdateItemDetails(
            itemName: "X",
            itemDescription: null,
            estimatedValue: null,
            actorUserId: OrganizerActorId);

        result.IsFailure.Should().BeTrue();
    }

    // ---------- UpdateName ----------

    [Fact]
    public void UpdateName_OnPendingMoney_Succeeds()
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.UpdateName(
            newName: "  Updated Person  ",
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
        sponsor.SponsorName.Should().Be("Updated Person");
        sponsor.LastEditedBy.Should().Be(SponsorActorId);
    }

    [Fact]
    public void UpdateName_OnCompletedStripe_Succeeds()
    {
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.UpdateName(
            newName: "Post-payment fix",
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_OnRecordedItem_Succeeds()
    {
        var sponsor = CreateValidItemSponsor();

        var result = sponsor.UpdateName(
            newName: "Item Person",
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_OnFailed_Fails()
    {
        var sponsor = CreateFailedSponsor();

        var result = sponsor.UpdateName(
            newName: "Cannot change",
            actorUserId: OrganizerActorId,
            actorIsOrganizer: true);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_WithEmptyOrWhitespace_Fails(string invalidName)
    {
        var sponsor = CreateValidMoneySponsor();

        var result = sponsor.UpdateName(
            newName: invalidName,
            actorUserId: SponsorActorId,
            actorIsOrganizer: false);

        result.IsFailure.Should().BeTrue();
    }

    // ---------- Audit field semantics ----------

    [Fact]
    public void Audit_LastEditedBy_IsNull_Initially()
    {
        var sponsor = CreateValidMoneySponsor();
        sponsor.LastEditedBy.Should().BeNull();
        sponsor.LastEditedAt.Should().BeNull();
    }

    [Fact]
    public void Audit_LifecycleTransition_DoesNotSetLastEditedBy()
    {
        // CompletePayment / MarkAsFailed / etc. are lifecycle, not human edits.
        // They update UpdatedAt but must NOT touch LastEditedBy / LastEditedAt.
        var sponsor = CreateValidMoneySponsor(desiredStatus: SponsorStatus.Completed);

        sponsor.LastEditedBy.Should().BeNull();
        sponsor.LastEditedAt.Should().BeNull();
    }

    [Fact]
    public void Audit_LastEditedBy_Set_OnEverySuccessfulMutator()
    {
        var sponsor = CreateValidMoneySponsor();

        sponsor.UpdateContactFields("note", null, SponsorActorId, false);
        sponsor.LastEditedBy.Should().Be(SponsorActorId);

        var firstEditAt = sponsor.LastEditedAt;
        firstEditAt.Should().NotBeNull();

        // Second edit by a different actor should overwrite the audit fields.
        sponsor.UpdateName("New", OrganizerActorId, true);
        sponsor.LastEditedBy.Should().Be(OrganizerActorId);
        sponsor.LastEditedAt.Should().BeOnOrAfter(firstEditAt!.Value);
    }

    #endregion

    // =========================================================================
    // Phase 6A.157 — Packaged sponsorship purchase flow
    //
    // Coverage matrix (14 cases):
    //   Factory:
    //     - 6 snapshot fields populated + Pending status + Money type
    //     - Distinct Money instances for Amount + PackagePriceSnapshot (EF owned-type trap)
    //     - Free package (price=0) accepted (instant-complete handled by caller)
    //     - Trims/normalizes inputs same as CreateMoneySponsor
    //     - Validation: empty packageId / empty packageName / null price / negative price / negative ticket count
    //     - RegistrationId always null (6A.159 cancelled)
    //   CompletePackagePayment:
    //     - Pending package sponsor → Completed + raises PackageSponsorCompletedEvent (NOT SponsorPaymentCompletedEvent)
    //     - Event carries 4 package snapshot fields
    //     - Cannot call on generic (non-package) sponsor
    //   Mutual guards:
    //     - Cannot call CompletePayment on package sponsor
    //     - Cannot call CompleteAsOrganizerCash on package sponsor
    // =========================================================================
    #region Package Sponsor (Phase 6A.157)

    private const string ValidPackageName = "  Gold Sponsor  ";
    private const string ValidPackageTier = "  Gold  ";
    private const int ValidIncludedTicketCount = 3;

    private static Sponsor CreateValidPackageSponsor(
        Guid? packageId = null,
        Money? price = null,
        int includedTicketCount = ValidIncludedTicketCount,
        string? tier = ValidPackageTier,
        SponsorStatus? desiredStatus = null)
    {
        var result = Sponsor.CreatePackageSponsor(
            ValidEventId,
            ValidUserId,
            ValidName,
            ValidEmail,
            ValidPhone,
            ValidOrganization,
            ValidNotes,
            packageId ?? Guid.NewGuid(),
            ValidPackageName,
            tier,
            price ?? CreateMoney(500m),
            includedTicketCount);

        result.IsSuccess.Should().BeTrue($"factory should succeed but said: {result.Error}");
        var sponsor = result.Value;

        if (desiredStatus == SponsorStatus.Completed)
        {
            sponsor.SetStripeCheckoutSession("cs_test_pkg_123", DateTime.UtcNow.AddHours(1));
            sponsor.CompletePackagePayment("pi_test_pkg_456");
        }

        return sponsor;
    }

    [Fact]
    public void CreatePackageSponsor_WithValidInputs_PopulatesAllSnapshotFields()
    {
        var packageId = Guid.NewGuid();
        var price = CreateMoney(500m, Currency.USD);

        var sponsor = CreateValidPackageSponsor(packageId: packageId, price: price);

        sponsor.EventId.Should().Be(ValidEventId);
        sponsor.Type.Should().Be(SponsorType.Money);
        sponsor.Status.Should().Be(SponsorStatus.Pending);
        sponsor.SponsorshipPackageId.Should().Be(packageId);
        sponsor.PackageNameSnapshot.Should().Be("Gold Sponsor"); // trimmed
        sponsor.PackageTierSnapshot.Should().Be("Gold"); // trimmed
        sponsor.PackagePriceSnapshot.Should().NotBeNull();
        sponsor.PackagePriceSnapshot!.Amount.Should().Be(500m);
        sponsor.PackagePriceSnapshot.Currency.Should().Be(Currency.USD);
        sponsor.IncludedTicketCountSnapshot.Should().Be(ValidIncludedTicketCount);
        sponsor.RegistrationId.Should().BeNull("RSVP-bundling (6A.159) was cancelled");
        sponsor.Amount.Should().NotBeNull();
        sponsor.Amount!.Amount.Should().Be(500m);
    }

    [Fact]
    public void CreatePackageSponsor_UsesDistinctMoneyInstancesForAmountAndSnapshot()
    {
        // EF Core owned-type rule — Amount and PackagePriceSnapshot must NOT
        // reference the same Money instance. The factory materializes a second
        // copy so EF tracks each independently.
        var sponsor = CreateValidPackageSponsor(price: CreateMoney(750m));

        sponsor.Amount.Should().NotBeSameAs(sponsor.PackagePriceSnapshot);
        sponsor.Amount!.Amount.Should().Be(sponsor.PackagePriceSnapshot!.Amount);
        sponsor.Amount.Currency.Should().Be(sponsor.PackagePriceSnapshot.Currency);
    }

    [Fact]
    public void CreatePackageSponsor_WithFreePackage_AcceptsZeroPrice()
    {
        // Free packages ("Friend of the show" tier with $0 price) are valid;
        // the caller is expected to immediately call CompletePackagePayment
        // with a sentinel intent ID rather than going through Stripe.
        var sponsor = CreateValidPackageSponsor(price: CreateMoney(0m));

        sponsor.Amount!.Amount.Should().Be(0m);
        sponsor.PackagePriceSnapshot!.Amount.Should().Be(0m);
        sponsor.Status.Should().Be(SponsorStatus.Pending);
    }

    [Fact]
    public void CreatePackageSponsor_TrimsAndNormalizesContactFields()
    {
        var sponsor = CreateValidPackageSponsor();

        sponsor.SponsorName.Should().Be("John Doe"); // trimmed
        sponsor.SponsorEmail.Should().Be("john.doe@example.com"); // trimmed + lowered
        sponsor.SponsorPhone.Should().Be("+1-555-1234");
        sponsor.SponsorOrganization.Should().Be("Acme Corp");
        sponsor.SponsorNotes.Should().Be("Happy to help");
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "SponsorshipPackageId is required")]
    public void CreatePackageSponsor_WithEmptyPackageId_ReturnsFailure(string emptyGuid, string expectedFragment)
    {
        var result = Sponsor.CreatePackageSponsor(
            ValidEventId, ValidUserId, ValidName, ValidEmail, ValidPhone,
            ValidOrganization, ValidNotes,
            Guid.Parse(emptyGuid), ValidPackageName, ValidPackageTier,
            CreateMoney(500m), ValidIncludedTicketCount);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(expectedFragment);
    }

    [Fact]
    public void CreatePackageSponsor_WithEmptyPackageName_ReturnsFailure()
    {
        var result = Sponsor.CreatePackageSponsor(
            ValidEventId, ValidUserId, ValidName, ValidEmail, ValidPhone,
            ValidOrganization, ValidNotes,
            Guid.NewGuid(), "  ", ValidPackageTier,
            CreateMoney(500m), ValidIncludedTicketCount);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("PackageName is required");
    }

    // Note: negative price is rejected by Money VO before reaching the
    // Sponsor factory — the defensive `packagePrice.Amount < 0` check inside
    // CreatePackageSponsor is unreachable via the public API. Kept as
    // defense in depth (future Money variants or reflection-based
    // construction) but not exercisable by a test.

    [Fact]
    public void CreatePackageSponsor_WithNegativeTicketCount_ReturnsFailure()
    {
        var result = Sponsor.CreatePackageSponsor(
            ValidEventId, ValidUserId, ValidName, ValidEmail, ValidPhone,
            ValidOrganization, ValidNotes,
            Guid.NewGuid(), ValidPackageName, ValidPackageTier,
            CreateMoney(500m), -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("IncludedTicketCount cannot be negative");
    }

    [Fact]
    public void CompletePackagePayment_OnPendingPackageSponsor_TransitionsToCompletedAndRaisesPackageEvent()
    {
        var sponsor = CreateValidPackageSponsor();
        sponsor.SetStripeCheckoutSession("cs_test_pkg_123", DateTime.UtcNow.AddHours(1));
        sponsor.ClearDomainEvents(); // strip any setup events

        var result = sponsor.CompletePackagePayment("pi_test_pkg_456");

        result.IsSuccess.Should().BeTrue($"expected success but got: {result.Error}");
        sponsor.Status.Should().Be(SponsorStatus.Completed);
        sponsor.StripePaymentIntentId.Should().Be("pi_test_pkg_456");
        sponsor.PaymentCompletedAt.Should().NotBeNull();
        sponsor.DomainEvents.Should().HaveCount(1);
        sponsor.DomainEvents[0].Should().BeOfType<PackageSponsorCompletedEvent>(
            "package sponsors raise the package-specific event, NOT the generic SponsorPaymentCompletedEvent");
    }

    [Fact]
    public void CompletePackagePayment_RaisedEventCarriesAllSnapshotFields()
    {
        var packageId = Guid.NewGuid();
        var sponsor = CreateValidPackageSponsor(packageId: packageId);
        sponsor.SetStripeCheckoutSession("cs_test_pkg_123", DateTime.UtcNow.AddHours(1));
        sponsor.ClearDomainEvents();

        sponsor.CompletePackagePayment("pi_test_pkg_456");

        var raised = sponsor.DomainEvents.OfType<PackageSponsorCompletedEvent>().Single();
        raised.SponsorshipPackageId.Should().Be(packageId);
        raised.PackageNameSnapshot.Should().Be("Gold Sponsor");
        raised.PackageTierSnapshot.Should().Be("Gold");
        raised.IncludedTicketCountSnapshot.Should().Be(ValidIncludedTicketCount);
        raised.PaymentIntentId.Should().Be("pi_test_pkg_456");
        raised.Amount.Should().Be(500m);
    }

    [Fact]
    public void CompletePackagePayment_OnGenericMoneySponsor_ReturnsFailure()
    {
        // Mutual guard: generic sponsors cannot use the package method.
        var generic = CreateValidMoneySponsor();
        generic.SetStripeCheckoutSession("cs_test_money_123", DateTime.UtcNow.AddHours(1));

        var result = generic.CompletePackagePayment("pi_test_pkg_456");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("CompletePackagePayment applies only to package sponsors");
        generic.Status.Should().Be(SponsorStatus.Pending); // unchanged
    }

    [Fact]
    public void CompletePayment_OnPackageSponsor_ReturnsFailure_MutualGuard()
    {
        // Mutual guard: package sponsors cannot use the generic method.
        // If this guard were missing, the generic SponsorPaymentCompletedEvent
        // would fire and the buyer would get the wrong confirmation email.
        var package = CreateValidPackageSponsor();
        package.SetStripeCheckoutSession("cs_test_pkg_123", DateTime.UtcNow.AddHours(1));

        var result = package.CompletePayment("pi_test_pkg_456");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Package sponsors must use CompletePackagePayment");
        package.Status.Should().Be(SponsorStatus.Pending); // unchanged
    }

    [Fact]
    public void CompleteAsOrganizerCash_OnPackageSponsor_ReturnsFailure_MutualGuard()
    {
        // Mutual guard: same reason as CompletePayment's guard. Off-platform
        // completion of a package sponsor would raise the generic event and
        // fire the wrong email template. Off-platform package completion is
        // not a supported v1 flow.
        var package = CreateValidPackageSponsor();

        var result = package.CompleteAsOrganizerCash();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("CompleteAsOrganizerCash does not apply to package sponsors");
        package.Status.Should().Be(SponsorStatus.Pending); // unchanged
    }

    [Fact]
    public void CompletePackagePayment_WhenStatusNotPending_ReturnsFailure()
    {
        var sponsor = CreateValidPackageSponsor(desiredStatus: SponsorStatus.Completed);

        var result = sponsor.CompletePackagePayment("pi_attempt_2");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot complete payment when status is Completed");
    }

    #endregion
}
