using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Raised when a respondent edits their existing response.
/// </summary>
public record FormResponseUpdatedEvent(
    Guid FormId,
    Guid ResponseId,
    DateTime OccurredAt) : IDomainEvent;
