using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.91: Domain event raised when a user requests a refund for a paid registration.
/// This event triggers:
/// - Sending refund request confirmation email to user
/// - Notifying event organizer of refund request
/// - Initiating Stripe refund process
/// </summary>
public record RefundRequestedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    string ContactEmail,
    string PaymentIntentId,
    decimal RefundAmount,
    DateTime RefundRequestedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
