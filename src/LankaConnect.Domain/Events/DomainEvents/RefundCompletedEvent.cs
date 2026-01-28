using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.91: Domain event raised when Stripe confirms a refund has been processed.
/// This event triggers:
/// - Sending refund completion confirmation email to user
/// - Notifying event organizer that refund is complete
/// - Updating any financial reports or dashboards
/// </summary>
public record RefundCompletedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    string ContactEmail,
    string StripeRefundId,
    decimal RefundAmount,
    DateTime RefundCompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
