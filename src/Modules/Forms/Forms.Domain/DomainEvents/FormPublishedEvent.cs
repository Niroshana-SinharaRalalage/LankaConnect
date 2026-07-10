using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Contracts;

namespace LankaConnect.Modules.Forms.Domain.DomainEvents;

/// <summary>
/// Raised when a form transitions from Draft to Active status.
/// </summary>
public record FormPublishedEvent(
    Guid EventId,
    Guid FormId,
    DateTime OccurredAt) : IDomainEvent;

