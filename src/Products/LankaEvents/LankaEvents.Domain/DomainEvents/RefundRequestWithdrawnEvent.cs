using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.148: Raised when an attendee withdraws their own pending refund request.
/// Notifies the organizer that the queue item is gone and confirms to the attendee
/// the registration is back to <c>Confirmed</c>.
/// </summary>
public record RefundRequestWithdrawnEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid RefundRequestId,
    Guid WithdrawnByUserId,
    DateTime WithdrawnAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
