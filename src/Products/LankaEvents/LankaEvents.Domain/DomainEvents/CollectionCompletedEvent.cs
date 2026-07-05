using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a collection (event fund) contribution payment is completed via Stripe webhook.
/// Triggers:
/// - Sending contribution receipt email to contributor
/// </summary>
public record CollectionCompletedEvent(
    Guid EventId,
    Guid CollectionId,
    Guid? ContributorUserId,
    string ContributorName,
    string ContributorEmail,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    DateTime PaymentCompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
