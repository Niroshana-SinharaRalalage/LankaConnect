namespace LankaConnect.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}