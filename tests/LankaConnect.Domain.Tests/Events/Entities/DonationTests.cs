using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class DonationTests
{
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Money _validAmount = Money.Create(25.00m, Currency.USD).Value;

    #region Create (Standalone) Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        // Arrange & Act
        var result = Donation.Create(
            _eventId,
            _userId,
            "John Doe",
            "john@example.com",
            "555-1234",
            "Keep up the great work!",
            _validAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var donation = result.Value;
        donation.EventId.Should().Be(_eventId);
        donation.RegistrationId.Should().BeNull();
        donation.DonorUserId.Should().Be(_userId);
        donation.DonorName.Should().Be("John Doe");
        donation.DonorEmail.Should().Be("john@example.com");
        donation.DonorPhone.Should().Be("555-1234");
        donation.DonorNotes.Should().Be("Keep up the great work!");
        donation.Amount.Should().Be(_validAmount);
        donation.Status.Should().Be(DonationStatus.Pending);
        donation.IsBundled.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAnonymousDonor_Should_Return_Success()
    {
        // Arrange & Act
        var result = Donation.Create(
            _eventId,
            donorUserId: null,
            "Jane Doe",
            "jane@example.com",
            null,
            null,
            _validAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DonorUserId.Should().BeNull();
        result.Value.DonorPhone.Should().BeNull();
        result.Value.DonorNotes.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyEventId_Should_Fail()
    {
        // Act
        var result = Donation.Create(
            Guid.Empty,
            _userId,
            "John Doe",
            "john@example.com",
            null,
            null,
            _validAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    [Fact]
    public void Create_WithEmptyDonorName_Should_Fail()
    {
        // Act
        var result = Donation.Create(
            _eventId,
            _userId,
            "",
            "john@example.com",
            null,
            null,
            _validAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Donor name is required");
    }

    [Fact]
    public void Create_WithEmptyDonorEmail_Should_Fail()
    {
        // Act
        var result = Donation.Create(
            _eventId,
            _userId,
            "John Doe",
            "",
            null,
            null,
            _validAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Donor email is required");
    }

    [Fact]
    public void Create_WithZeroAmount_Should_Fail()
    {
        // Arrange
        var zeroAmount = Money.Create(0m, Currency.USD).Value;

        // Act
        var result = Donation.Create(
            _eventId,
            _userId,
            "John Doe",
            "john@example.com",
            null,
            null,
            zeroAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("greater than zero");
    }

    [Fact]
    public void Create_TrimsAndNormalizesInput()
    {
        // Act
        var result = Donation.Create(
            _eventId,
            _userId,
            "  John Doe  ",
            "  John@Example.COM  ",
            "  555-1234  ",
            "  Some notes  ",
            _validAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DonorName.Should().Be("John Doe");
        result.Value.DonorEmail.Should().Be("john@example.com");
        result.Value.DonorPhone.Should().Be("555-1234");
        result.Value.DonorNotes.Should().Be("Some notes");
    }

    #endregion

    #region CreateBundledWithRegistration Tests

    [Fact]
    public void CreateBundled_WithValidData_Should_Return_Success()
    {
        // Arrange
        var registrationId = Guid.NewGuid();

        // Act
        var result = Donation.CreateBundledWithRegistration(
            _eventId,
            registrationId,
            _userId,
            "John Doe",
            "john@example.com",
            null,
            null,
            _validAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationId.Should().Be(registrationId);
        result.Value.IsBundled.Should().BeTrue();
        result.Value.Status.Should().Be(DonationStatus.Pending);
    }

    [Fact]
    public void CreateBundled_WithEmptyRegistrationId_Should_Fail()
    {
        // Act
        var result = Donation.CreateBundledWithRegistration(
            _eventId,
            Guid.Empty,
            _userId,
            "John Doe",
            "john@example.com",
            null,
            null,
            _validAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Registration ID is required");
    }

    #endregion

    #region SetStripeCheckoutSession Tests

    [Fact]
    public void SetStripeCheckoutSession_WhenPending_Should_Succeed()
    {
        // Arrange
        var donation = CreateValidDonation();
        var sessionId = "cs_test_abc123";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        var result = donation.SetStripeCheckoutSession(sessionId, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.StripeCheckoutSessionId.Should().Be(sessionId);
        donation.CheckoutExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void SetStripeCheckoutSession_WithEmptySessionId_Should_Fail()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        var result = donation.SetStripeCheckoutSession("", DateTime.UtcNow.AddHours(24));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Checkout session ID is required");
    }

    [Fact]
    public void SetStripeCheckoutSession_WithPastExpiration_Should_Fail()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        var result = donation.SetStripeCheckoutSession("cs_test_abc", DateTime.UtcNow.AddHours(-1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Expiration time must be in the future");
    }

    [Fact]
    public void SetStripeCheckoutSession_WhenNotPending_Should_Fail()
    {
        // Arrange
        var donation = CreateCompletedDonation();

        // Act
        var result = donation.SetStripeCheckoutSession("cs_test_new", DateTime.UtcNow.AddHours(24));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot set checkout session when status is");
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void CompletePayment_FromPending_Should_Succeed()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        var result = donation.CompletePayment("pi_test_abc123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Completed);
        donation.StripePaymentIntentId.Should().Be("pi_test_abc123");
        donation.PaymentCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompletePayment_Should_Raise_DonationCompletedEvent()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        donation.CompletePayment("pi_test_abc123");

        // Assert
        donation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DonationCompletedEvent>();
        var domainEvent = (DonationCompletedEvent)donation.DomainEvents.First();
        domainEvent.DonationId.Should().Be(donation.Id);
        domainEvent.EventId.Should().Be(donation.EventId);
        domainEvent.DonorName.Should().Be(donation.DonorName);
        domainEvent.DonorEmail.Should().Be(donation.DonorEmail);
        domainEvent.PaymentIntentId.Should().Be("pi_test_abc123");
        domainEvent.Amount.Should().Be(donation.Amount.Amount);
    }

    [Fact]
    public void CompletePayment_WithEmptyPaymentIntentId_Should_Fail()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        var result = donation.CompletePayment("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment intent ID is required");
    }

    [Fact]
    public void CompletePayment_WhenNotPending_Should_Fail()
    {
        // Arrange
        var donation = CreateCompletedDonation();

        // Act
        var result = donation.CompletePayment("pi_test_new");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot complete payment when status is");
    }

    [Fact]
    public void MarkAsFailed_FromPending_Should_Succeed()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        var result = donation.MarkAsFailed();

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Failed);
        donation.FailedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_WhenNotPending_Should_Fail()
    {
        // Arrange
        var donation = CreateCompletedDonation();

        // Act
        var result = donation.MarkAsFailed();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkAsAbandoned_FromPending_Should_Succeed()
    {
        // Arrange
        var donation = CreateValidDonation();

        // Act
        var result = donation.MarkAsAbandoned();

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Abandoned);
        donation.AbandonedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsAbandoned_WhenNotPending_Should_Fail()
    {
        // Arrange
        var donation = CreateCompletedDonation();

        // Act
        var result = donation.MarkAsAbandoned();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Must be Pending");
    }

    [Fact]
    public void MarkAsRefunded_FromCompleted_Should_Succeed()
    {
        // Arrange
        var donation = CreateCompletedDonation();

        // Act
        var result = donation.MarkAsRefunded();

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Refunded);
        donation.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRefunded_WhenNotCompleted_Should_Fail()
    {
        // Arrange
        var donation = CreateValidDonation(); // Pending

        // Act
        var result = donation.MarkAsRefunded();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Must be Completed");
    }

    #endregion

    #region SetRevenueBreakdown Tests

    [Fact]
    public void SetRevenueBreakdown_WithValidData_Should_Succeed()
    {
        // Arrange
        var donation = CreateValidDonation();
        var stripeFee = Money.Create(1.03m, Currency.USD).Value;
        var platformCommission = Money.Create(0.50m, Currency.USD).Value;
        var payout = Money.Create(23.47m, Currency.USD).Value;

        // Act
        var result = donation.SetRevenueBreakdown(stripeFee, platformCommission, payout);

        // Assert
        result.IsSuccess.Should().BeTrue();
        donation.StripeFeeAmount.Should().Be(stripeFee);
        donation.PlatformCommissionAmount.Should().Be(platformCommission);
        donation.OrganizerPayoutAmount.Should().Be(payout);
    }

    #endregion

    #region Terminal State Tests

    [Fact]
    public void IsTerminal_ForPending_Should_BeFalse()
    {
        var donation = CreateValidDonation();
        donation.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void IsTerminal_ForCompleted_Should_BeTrue()
    {
        var donation = CreateCompletedDonation();
        donation.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_ForFailed_Should_BeTrue()
    {
        var donation = CreateValidDonation();
        donation.MarkAsFailed();
        donation.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_ForAbandoned_Should_BeTrue()
    {
        var donation = CreateValidDonation();
        donation.MarkAsAbandoned();
        donation.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_ForRefunded_Should_BeTrue()
    {
        var donation = CreateCompletedDonation();
        donation.MarkAsRefunded();
        donation.IsTerminal.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private Donation CreateValidDonation()
    {
        return Donation.Create(
            _eventId,
            _userId,
            "John Doe",
            "john@example.com",
            null,
            null,
            _validAmount).Value;
    }

    private Donation CreateCompletedDonation()
    {
        var donation = CreateValidDonation();
        donation.CompletePayment("pi_test_completed");
        donation.ClearDomainEvents();
        return donation;
    }

    #endregion
}
