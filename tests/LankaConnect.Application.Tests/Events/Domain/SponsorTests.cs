using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
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

    #region BaseEntity Properties Tests

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
}
