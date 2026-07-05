using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Tracks individual payments made for a registration.
/// Supports initial payments and subsequent additions.
/// Part of the Add-Only Attendees with Delta Payment feature.
///
/// This entity provides:
/// - Audit trail for all payments
/// - Reconciliation with Stripe dashboard
/// - Foundation for future partial refund support
/// </summary>
public class RegistrationPayment : LegacyBaseEntity
{
    /// <summary>
    /// ID of the registration this payment belongs to.
    /// </summary>
    public Guid RegistrationId { get; private set; }

    /// <summary>
    /// Stripe Payment Intent ID for this payment.
    /// Used for reconciliation with Stripe.
    /// </summary>
    public string StripePaymentIntentId { get; private set; } = null!;

    /// <summary>
    /// Amount paid in this transaction.
    /// </summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>
    /// Type of payment (Initial registration or Addition).
    /// </summary>
    public PaymentType Type { get; private set; }

    /// <summary>
    /// Status of this payment.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// When payment was completed via Stripe.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Optional reference to the RegistrationAddition for addition payments.
    /// Null for initial payments.
    /// </summary>
    public Guid? RegistrationAdditionId { get; private set; }

    // EF Core constructor
    private RegistrationPayment()
    {
        // Required for EF Core
    }

    private RegistrationPayment(
        Guid registrationId,
        string stripePaymentIntentId,
        Money amount,
        PaymentType type,
        PaymentStatus status,
        Guid? registrationAdditionId = null)
    {
        RegistrationId = registrationId;
        StripePaymentIntentId = stripePaymentIntentId;
        Amount = amount;
        Type = type;
        Status = status;
        RegistrationAdditionId = registrationAdditionId;
    }

    /// <summary>
    /// Creates a new RegistrationPayment for an initial registration payment.
    /// </summary>
    /// <param name="registrationId">ID of the registration.</param>
    /// <param name="stripePaymentIntentId">Stripe Payment Intent ID.</param>
    /// <param name="amount">Amount paid.</param>
    /// <param name="status">Payment status (typically Completed).</param>
    public static Result<RegistrationPayment> CreateInitial(
        Guid registrationId,
        string stripePaymentIntentId,
        Money amount,
        PaymentStatus status = PaymentStatus.Completed)
    {
        return Create(registrationId, stripePaymentIntentId, amount, PaymentType.Initial, status, null);
    }

    /// <summary>
    /// Creates a new RegistrationPayment for an addition payment.
    /// </summary>
    /// <param name="registrationId">ID of the registration.</param>
    /// <param name="stripePaymentIntentId">Stripe Payment Intent ID.</param>
    /// <param name="amount">Additional amount paid.</param>
    /// <param name="registrationAdditionId">ID of the RegistrationAddition.</param>
    /// <param name="status">Payment status (typically Completed).</param>
    public static Result<RegistrationPayment> CreateAddition(
        Guid registrationId,
        string stripePaymentIntentId,
        Money amount,
        Guid registrationAdditionId,
        PaymentStatus status = PaymentStatus.Completed)
    {
        if (registrationAdditionId == Guid.Empty)
            return Result<RegistrationPayment>.Failure("Registration Addition ID is required for addition payments");

        return Create(registrationId, stripePaymentIntentId, amount, PaymentType.Addition, status, registrationAdditionId);
    }

    /// <summary>
    /// Creates a new RegistrationPayment with validation.
    /// </summary>
    private static Result<RegistrationPayment> Create(
        Guid registrationId,
        string stripePaymentIntentId,
        Money amount,
        PaymentType type,
        PaymentStatus status,
        Guid? registrationAdditionId)
    {
        if (registrationId == Guid.Empty)
            return Result<RegistrationPayment>.Failure("Registration ID is required");

        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
            return Result<RegistrationPayment>.Failure("Stripe Payment Intent ID is required");

        if (amount == null)
            return Result<RegistrationPayment>.Failure("Amount is required");

        if (amount.Amount < 0)
            return Result<RegistrationPayment>.Failure("Amount cannot be negative");

        var payment = new RegistrationPayment(
            registrationId,
            stripePaymentIntentId,
            amount,
            type,
            status,
            registrationAdditionId);

        // Set completed timestamp if status is Completed
        if (status == PaymentStatus.Completed)
        {
            payment.CompletedAt = DateTime.UtcNow;
        }

        return Result<RegistrationPayment>.Success(payment);
    }

    /// <summary>
    /// Marks the payment as completed.
    /// Transitions from Pending to Completed.
    /// </summary>
    public Result MarkAsCompleted()
    {
        if (Status == PaymentStatus.Completed)
            return Result.Failure("Payment is already completed");

        if (Status == PaymentStatus.Failed)
            return Result.Failure("Cannot complete a failed payment");

        if (Status == PaymentStatus.Refunded)
            return Result.Failure("Cannot complete a refunded payment");

        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the payment as failed.
    /// </summary>
    public Result MarkAsFailed()
    {
        if (Status == PaymentStatus.Completed)
            return Result.Failure("Cannot mark a completed payment as failed");

        if (Status == PaymentStatus.Failed)
            return Result.Failure("Payment is already marked as failed");

        if (Status == PaymentStatus.Refunded)
            return Result.Failure("Cannot mark a refunded payment as failed");

        Status = PaymentStatus.Failed;

        return Result.Success();
    }

    /// <summary>
    /// Marks the payment as refunded.
    /// </summary>
    public Result MarkAsRefunded()
    {
        if (Status != PaymentStatus.Completed)
            return Result.Failure("Only completed payments can be refunded");

        Status = PaymentStatus.Refunded;

        return Result.Success();
    }
}
