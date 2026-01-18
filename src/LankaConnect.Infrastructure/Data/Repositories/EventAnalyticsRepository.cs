using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EventAnalytics aggregate
/// Provides data access for analytics tracking
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EventAnalyticsRepository : Repository<EventAnalytics>, IEventAnalyticsRepository
{
    private readonly ILogger<EventAnalyticsRepository> _repoLogger;

    public EventAnalyticsRepository(
        AppDbContext context,
        ILogger<EventAnalyticsRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Get analytics for a specific event
    /// </summary>
    public async Task<EventAnalytics?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "EventAnalytics"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventIdAsync START: EventId={EventId}", eventId);

            try
            {
                var analytics = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.EventId == eventId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Found={Found}, Duration={ElapsedMs}ms",
                    eventId,
                    analytics != null,
                    stopwatch.ElapsedMilliseconds);

                return analytics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
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
        using (LogContext.PushProperty("Operation", "ShouldCountView"))
        using (LogContext.PushProperty("EntityType", "EventAnalytics"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        using (LogContext.PushProperty("DeduplicationWindowMinutes", deduplicationWindow.TotalMinutes))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "ShouldCountViewAsync START: EventId={EventId}, UserId={UserId}, IpAddress={IpAddress}, Window={WindowMinutes}min",
                eventId, userId, ipAddress, deduplicationWindow.TotalMinutes);

            try
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

                var shouldCount = !recentView; // Should count if NO recent view exists
                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "ShouldCountViewAsync COMPLETE: EventId={EventId}, UserId={UserId}, RecentViewFound={RecentViewFound}, ShouldCount={ShouldCount}, Duration={ElapsedMs}ms",
                    eventId,
                    userId,
                    recentView,
                    shouldCount,
                    stopwatch.ElapsedMilliseconds);

                return shouldCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "ShouldCountViewAsync FAILED: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Update the unique viewer count for an event
    /// Called asynchronously after view records are processed
    /// </summary>
    public async Task UpdateUniqueViewerCountAsync(Guid eventId, int count, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "UpdateUniqueViewerCount"))
        using (LogContext.PushProperty("EntityType", "EventAnalytics"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("Count", count))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("UpdateUniqueViewerCountAsync START: EventId={EventId}, Count={Count}", eventId, count);

            try
            {
                var analytics = await _dbSet
                    .FirstOrDefaultAsync(a => a.EventId == eventId, cancellationToken);

                if (analytics != null)
                {
                    analytics.UpdateUniqueViewers(count);
                    _dbSet.Update(analytics);
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "UpdateUniqueViewerCountAsync COMPLETE: EventId={EventId}, Count={Count}, AnalyticsFound={Found}, Duration={ElapsedMs}ms",
                    eventId,
                    count,
                    analytics != null,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "UpdateUniqueViewerCountAsync FAILED: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    count,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
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
        using (LogContext.PushProperty("Operation", "GetOrganizerDashboardData"))
        using (LogContext.PushProperty("EntityType", "EventAnalytics"))
        using (LogContext.PushProperty("OrganizerId", organizerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetOrganizerDashboardDataAsync START: OrganizerId={OrganizerId}", organizerId);

            try
            {
                // Get all events for the organizer
                var events = await _context.Set<Domain.Events.Event>()
                    .AsNoTracking()
                    .Where(e => e.OrganizerId == organizerId)
                    .Select(e => new { e.Id, e.Title, e.StartDate })
                    .ToListAsync(cancellationToken);

                if (!events.Any())
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetOrganizerDashboardDataAsync COMPLETE: OrganizerId={OrganizerId}, EventCount=0, Result=null, Duration={ElapsedMs}ms",
                        organizerId,
                        stopwatch.ElapsedMilliseconds);
                    return null;
                }

                var eventIds = events.Select(e => e.Id).ToList();

                // Get analytics for all organizer events
                var analytics = await _dbSet
                    .AsNoTracking()
                    .Where(a => eventIds.Contains(a.EventId))
                    .ToListAsync(cancellationToken);

                if (!analytics.Any())
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetOrganizerDashboardDataAsync COMPLETE: OrganizerId={OrganizerId}, EventCount={EventCount}, AnalyticsCount=0, Result=null, Duration={ElapsedMs}ms",
                        organizerId,
                        events.Count,
                        stopwatch.ElapsedMilliseconds);
                    return null;
                }

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

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetOrganizerDashboardDataAsync COMPLETE: OrganizerId={OrganizerId}, EventCount={EventCount}, TotalViews={TotalViews}, TotalRegistrations={TotalRegistrations}, Duration={ElapsedMs}ms",
                    organizerId,
                    dashboardData.TotalEvents,
                    dashboardData.TotalViews,
                    dashboardData.TotalRegistrations,
                    stopwatch.ElapsedMilliseconds);

                return dashboardData;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetOrganizerDashboardDataAsync FAILED: OrganizerId={OrganizerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    organizerId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
