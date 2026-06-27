using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.91: Domain event raised when Stripe confirms a refund has been processed.
/// This event triggers:
/// - Sending refund completion confirmation email to user
/// - Notifying event organizer that refund is complete
/// - Updating any financial reports or dashboards
/// Phase 6A.135: Added AddOnRefundAmount so completion email shows combined total.
/// </summary>
public record RefundCompletedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    string ContactEmail,
    string StripeRefundId,
    decimal RefundAmount,
    DateTime RefundCompletedAt,
    decimal AddOnRefundAmount = 0m
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
