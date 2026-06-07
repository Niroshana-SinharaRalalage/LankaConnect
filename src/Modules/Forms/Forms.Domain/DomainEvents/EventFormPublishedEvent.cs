using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Forms.Domain.DomainEvents;

/// <summary>
/// Raised when a form transitions from Draft to Active status.
/// </summary>
public record EventFormPublishedEvent(
    Guid EventId,
    Guid FormId,
    DateTime OccurredAt) : IDomainEvent;
