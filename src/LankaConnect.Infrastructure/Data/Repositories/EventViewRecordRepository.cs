using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Analytics;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EventViewRecord entity
/// Handles detailed view tracking for unique viewer calculations
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EventViewRecordRepository : IEventViewRecordRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<EventViewRecord> _dbSet;
    private readonly ILogger<EventViewRecordRepository> _repoLogger;

    public EventViewRecordRepository(
        AppDbContext context,
        ILogger<EventViewRecordRepository> logger)
    {
        _context = context;
        _dbSet = context.Set<EventViewRecord>();
        _repoLogger = logger;
    }

    /// <summary>
    /// Add a new view record
    /// </summary>
    public async Task AddAsync(EventViewRecord record, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "EventViewRecord"))
        using (LogContext.PushProperty("EventId", record.EventId))
        using (LogContext.PushProperty("UserId", record.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("AddAsync START: EventId={EventId}, UserId={UserId}", record.EventId, record.UserId);

            try
            {
                await _dbSet.AddAsync(record, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "AddAsync COMPLETE: EventId={EventId}, UserId={UserId}, IpAddress={IpAddress}, Duration={ElapsedMs}ms",
                    record.EventId,
                    record.UserId,
                    record.IpAddress,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "AddAsync FAILED: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    record.EventId,
                    record.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Get unique viewer count for an event
    /// Counts distinct user_id (for authenticated) or ip_address (for anonymous)
    /// </summary>
    public async Task<int> GetUniqueViewerCountAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUniqueViewerCount"))
        using (LogContext.PushProperty("EntityType", "EventViewRecord"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetUniqueViewerCountAsync START: EventId={EventId}", eventId);

            try
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

                var totalUniqueViewers = authenticatedUsers + anonymousIps;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetUniqueViewerCountAsync COMPLETE: EventId={EventId}, AuthenticatedUsers={AuthenticatedUsers}, AnonymousIps={AnonymousIps}, TotalUniqueViewers={TotalUniqueViewers}, Duration={ElapsedMs}ms",
                    eventId,
                    authenticatedUsers,
                    anonymousIps,
                    totalUniqueViewers,
                    stopwatch.ElapsedMilliseconds);

                return totalUniqueViewers;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetUniqueViewerCountAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
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
        using (LogContext.PushProperty("Operation", "ViewExistsInWindow"))
        using (LogContext.PushProperty("EntityType", "EventViewRecord"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        using (LogContext.PushProperty("WindowStart", windowStart))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "ViewExistsInWindowAsync START: EventId={EventId}, UserId={UserId}, IpAddress={IpAddress}, WindowStart={WindowStart}",
                eventId, userId, ipAddress, windowStart);

            try
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

                var exists = await query.AnyAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "ViewExistsInWindowAsync COMPLETE: EventId={EventId}, UserId={UserId}, IpAddress={IpAddress}, Exists={Exists}, Duration={ElapsedMs}ms",
                    eventId,
                    userId,
                    ipAddress,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "ViewExistsInWindowAsync FAILED: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
