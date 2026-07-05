namespace LankaConnect.BuildingBlocks.Domain.Contracts;

/// <summary>
/// Contract for domain events
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}