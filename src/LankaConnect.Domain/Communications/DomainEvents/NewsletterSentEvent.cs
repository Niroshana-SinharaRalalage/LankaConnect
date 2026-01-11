using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Domain event raised when newsletter emails are successfully sent
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public record NewsletterSentEvent(
    Guid NewsletterId,
    DateTime SentAt,
    int RecipientCount
) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
