using LankaConnect.BuildingBlocks.Domain.Contracts;

namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Restored Day 5 per Consult #12: base record for domain events.
/// Provides IDomainEvent (Id + OccurredOn) with sensible defaults.
/// Sub-events inherit as records: `public record MyEvent(...) : DomainEvent;`
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
