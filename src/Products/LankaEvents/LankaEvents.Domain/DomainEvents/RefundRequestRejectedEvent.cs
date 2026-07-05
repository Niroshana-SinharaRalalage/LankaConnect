using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.148: Raised when an organizer declines a refund request. Triggers the
/// declined-refund email to the attendee with the supplied <c>RejectionReason</c>.
/// </summary>
public record RefundRequestRejectedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid RefundRequestId,
    Guid OrganizerUserId,
    string RejectionReason,
    DateTime RejectedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
