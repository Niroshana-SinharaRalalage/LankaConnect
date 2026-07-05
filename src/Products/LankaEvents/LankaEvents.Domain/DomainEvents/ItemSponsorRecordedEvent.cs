using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when an item-based sponsorship is recorded (no payment, immediate recording).
/// Triggers:
/// - Sending item sponsor acknowledgment email
/// </summary>
public record ItemSponsorRecordedEvent(
    Guid EventId,
    Guid SponsorId,
    Guid? SponsorUserId,
    string SponsorName,
    string SponsorEmail,
    string? SponsorOrganization,
    string ItemName,
    string? ItemDescription,
    decimal? EstimatedValue,
    DateTime RecordedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
