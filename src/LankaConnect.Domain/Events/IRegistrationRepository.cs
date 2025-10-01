using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

public interface IRegistrationRepository : IRepository<Registration>
{
    Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default);
    Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}