using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Forms.Domain.DomainEvents;

/// <summary>
/// Raised when a form transitions from Active to Closed status.
/// </summary>
public record FormClosedEvent(
    Guid EventId,
    Guid FormId,
    DateTime OccurredAt) : IDomainEvent;
