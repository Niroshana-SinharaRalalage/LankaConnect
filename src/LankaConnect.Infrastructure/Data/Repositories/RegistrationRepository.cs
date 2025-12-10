using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class RegistrationRepository : Repository<Registration>, IRegistrationRepository
{
    public RegistrationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId &&
                       r.Status != RegistrationStatus.Cancelled &&
                       r.Status != RegistrationStatus.Refunded)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Only return active registrations (exclude cancelled and refunded)
        // This fixes the multi-attendee re-registration issue (Session 30)
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId &&
                                     r.UserId == userId &&
                                     r.Status != RegistrationStatus.Cancelled &&
                                     r.Status != RegistrationStatus.Refunded, cancellationToken);
    }

    public async Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
            .SumAsync(r => r.Quantity, cancellationToken);
    }
}