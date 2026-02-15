using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Raised when a respondent submits a response to an event form.
/// Phase 6A.107 Update: Added AccessToken for email edit link generation.
/// Architect Review: Approved - plaintext token needed for edit URL, never persisted.
/// </summary>
public record FormResponseSubmittedEvent(
    Guid FormId,
    Guid ResponseId,
    string? RespondentEmail,
    string? AccessToken,  // Plaintext token (in-memory only, never persisted)
    DateTime OccurredAt) : IDomainEvent;
