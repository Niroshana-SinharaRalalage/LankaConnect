namespace LankaConnect.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}