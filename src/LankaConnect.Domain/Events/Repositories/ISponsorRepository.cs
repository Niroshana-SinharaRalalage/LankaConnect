using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for Sponsor operations.
/// </summary>
public interface ISponsorRepository : IRepository<Sponsor>
{
    Task<Sponsor?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sponsor>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sponsor>> GetByEventIdAndTypeAsync(
        Guid eventId,
        SponsorType type,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sponsor>> GetCompletedMoneySponsorsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sponsor>> GetRecordedItemSponsorsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalMoneySponsoredForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sponsor>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}
