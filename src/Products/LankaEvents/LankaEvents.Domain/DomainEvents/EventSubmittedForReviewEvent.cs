using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record EventSubmittedForReviewEvent(
    Guid EventId,
    DateTime SubmittedAt,
    bool RequiresCulturalApproval
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}