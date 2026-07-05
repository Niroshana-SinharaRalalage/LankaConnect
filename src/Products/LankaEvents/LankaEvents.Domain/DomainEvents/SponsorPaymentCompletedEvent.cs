using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a money-based sponsorship payment is completed via Stripe webhook.
/// Triggers:
/// - Sending money sponsor confirmation email
/// </summary>
public record SponsorPaymentCompletedEvent(
    Guid EventId,
    Guid SponsorId,
    Guid? SponsorUserId,
    string SponsorName,
    string SponsorEmail,
    string? SponsorOrganization,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    DateTime PaymentCompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
