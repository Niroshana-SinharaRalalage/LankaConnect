using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record EventSubmittedForReviewEvent(
    Guid EventId,
    DateTime SubmittedAt,
    bool RequiresCulturalApproval
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}