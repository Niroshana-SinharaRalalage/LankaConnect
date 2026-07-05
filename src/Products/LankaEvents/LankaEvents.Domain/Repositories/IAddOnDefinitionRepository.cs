using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

/// <summary>
/// Repository interface for AddOnDefinition operations.
/// Includes atomic stock management methods using raw SQL.
/// </summary>
public interface IAddOnDefinitionRepository : IRepository<AddOnDefinition>
{
    Task<IReadOnlyList<AddOnDefinition>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnDefinition>> GetActiveByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reserves stock for a purchase using raw SQL.
    /// Returns true if stock was successfully reserved, false if insufficient stock.
    /// Uses: UPDATE ... WHERE quantity_sold + @qty &lt;= quantity_limit
    /// </summary>
    Task<bool> TryReserveStockAsync(
        Guid definitionId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically restores stock after a failed/abandoned/refunded purchase using raw SQL.
    /// Used when a purchase fails, expires, or is refunded.
    /// </summary>
    Task<bool> TryRestoreStockAsync(
        Guid definitionId,
        int quantity,
        CancellationToken cancellationToken = default);
}
