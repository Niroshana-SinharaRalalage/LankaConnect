using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Raised when a respondent edits their existing response.
/// Phase 6A.114: Added Form and Event entities to eliminate duplicate queries in email handler.
/// Performance optimization: Reduces email handler from 40s → 5-8s by passing already-loaded data.
/// </summary>
public record FormResponseUpdatedEvent(
    Guid FormId,
    Guid ResponseId,
    DateTime OccurredAt,
    EventForm Form,
    Event Event) : IDomainEvent;
