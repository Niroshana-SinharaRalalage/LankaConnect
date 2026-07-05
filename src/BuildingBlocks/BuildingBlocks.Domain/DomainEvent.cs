namespace LankaConnect.BuildingBlocks.Domain;

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}