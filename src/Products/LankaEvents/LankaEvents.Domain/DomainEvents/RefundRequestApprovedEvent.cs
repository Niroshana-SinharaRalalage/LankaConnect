using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.148: Raised when an organizer approves an attendee refund request.
/// Triggers the approved-refund email and queues Stripe dispatch via
/// <c>RefundExecutionService</c> (architect F10 — dispatch runs OUTSIDE the approve
/// transaction in a fresh DbContext scope to avoid holding a write tx across Stripe HTTP).
/// </summary>
public record RefundRequestApprovedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid RefundRequestId,
    Guid OrganizerUserId,
    string? OrganizerNotes,
    DateTime ApprovedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
