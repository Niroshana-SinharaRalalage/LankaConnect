using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for AddOnPurchase operations.
/// </summary>
public interface IAddOnPurchaseRepository : IRepository<AddOnPurchase>
{
    Task<AddOnPurchase?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetByDefinitionIdAsync(
        Guid definitionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalPurchasesForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnPurchase>> GetExpiredPendingPurchasesAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default);
}
