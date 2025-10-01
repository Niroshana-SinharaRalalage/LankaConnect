using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published && 
                       e.StartDate > DateTime.UtcNow)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetEventsByStatusAsync(EventStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetEventsWithAvailableCapacityAsync(CancellationToken cancellationToken = default)
    {
        var events = await _dbSet
            .AsNoTracking()
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published && 
                       e.StartDate > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        // Apply business logic filtering on client-side
        return events
            .Where(e => e.HasCapacityFor(1))
            .OrderBy(e => e.StartDate)
            .ToList();
    }

    public async Task<Event?> GetWithRegistrationsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetPublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }
}