namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Scoped buffer of integration events raised by aggregate roots during a
/// request. Concrete implementation lives with <c>BaseDbContext</c> (W2.5)
/// which can collect events from tracked entities on SaveChanges.
/// </summary>
/// <remarks>
/// Extracted from <c>OutboxBehavior.cs</c> in W1A (2026-06-04) per
/// architect ruling: this is a contract, not a behavior, so it belongs
/// in <c>BuildingBlocks.Abstractions</c> alongside the other cross-cutting
/// interfaces.
/// </remarks>
public interface IIntegrationEventBuffer
{
    /// <summary>
    /// Returns + clears the buffered events. Subsequent calls within the same
    /// scope return empty.
    /// </summary>
    IReadOnlyList<object> DrainEvents();
}
