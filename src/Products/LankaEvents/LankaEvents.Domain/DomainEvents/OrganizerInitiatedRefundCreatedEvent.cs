using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.148: Raised when an organizer initiates a refund on behalf of an attendee.
/// Skips the Pending state — request is created directly in Approved. Triggers the
/// approved-refund email to the attendee and queues Stripe dispatch via
/// <c>RefundExecutionService</c>.
/// </summary>
public record OrganizerInitiatedRefundCreatedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid RefundRequestId,
    Guid OrganizerUserId,
    string? OrganizerNotes,
    bool ScanGuardOverridden,
    DateTime CreatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
