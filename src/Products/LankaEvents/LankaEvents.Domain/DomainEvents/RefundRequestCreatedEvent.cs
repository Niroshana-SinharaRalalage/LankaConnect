using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.148: Raised when an attendee submits a refund request. Triggers email to
/// the event organizer(s) and a confirmation email to the requesting attendee.
/// </summary>
public record RefundRequestCreatedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid RefundRequestId,
    Guid RequestedByUserId,
    string? RequesterReason,
    DateTime RequestedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
