using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a donation payment is completed via Stripe webhook.
/// Triggers:
/// - Sending donation receipt email to donor
/// </summary>
public record DonationCompletedEvent(
    Guid EventId,
    Guid DonationId,
    Guid? DonorUserId,
    string DonorName,
    string DonorEmail,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    DateTime PaymentCompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
