using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Standalone add-on purchase entity with its own payment lifecycle.
/// Follows the Donation pattern for Stripe payment handling.
///
/// Key design decisions:
/// - UnitPrice is a SNAPSHOT captured at checkout session creation time (M3: price snapshot).
///   If the organizer changes the add-on price later, existing purchases retain their original price.
/// - Stock reservation is handled via atomic SQL (not through this entity).
///
/// Lifecycle:
/// 1. Buyer initiates purchase -> Status = Pending, stock reserved, Stripe checkout created
/// 2. Buyer completes payment -> Status = Completed (via webhook)
///
/// Alternative paths:
/// - Payment fails -> Status = Failed, stock restored
/// - Checkout expires (24h) -> Status = Abandoned, stock restored
/// - Payment refunded -> Status = Refunded, stock restored
/// </summary>
public class AddOnPurchase : LegacyBaseEntity
{
    // Event and add-on linkage
    public Guid EventId { get; private set; }
    public Guid AddOnDefinitionId { get; private set; }

    /// <summary>
    /// Links to registration when purchase is bundled during registration (combined checkout).
    /// Null for standalone purchases from event details page.
    /// </summary>
    public Guid? RegistrationId { get; private set; }

    // Buyer information
    /// <summary>
    /// User ID of the buyer. Null for anonymous purchases.
    /// </summary>
    public Guid? BuyerUserId { get; private set; }

    public string BuyerName { get; private set; } = null!;
    public string BuyerEmail { get; private set; } = null!;
    public string? BuyerPhone { get; private set; }

    // Purchase details
    public int Quantity { get; private set; }

    /// <summary>
    /// SNAPSHOT of the add-on price at checkout session creation time (M3).
    /// This does NOT change if the organizer later updates the add-on definition price.
    /// </summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>
    /// Total purchase amount: Quantity * UnitPrice.Amount.
    /// </summary>
    public Money TotalAmount { get; private set; } = null!;

    // Status tracking
    public AddOnPurchaseStatus Status { get; private set; }

    // Stripe payment fields
    public string? StripeCheckoutSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public DateTime? CheckoutExpiresAt { get; private set; }

    // Revenue breakdown fields (populated after payment completion)
    public Money? StripeFeeAmount { get; private set; }
    public Money? PlatformCommissionAmount { get; private set; }
    public Money? OrganizerPayoutAmount { get; private set; }

    // Lifecycle timestamps
    public DateTime? PaymentCompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? AbandonedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    // EF Core constructor
    private AddOnPurchase()
    {
    }

    /// <summary>
    /// Creates a standalone add-on purchase (from event details page).
    /// UnitPrice is snapshotted from the AddOnDefinition at creation time.
    /// </summary>
    public static Result<AddOnPurchase> Create(
        Guid eventId,
        Guid addOnDefinitionId,
        Guid? buyerUserId,
        string buyerName,
        string buyerEmail,
        string? buyerPhone,
        int quantity,
        Money unitPrice)
    {
        return CreateInternal(
            eventId, addOnDefinitionId, registrationId: null,
            buyerUserId, buyerName, buyerEmail, buyerPhone,
            quantity, unitPrice);
    }

    /// <summary>
    /// Creates an add-on purchase bundled with a registration (combined checkout).
    /// UnitPrice is snapshotted from the AddOnDefinition at creation time.
    /// </summary>
    public static Result<AddOnPurchase> CreateBundledWithRegistration(
        Guid eventId,
        Guid addOnDefinitionId,
        Guid registrationId,
        Guid? buyerUserId,
        string buyerName,
        string buyerEmail,
        string? buyerPhone,
        int quantity,
        Money unitPrice)
    {
        if (registrationId == Guid.Empty)
            return Result<AddOnPurchase>.Failure("Registration ID is required for bundled purchases");

        return CreateInternal(
            eventId, addOnDefinitionId, registrationId,
            buyerUserId, buyerName, buyerEmail, buyerPhone,
            quantity, unitPrice);
    }

    private static Result<AddOnPurchase> CreateInternal(
        Guid eventId,
        Guid addOnDefinitionId,
        Guid? registrationId,
        Guid? buyerUserId,
        string buyerName,
        string buyerEmail,
        string? buyerPhone,
        int quantity,
        Money unitPrice)
    {
        if (eventId == Guid.Empty)
            return Result<AddOnPurchase>.Failure("Event ID is required");

        if (addOnDefinitionId == Guid.Empty)
            return Result<AddOnPurchase>.Failure("Add-on definition ID is required");

        if (string.IsNullOrWhiteSpace(buyerName))
            return Result<AddOnPurchase>.Failure("Buyer name is required");

        if (string.IsNullOrWhiteSpace(buyerEmail))
            return Result<AddOnPurchase>.Failure("Buyer email is required");

        if (quantity <= 0)
            return Result<AddOnPurchase>.Failure("Quantity must be greater than zero");

        if (unitPrice == null)
            return Result<AddOnPurchase>.Failure("Unit price is required");

        if (unitPrice.Amount < 0)
            return Result<AddOnPurchase>.Failure("Unit price cannot be negative");

        // Calculate total amount
        var totalAmountResult = Money.Create(unitPrice.Amount * quantity, unitPrice.Currency);
        if (totalAmountResult.IsFailure)
            return Result<AddOnPurchase>.Failure(totalAmountResult.Error);

        var purchase = new AddOnPurchase
        {
            EventId = eventId,
            AddOnDefinitionId = addOnDefinitionId,
            RegistrationId = registrationId,
            BuyerUserId = buyerUserId,
            BuyerName = buyerName.Trim(),
            BuyerEmail = buyerEmail.Trim().ToLowerInvariant(),
            BuyerPhone = buyerPhone?.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalAmount = totalAmountResult.Value,
            Status = AddOnPurchaseStatus.Pending
        };

        return Result<AddOnPurchase>.Success(purchase);
    }

    /// <summary>
    /// Sets the Stripe checkout session ID and expiration time.
    /// </summary>
    public Result SetStripeCheckoutSession(string sessionId, DateTime expiresAt)
    {
        if (Status != AddOnPurchaseStatus.Pending)
            return Result.Failure($"Cannot set checkout session when status is {Status}");

        if (string.IsNullOrWhiteSpace(sessionId))
            return Result.Failure("Checkout session ID is required");

        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure("Expiration time must be in the future");

        StripeCheckoutSessionId = sessionId;
        CheckoutExpiresAt = expiresAt;

        return Result.Success();
    }

    /// <summary>
    /// Marks the purchase as completed after receiving Stripe webhook.
    /// Raises AddOnPurchaseCompletedEvent for email notifications.
    /// </summary>
    public Result CompletePayment(string paymentIntentId)
    {
        if (Status != AddOnPurchaseStatus.Pending)
            return Result.Failure($"Cannot complete payment when status is {Status}");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure("Payment intent ID is required");

        StripePaymentIntentId = paymentIntentId;
        Status = AddOnPurchaseStatus.Completed;
        PaymentCompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AddOnPurchaseCompletedEvent(
            EventId,
            Id,
            AddOnDefinitionId,
            BuyerUserId,
            BuyerName,
            BuyerEmail,
            paymentIntentId,
            Quantity,
            UnitPrice.Amount,
            TotalAmount.Amount,
            TotalAmount.Currency.ToString(),
            PaymentCompletedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Marks the purchase as failed due to payment failure.
    /// NOTE: Stock should be restored via AddOnDefinitionRepository.TryRestoreStockAsync().
    /// </summary>
    public Result MarkAsFailed()
    {
        if (Status != AddOnPurchaseStatus.Pending)
            return Result.Failure($"Cannot mark as failed when status is {Status}. Must be Pending.");

        Status = AddOnPurchaseStatus.Failed;
        FailedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the purchase as abandoned due to checkout expiration.
    /// NOTE: Stock should be restored via AddOnDefinitionRepository.TryRestoreStockAsync().
    /// </summary>
    public Result MarkAsAbandoned()
    {
        if (Status != AddOnPurchaseStatus.Pending)
            return Result.Failure($"Cannot mark as abandoned when status is {Status}. Must be Pending.");

        Status = AddOnPurchaseStatus.Abandoned;
        AbandonedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the purchase as refunded after a completed payment.
    /// NOTE: Stock should be restored via AddOnDefinitionRepository.TryRestoreStockAsync().
    /// </summary>
    public Result MarkAsRefunded()
    {
        if (Status != AddOnPurchaseStatus.Completed)
            return Result.Failure($"Cannot refund when status is {Status}. Must be Completed.");

        Status = AddOnPurchaseStatus.Refunded;
        RefundedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Sets the revenue breakdown (fees and payout) after payment.
    /// </summary>
    public Result SetRevenueBreakdown(Money stripeFee, Money platformCommission, Money organizerPayout)
    {
        if (Status != AddOnPurchaseStatus.Pending && Status != AddOnPurchaseStatus.Completed)
            return Result.Failure($"Cannot set revenue breakdown when status is {Status}");

        if (stripeFee == null || platformCommission == null || organizerPayout == null)
            return Result.Failure("All revenue breakdown components are required");

        StripeFeeAmount = stripeFee;
        PlatformCommissionAmount = platformCommission;
        OrganizerPayoutAmount = organizerPayout;

        return Result.Success();
    }

    /// <summary>
    /// Whether the purchase is in a terminal state (cannot change anymore).
    /// </summary>
    public bool IsTerminal => Status == AddOnPurchaseStatus.Completed ||
                              Status == AddOnPurchaseStatus.Failed ||
                              Status == AddOnPurchaseStatus.Abandoned ||
                              Status == AddOnPurchaseStatus.Refunded;

    /// <summary>
    /// Whether the checkout session has expired.
    /// </summary>
    public bool IsCheckoutExpired => CheckoutExpiresAt.HasValue && DateTime.UtcNow > CheckoutExpiresAt.Value;

    /// <summary>
    /// Whether this purchase is bundled with a registration.
    /// </summary>
    public bool IsBundled => RegistrationId.HasValue;
}
