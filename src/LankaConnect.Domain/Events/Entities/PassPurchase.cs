using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Entity representing a user's purchase of event passes
/// Tracks purchase status, payment, and generates QR code for check-in
/// </summary>
public class PassPurchase : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid EventPassId { get; private set; }
    public int Quantity { get; private set; }
    public Money TotalPrice { get; private set; }
    public PassPurchaseStatus Status { get; private set; }
    public string QRCode { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // EF Core constructor
    private PassPurchase()
    {
        TotalPrice = null!;
        QRCode = null!;
    }

    private PassPurchase(
        Guid userId,
        Guid eventId,
        Guid eventPassId,
        int quantity,
        Money unitPrice)
    {
        UserId = userId;
        EventId = eventId;
        EventPassId = eventPassId;
        Quantity = quantity;
        TotalPrice = unitPrice.Multiply(quantity).Value; // Multiply returns Result<Money>
        Status = PassPurchaseStatus.Pending;
        QRCode = GenerateQRCode();
    }

    public static Result<PassPurchase> Create(
        Guid userId,
        Guid eventId,
        Guid eventPassId,
        int quantity,
        Money unitPrice)
    {
        if (userId == Guid.Empty)
            return Result<PassPurchase>.Failure("User ID is required");

        if (eventId == Guid.Empty)
            return Result<PassPurchase>.Failure("Event ID is required");

        if (eventPassId == Guid.Empty)
            return Result<PassPurchase>.Failure("Event pass ID is required");

        if (quantity <= 0)
            return Result<PassPurchase>.Failure("Quantity must be greater than 0");

        if (unitPrice == null)
            return Result<PassPurchase>.Failure("Unit price is required");

        var purchase = new PassPurchase(userId, eventId, eventPassId, quantity, unitPrice);
        return Result<PassPurchase>.Success(purchase);
    }

    /// <summary>
    /// Confirms the purchase after successful payment
    /// </summary>
    public Result Confirm()
    {
        if (Status == PassPurchaseStatus.Confirmed)
            return Result.Failure("Purchase is already confirmed");

        if (Status == PassPurchaseStatus.Cancelled)
            return Result.Failure("Cannot confirm a cancelled purchase");

        Status = PassPurchaseStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new PassPurchasedEvent(Id, UserId, EventId, EventPassId, Quantity, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Cancels the purchase and initiates refund process
    /// </summary>
    public Result Cancel()
    {
        if (Status == PassPurchaseStatus.Cancelled)
            return Result.Failure("Purchase is already cancelled");

        Status = PassPurchaseStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new PassCancelledEvent(Id, UserId, EventId, Quantity, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Generates a unique QR code for the purchase
    /// Format: {EventId}-{UserId}-{PurchaseId}-{Timestamp}
    /// </summary>
    private string GenerateQRCode()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{EventId:N}-{UserId:N}-{uniqueId}-{timestamp}";
    }
}
