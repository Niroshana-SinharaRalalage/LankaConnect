using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Analytics;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EventViewRecord entity
/// Handles detailed view tracking for unique viewer calculations
/// </summary>
public class EventViewRecordRepository : IEventViewRecordRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<EventViewRecord> _dbSet;

    public EventViewRecordRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<EventViewRecord>();
    }

    /// <summary>
    /// Add a new view record
    /// </summary>
    public async Task AddAsync(EventViewRecord record, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(record, cancellationToken);
    }

    /// <summary>
    /// Get unique viewer count for an event
    /// Counts distinct user_id (for authenticated) or ip_address (for anonymous)
    /// </summary>
    public async Task<int> GetUniqueViewerCountAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // Count authenticated users
        var authenticatedUsers = await _dbSet
            .AsNoTracking()
            .Where(v => v.EventId == eventId && v.UserId != null)
            .Select(v => v.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Count anonymous users (by IP address, excluding those who later authenticated)
        var authenticatedUserIds = await _dbSet
            .AsNoTracking()
            .Where(v => v.EventId == eventId && v.UserId != null)
            .Select(v => v.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var anonymousIps = await _dbSet
            .AsNoTracking()
            .Where(v => v.EventId == eventId && v.UserId == null)
            .Select(v => v.IpAddress)
            .Distinct()
            .CountAsync(cancellationToken);

        return authenticatedUsers + anonymousIps;
    }

    /// <summary>
    /// Check if a view exists within a time window (for deduplication)
    /// </summary>
    public async Task<bool> ViewExistsInWindowAsync(
        Guid eventId,
        Guid? userId,
        string ipAddress,
        DateTime windowStart,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(v => v.EventId == eventId)
            .Where(v => v.ViewedAt >= windowStart);

        // Check by user ID if authenticated
        if (userId.HasValue)
        {
            query = query.Where(v => v.UserId == userId);
        }
        else
        {
            // Check by IP address for anonymous users
            query = query.Where(v => v.IpAddress == ipAddress && v.UserId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
