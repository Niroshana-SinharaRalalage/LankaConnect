using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.91: Domain event raised when a user withdraws their refund request.
/// This event is primarily for audit trail purposes.
/// The user keeps their registration and the Stripe refund is cancelled (if not yet processed).
/// </summary>
public record RefundWithdrawnEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    string ContactEmail,
    DateTime RefundWithdrawnAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
