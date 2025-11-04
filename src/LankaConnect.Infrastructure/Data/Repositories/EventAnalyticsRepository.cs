using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EventAnalytics aggregate
/// Provides data access for analytics tracking
/// </summary>
public class EventAnalyticsRepository : Repository<EventAnalytics>, IEventAnalyticsRepository
{
    public EventAnalyticsRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get analytics for a specific event
    /// </summary>
    public async Task<EventAnalytics?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.EventId == eventId, cancellationToken);
    }

    /// <summary>
    /// Check if a view should be counted (deduplication logic)
    /// Returns true if this view should be counted, false if it's a duplicate within the window
    /// </summary>
    public async Task<bool> ShouldCountViewAsync(
        Guid eventId,
        Guid? userId,
        string ipAddress,
        TimeSpan deduplicationWindow,
        CancellationToken cancellationToken = default)
    {
        var windowStart = DateTime.UtcNow.Subtract(deduplicationWindow);

        // Query event_view_records table for recent views
        var recentView = await _context.Set<EventViewRecord>()
            .AsNoTracking()
            .Where(v => v.EventId == eventId)
            .Where(v => v.ViewedAt >= windowStart)
            .Where(v =>
                (userId.HasValue && v.UserId == userId) ||
                (!userId.HasValue && v.IpAddress == ipAddress))
            .AnyAsync(cancellationToken);

        return !recentView; // Should count if NO recent view exists
    }

    /// <summary>
    /// Update the unique viewer count for an event
    /// Called asynchronously after view records are processed
    /// </summary>
    public async Task UpdateUniqueViewerCountAsync(Guid eventId, int count, CancellationToken cancellationToken = default)
    {
        var analytics = await _dbSet
            .FirstOrDefaultAsync(a => a.EventId == eventId, cancellationToken);

        if (analytics != null)
        {
            analytics.UpdateUniqueViewers(count);
            _dbSet.Update(analytics);
        }
    }

    /// <summary>
    /// Get aggregated analytics for an organizer (all their events)
    /// Used for organizer dashboard
    /// </summary>
    public async Task<OrganizerDashboardData?> GetOrganizerDashboardDataAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        // Get all events for the organizer
        var events = await _context.Set<Domain.Events.Event>()
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .Select(e => new { e.Id, e.Title, e.StartDate })
            .ToListAsync(cancellationToken);

        if (!events.Any())
            return null;

        var eventIds = events.Select(e => e.Id).ToList();

        // Get analytics for all organizer events
        var analytics = await _dbSet
            .AsNoTracking()
            .Where(a => eventIds.Contains(a.EventId))
            .ToListAsync(cancellationToken);

        if (!analytics.Any())
            return null;

        // Get registrations count for all events
        var registrations = await _context.Set<Domain.Events.Registration>()
            .AsNoTracking()
            .Where(r => eventIds.Contains(r.EventId))
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var registrationDict = registrations.ToDictionary(r => r.EventId, r => r.Count);

        // Build dashboard data
        var dashboardData = new OrganizerDashboardData
        {
            OrganizerId = organizerId,
            TotalEvents = events.Count,
            TotalViews = analytics.Sum(a => a.TotalViews),
            TotalUniqueViewers = analytics.Sum(a => a.UniqueViewers),
            TotalRegistrations = registrationDict.Values.Sum(),
            AverageConversionRate = analytics.Any()
                ? Math.Round(analytics.Average(a => a.ConversionRate), 2)
                : 0,
            LastActivityAt = analytics.Max(a => a.LastViewedAt),
            TopEvents = analytics
                .OrderByDescending(a => a.TotalViews)
                .Take(5)
                .Select(a => new EventAnalyticsSummary
                {
                    EventId = a.EventId,
                    Title = events.First(e => e.Id == a.EventId).Title.Value,
                    EventDate = events.First(e => e.Id == a.EventId).StartDate,
                    Views = a.TotalViews,
                    Registrations = registrationDict.GetValueOrDefault(a.EventId, 0),
                    ConversionRate = a.ConversionRate
                })
                .ToList(),
            UpcomingEvents = events
                .Where(e => e.StartDate > DateTime.UtcNow)
                .OrderBy(e => e.StartDate)
                .Take(5)
                .Select(e => new EventAnalyticsSummary
                {
                    EventId = e.Id,
                    Title = e.Title.Value,
                    EventDate = e.StartDate,
                    Views = analytics.FirstOrDefault(a => a.EventId == e.Id)?.TotalViews ?? 0,
                    Registrations = registrationDict.GetValueOrDefault(e.Id, 0),
                    ConversionRate = analytics.FirstOrDefault(a => a.EventId == e.Id)?.ConversionRate ?? 0
                })
                .ToList()
        };

        return dashboardData;
    }
}
