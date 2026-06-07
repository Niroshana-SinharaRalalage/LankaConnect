using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Forms.Domain.DomainEvents;

/// <summary>
/// Raised when a new custom form is created for an event.
/// </summary>
public record EventFormCreatedEvent(
    Guid EventId,
    Guid FormId,
    string Title,
    DateTime OccurredAt) : IDomainEvent;
