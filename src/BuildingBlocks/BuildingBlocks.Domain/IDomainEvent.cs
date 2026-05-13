namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Marker for in-process domain events. Implementations are typically
/// <c>record</c>s carrying the data downstream handlers need.
/// </summary>
/// <remarks>
/// <para>
/// Domain events stay <b>inside</b> the aggregate that raised them and the
/// handlers in the same module. <b>Cross-module</b> integration is the job of
/// <c>BuildingBlocks.Contracts.IntegrationEventV1</c> (W2.7), which uses the
/// outbox + dispatcher pattern. Mixing the two leaks aggregate internals
/// into other modules and is enforced against by ArchTest.
/// </para>
/// </remarks>
public interface IDomainEvent
{
    /// <summary>UTC timestamp the event was raised.</summary>
    DateTime OccurredAt { get; }
}
