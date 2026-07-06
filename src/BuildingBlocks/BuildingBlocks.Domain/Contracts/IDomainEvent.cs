namespace LankaConnect.BuildingBlocks.Domain.Contracts;

/// <summary>
/// Contract for domain events
/// </summary>
public interface IDomainEvent
{
    // Sprint Day 5 (Consult #12): default interface members so many pre-existing
    // domain events that don't declare Id / OccurredOn still satisfy the contract.
    // Post-sprint refactor: normalize all events to inherit DomainEvent base record.
    Guid Id => Guid.NewGuid();
    DateTime OccurredOn => DateTime.UtcNow;
}