using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for Collection (event fund) operations.
/// </summary>
public interface ICollectionRepository : IRepository<Collection>
{
    Task<Collection?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Collection>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Collection>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalCollectedForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<int> GetContributorCountForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Collection>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Collection>> GetExpiredPendingCollectionsAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default);
}
