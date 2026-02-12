using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Raised when a respondent submits a response to an event form.
/// </summary>
public record FormResponseSubmittedEvent(
    Guid FormId,
    Guid ResponseId,
    string? RespondentEmail,
    DateTime OccurredAt) : IDomainEvent;
