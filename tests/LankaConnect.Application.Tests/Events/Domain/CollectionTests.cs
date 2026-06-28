using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD Domain Tests: Collection (Event Fund Contribution) Entity
/// Covers all lifecycle transitions, validation rules, and domain event emission.
/// </summary>
public class CollectionTests
{
    #region Test Helpers

    private static Money CreateMoney(decimal amount = 50m, Currency currency = Currency.USD)
        => Money.Create(amount, currency).Value;

    private static Collection CreateValidCollection(
        Guid? eventId = null,
        Guid? contributorUserId = null,
        string contributorName = "John Doe",
        string contributorEmail = "john@test.com",
        string? contributorPhone = null,
        string? contributorNotes = null,
        Money? amount = null)
    {
        return Collection.Create(
            eventId ?? Guid.NewGuid(),
            contributorUserId,
            contributorName,
            contributorEmail,
            contributorPhone,
            contributorNotes,
            amount ?? CreateMoney()).Value;
    }

    private static Collection CreateCompletedCollection()
    {
        var collection = CreateValidCollection();
        collection.CompletePayment("pi_test_completed");
        return collection;
    }

    #endregion

    #region Create() Factory Method

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = CreateMoney(100m);

        // Act
        var result = Collection.Create(eventId, userId, "John Doe", "john@test.com", "+1234567890", "Some notes", amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var collection = result.Value;
        collection.EventId.Should().Be(eventId);
        collection.ContributorUserId.Should().Be(userId);
        collection.ContributorName.Should().Be("John Doe");
        collection.ContributorEmail.Should().Be("john@test.com");
        collection.ContributorPhone.Should().Be("+1234567890");
        collection.ContributorNotes.Should().Be("Some notes");
        collection.Amount.Should().Be(amount);
        collection.Status.Should().Be(CollectionStatus.Pending);
    }

    [Fact]
    public void Create_WithNullContributorUserId_ShouldSucceed()
    {
        // Act — anonymous contribution
        var result = Collection.Create(Guid.NewGuid(), null, "Anon", "anon@test.com", null, null, CreateMoney());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ContributorUserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullOptionalFields_ShouldSucceed()
    {
        // Act
        var result = Collection.Create(Guid.NewGuid(), Guid.NewGuid(), "Jane", "jane@test.com", null, null, CreateMoney());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ContributorPhone.Should().BeNull();
        result.Value.ContributorNotes.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        var collection = CreateValidCollection();
        collection.Status.Should().Be(CollectionStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        var result = Collection.Create(Guid.Empty, Guid.NewGuid(), "John", "john@test.com", null, null, CreateMoney());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyContributorName_ShouldFail(string? name)
    {
        var result = Collection.Create(Guid.NewGuid(), null, name!, "john@test.com", null, null, CreateMoney());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contributor name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyContributorEmail_ShouldFail(string? email)
    {
        var result = Collection.Create(Guid.NewGuid(), null, "John", email!, null, null, CreateMoney());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contributor email");
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldFail()
    {
        // Money.Create(0m) succeeds but Collection rejects amount <= 0
        var zeroMoney = CreateMoney(0m);
        var result = Collection.Create(Guid.NewGuid(), null, "John", "john@test.com", null, null, zeroMoney);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("greater than zero");
    }

    [Fact]
    public void Create_WithNegativeAmount_MoneyCreateShouldRejectIt()
    {
        // Money value object itself rejects negative amounts, so Collection.Create
        // can never receive a negative Money. Verify Money guards this.
        var moneyResult = Money.Create(-10m, Currency.USD);
        moneyResult.IsFailure.Should().BeTrue();
        moneyResult.Error.Should().Contain("negative");
    }

    [Fact]
    public void Create_ShouldTrimContributorName()
    {
        var collection = CreateValidCollection(contributorName: "  John Doe  ");
        collection.ContributorName.Should().Be("John Doe");
    }

    [Fact]
    public void Create_ShouldLowercaseAndTrimEmail()
    {
        var collection = CreateValidCollection(contributorEmail: "  JOHN@Test.COM  ");
        collection.ContributorEmail.Should().Be("john@test.com");
    }

    [Fact]
    public void Create_ShouldTrimOptionalFields()
    {
        var collection = CreateValidCollection(contributorPhone: "  +1234567890  ", contributorNotes: "  Some notes  ");
        collection.ContributorPhone.Should().Be("+1234567890");
        collection.ContributorNotes.Should().Be("Some notes");
    }

    [Fact]
    public void Create_ShouldGenerateId()
    {
        var collection = CreateValidCollection();
        collection.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow;
        var collection = CreateValidCollection();
        var after = DateTime.UtcNow;

        collection.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion

    #region SetStripeCheckoutSession()

    [Fact]
    public void SetStripeCheckoutSession_WhenPending_WithValidData_ShouldSucceed()
    {
        // Arrange
        var collection = CreateValidCollection();
        var sessionId = "cs_test_session_123";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        var result = collection.SetStripeCheckoutSession(sessionId, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.StripeCheckoutSessionId.Should().Be(sessionId);
        collection.CheckoutExpiresAt.Should().Be(expiresAt);
        collection.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetStripeCheckoutSession_WithEmptySessionId_ShouldFail(string? sessionId)
    {
        var collection = CreateValidCollection();
        var result = collection.SetStripeCheckoutSession(sessionId!, DateTime.UtcNow.AddHours(24));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("session ID");
    }

    [Fact]
    public void SetStripeCheckoutSession_WithPastExpiration_ShouldFail()
    {
        var collection = CreateValidCollection();
        var result = collection.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddMinutes(-5));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("future");
    }

    [Fact]
    public void SetStripeCheckoutSession_WhenCompleted_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        var result = collection.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(24));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public void SetStripeCheckoutSession_WhenFailed_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();

        var result = collection.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(24));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void SetStripeCheckoutSession_WhenAbandoned_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();

        var result = collection.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(24));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void SetStripeCheckoutSession_WhenRefunded_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();

        var result = collection.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(24));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refunded");
    }

    #endregion

    #region CompletePayment()

    [Fact]
    public void CompletePayment_WhenPending_WithValidPaymentIntentId_ShouldSucceed()
    {
        // Arrange
        var collection = CreateValidCollection();
        var paymentIntentId = "pi_test_payment_123";

        // Act
        var before = DateTime.UtcNow;
        var result = collection.CompletePayment(paymentIntentId);
        var after = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Completed);
        collection.StripePaymentIntentId.Should().Be(paymentIntentId);
        collection.PaymentCompletedAt.Should().NotBeNull();
        collection.PaymentCompletedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        collection.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompletePayment_ShouldRaiseCollectionCompletedEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = CreateMoney(75m, Currency.USD);
        var collection = CreateValidCollection(eventId: eventId, contributorUserId: userId, amount: amount);
        var paymentIntentId = "pi_test_event_123";

        // Act
        collection.CompletePayment(paymentIntentId);

        // Assert
        collection.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CollectionCompletedEvent>();

        var domainEvent = (CollectionCompletedEvent)collection.DomainEvents.Single();
        domainEvent.EventId.Should().Be(eventId);
        domainEvent.CollectionId.Should().Be(collection.Id);
        domainEvent.ContributorUserId.Should().Be(userId);
        domainEvent.ContributorName.Should().Be("John Doe");
        domainEvent.ContributorEmail.Should().Be("john@test.com");
        domainEvent.PaymentIntentId.Should().Be(paymentIntentId);
        domainEvent.Amount.Should().Be(75m);
        domainEvent.Currency.Should().Be("USD");
        domainEvent.PaymentCompletedAt.Should().Be(collection.PaymentCompletedAt!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CompletePayment_WithEmptyPaymentIntentId_ShouldFail(string? paymentIntentId)
    {
        var collection = CreateValidCollection();
        var result = collection.CompletePayment(paymentIntentId!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment intent ID");
    }

    [Fact]
    public void CompletePayment_WhenAlreadyCompleted_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        var result = collection.CompletePayment("pi_test_second");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public void CompletePayment_WhenFailed_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();

        var result = collection.CompletePayment("pi_test_123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void CompletePayment_WhenAbandoned_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();

        var result = collection.CompletePayment("pi_test_123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void CompletePayment_WhenRefunded_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();

        var result = collection.CompletePayment("pi_test_123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refunded");
    }

    #endregion

    #region MarkAsFailed()

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldSucceed()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act
        var before = DateTime.UtcNow;
        var result = collection.MarkAsFailed();
        var after = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Failed);
        collection.FailedAt.Should().NotBeNull();
        collection.FailedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        collection.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_WhenCompleted_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        var result = collection.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyFailed_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();

        var result = collection.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void MarkAsFailed_WhenAbandoned_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();

        var result = collection.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void MarkAsFailed_WhenRefunded_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();

        var result = collection.MarkAsFailed();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refunded");
    }

    #endregion

    #region MarkAsAbandoned()

    [Fact]
    public void MarkAsAbandoned_WhenPending_ShouldSucceed()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act
        var before = DateTime.UtcNow;
        var result = collection.MarkAsAbandoned();
        var after = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Abandoned);
        collection.AbandonedAt.Should().NotBeNull();
        collection.AbandonedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        collection.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsAbandoned_WhenCompleted_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        var result = collection.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public void MarkAsAbandoned_WhenFailed_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();

        var result = collection.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void MarkAsAbandoned_WhenAlreadyAbandoned_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();

        var result = collection.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void MarkAsAbandoned_WhenRefunded_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();

        var result = collection.MarkAsAbandoned();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refunded");
    }

    #endregion

    #region MarkAsRefunded()

    [Fact]
    public void MarkAsRefunded_WhenCompleted_ShouldSucceed()
    {
        // Arrange
        var collection = CreateCompletedCollection();

        // Act
        var before = DateTime.UtcNow;
        var result = collection.MarkAsRefunded();
        var after = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Refunded);
        collection.RefundedAt.Should().NotBeNull();
        collection.RefundedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        collection.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRefunded_WhenPending_ShouldFail()
    {
        var collection = CreateValidCollection();
        var result = collection.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void MarkAsRefunded_WhenFailed_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();

        var result = collection.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void MarkAsRefunded_WhenAbandoned_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();

        var result = collection.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void MarkAsRefunded_WhenAlreadyRefunded_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();

        var result = collection.MarkAsRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refunded");
    }

    #endregion

    #region SetRevenueBreakdown()

    [Fact]
    public void SetRevenueBreakdown_WhenPending_ShouldSucceed()
    {
        // Arrange
        var collection = CreateValidCollection();
        var stripeFee = CreateMoney(1.50m);
        var platformCommission = CreateMoney(2.50m);
        var organizerPayout = CreateMoney(46m);

        // Act
        var result = collection.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.StripeFeeAmount.Should().Be(stripeFee);
        collection.PlatformCommissionAmount.Should().Be(platformCommission);
        collection.OrganizerPayoutAmount.Should().Be(organizerPayout);
        collection.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetRevenueBreakdown_WhenCompleted_ShouldSucceed()
    {
        // Arrange
        var collection = CreateCompletedCollection();
        var stripeFee = CreateMoney(1.50m);
        var platformCommission = CreateMoney(2.50m);
        var organizerPayout = CreateMoney(46m);

        // Act
        var result = collection.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);

        // Assert
        result.IsSuccess.Should().BeTrue();
        collection.StripeFeeAmount.Should().Be(stripeFee);
        collection.PlatformCommissionAmount.Should().Be(platformCommission);
        collection.OrganizerPayoutAmount.Should().Be(organizerPayout);
    }

    [Fact]
    public void SetRevenueBreakdown_WhenFailed_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();

        var result = collection.SetRevenueBreakdown(CreateMoney(1m), CreateMoney(2m), CreateMoney(47m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed");
    }

    [Fact]
    public void SetRevenueBreakdown_WhenAbandoned_ShouldFail()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();

        var result = collection.SetRevenueBreakdown(CreateMoney(1m), CreateMoney(2m), CreateMoney(47m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public void SetRevenueBreakdown_WhenRefunded_ShouldFail()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();

        var result = collection.SetRevenueBreakdown(CreateMoney(1m), CreateMoney(2m), CreateMoney(47m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refunded");
    }

    [Fact]
    public void SetRevenueBreakdown_WithNullStripeFee_ShouldFail()
    {
        var collection = CreateValidCollection();
        var result = collection.SetRevenueBreakdown(null!, CreateMoney(2m), CreateMoney(47m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public void SetRevenueBreakdown_WithNullPlatformCommission_ShouldFail()
    {
        var collection = CreateValidCollection();
        var result = collection.SetRevenueBreakdown(CreateMoney(1m), null!, CreateMoney(47m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public void SetRevenueBreakdown_WithNullOrganizerPayout_ShouldFail()
    {
        var collection = CreateValidCollection();
        var result = collection.SetRevenueBreakdown(CreateMoney(1m), CreateMoney(2m), null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    #endregion

    #region IsTerminal Property

    [Fact]
    public void IsTerminal_WhenPending_ShouldBeFalse()
    {
        var collection = CreateValidCollection();
        collection.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void IsTerminal_WhenCompleted_ShouldBeTrue()
    {
        var collection = CreateCompletedCollection();
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenFailed_ShouldBeTrue()
    {
        var collection = CreateValidCollection();
        collection.MarkAsFailed();
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenAbandoned_ShouldBeTrue()
    {
        var collection = CreateValidCollection();
        collection.MarkAsAbandoned();
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WhenRefunded_ShouldBeTrue()
    {
        var collection = CreateCompletedCollection();
        collection.MarkAsRefunded();
        collection.IsTerminal.Should().BeTrue();
    }

    #endregion

    #region IsCheckoutExpired Property

    [Fact]
    public void IsCheckoutExpired_WhenNoCheckoutSession_ShouldBeFalse()
    {
        var collection = CreateValidCollection();
        collection.IsCheckoutExpired.Should().BeFalse();
    }

    [Fact]
    public void IsCheckoutExpired_WhenCheckoutNotExpired_ShouldBeFalse()
    {
        var collection = CreateValidCollection();
        collection.SetStripeCheckoutSession("cs_test_123", DateTime.UtcNow.AddHours(24));
        collection.IsCheckoutExpired.Should().BeFalse();
    }

    #endregion

    #region Full Lifecycle Scenarios

    [Fact]
    public void FullLifecycle_PendingToCompleted_ShouldTransitionCorrectly()
    {
        // Create
        var collection = CreateValidCollection();
        collection.Status.Should().Be(CollectionStatus.Pending);

        // Set checkout session
        collection.SetStripeCheckoutSession("cs_test_lifecycle", DateTime.UtcNow.AddHours(24));
        collection.StripeCheckoutSessionId.Should().Be("cs_test_lifecycle");

        // Set revenue breakdown (pre-payment estimate)
        collection.SetRevenueBreakdown(CreateMoney(1.50m), CreateMoney(2.50m), CreateMoney(46m));

        // Complete payment
        var result = collection.CompletePayment("pi_test_lifecycle");
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Completed);
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_PendingToFailed_ShouldTransitionCorrectly()
    {
        var collection = CreateValidCollection();
        collection.SetStripeCheckoutSession("cs_test_fail", DateTime.UtcNow.AddHours(24));

        var result = collection.MarkAsFailed();
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Failed);
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_PendingToAbandoned_ShouldTransitionCorrectly()
    {
        var collection = CreateValidCollection();
        collection.SetStripeCheckoutSession("cs_test_abandon", DateTime.UtcNow.AddHours(24));

        var result = collection.MarkAsAbandoned();
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Abandoned);
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_CompletedToRefunded_ShouldTransitionCorrectly()
    {
        var collection = CreateCompletedCollection();
        collection.Status.Should().Be(CollectionStatus.Completed);

        var result = collection.MarkAsRefunded();
        result.IsSuccess.Should().BeTrue();
        collection.Status.Should().Be(CollectionStatus.Refunded);
        collection.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_CompletedWithRevenueBreakdown_ShouldWork()
    {
        // Complete payment first
        var collection = CreateCompletedCollection();

        // Then set final revenue breakdown
        var stripeFee = CreateMoney(1.45m);
        var platformCommission = CreateMoney(2.50m);
        var organizerPayout = CreateMoney(46.05m);

        var result = collection.SetRevenueBreakdown(stripeFee, platformCommission, organizerPayout);
        result.IsSuccess.Should().BeTrue();
        collection.StripeFeeAmount.Should().Be(stripeFee);
        collection.PlatformCommissionAmount.Should().Be(platformCommission);
        collection.OrganizerPayoutAmount.Should().Be(organizerPayout);
    }

    [Fact]
    public void TerminalStates_ShouldNotAllowFurtherTransitions()
    {
        // Failed -> no further transitions
        var failed = CreateValidCollection();
        failed.MarkAsFailed();
        failed.CompletePayment("pi_test").IsFailure.Should().BeTrue();
        failed.MarkAsAbandoned().IsFailure.Should().BeTrue();
        failed.MarkAsRefunded().IsFailure.Should().BeTrue();

        // Abandoned -> no further transitions
        var abandoned = CreateValidCollection();
        abandoned.MarkAsAbandoned();
        abandoned.CompletePayment("pi_test").IsFailure.Should().BeTrue();
        abandoned.MarkAsFailed().IsFailure.Should().BeTrue();
        abandoned.MarkAsRefunded().IsFailure.Should().BeTrue();

        // Refunded -> no further transitions
        var refunded = CreateCompletedCollection();
        refunded.MarkAsRefunded();
        refunded.CompletePayment("pi_test").IsFailure.Should().BeTrue();
        refunded.MarkAsFailed().IsFailure.Should().BeTrue();
        refunded.MarkAsAbandoned().IsFailure.Should().BeTrue();
    }

    #endregion
}
