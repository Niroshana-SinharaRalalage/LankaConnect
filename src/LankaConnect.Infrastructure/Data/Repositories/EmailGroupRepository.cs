using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailGroup entity
/// Phase 6A.25: Email Groups Management
/// Phase 6A.X: Enhanced with comprehensive logging pattern
/// </summary>
public class EmailGroupRepository : Repository<EmailGroup>, IEmailGroupRepository
{
    private readonly ILogger<EmailGroupRepository> _repoLogger;

    public EmailGroupRepository(
        AppDbContext context,
        ILogger<EmailGroupRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Gets all email groups owned by a specific user
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<EmailGroup>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByOwner"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        using (LogContext.PushProperty("OwnerId", ownerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByOwnerAsync START: OwnerId={OwnerId}", ownerId);

            try
            {
                var groups = await _dbSet
                    .AsNoTracking()
                    .Where(g => g.OwnerId == ownerId && g.IsActive)
                    .OrderByDescending(g => g.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByOwnerAsync COMPLETE: OwnerId={OwnerId}, Count={Count}, Duration={ElapsedMs}ms",
                    ownerId,
                    groups.Count,
                    stopwatch.ElapsedMilliseconds);

                return groups;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByOwnerAsync FAILED: OwnerId={OwnerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ownerId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Gets all active email groups (for admin view)
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<EmailGroup>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllActive"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetAllActiveAsync START");

            try
            {
                var groups = await _dbSet
                    .AsNoTracking()
                    .Where(g => g.IsActive)
                    .OrderByDescending(g => g.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAllActiveAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    groups.Count,
                    stopwatch.ElapsedMilliseconds);

                return groups;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAllActiveAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Checks if a group with the given name already exists for the owner
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<bool> NameExistsForOwnerAsync(
        Guid ownerId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "NameExistsForOwner"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        using (LogContext.PushProperty("OwnerId", ownerId))
        using (LogContext.PushProperty("Name", name))
        using (LogContext.PushProperty("ExcludeId", excludeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "NameExistsForOwnerAsync START: OwnerId={OwnerId}, Name={Name}, ExcludeId={ExcludeId}",
                ownerId, name, excludeId);

            try
            {
                var normalizedName = name.Trim().ToLowerInvariant();

                var query = _dbSet
                    .Where(g => g.OwnerId == ownerId && g.IsActive);

                // Exclude the current group when checking for duplicates (for update scenarios)
                if (excludeId.HasValue)
                {
                    query = query.Where(g => g.Id != excludeId.Value);
                }

                var exists = await query
                    .AnyAsync(g => g.Name.ToLower() == normalizedName, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "NameExistsForOwnerAsync COMPLETE: OwnerId={OwnerId}, Name={Name}, ExcludeId={ExcludeId}, Exists={Exists}, Duration={ElapsedMs}ms",
                    ownerId,
                    name,
                    excludeId,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "NameExistsForOwnerAsync FAILED: OwnerId={OwnerId}, Name={Name}, ExcludeId={ExcludeId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ownerId,
                    name,
                    excludeId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Gets multiple email groups by their IDs in a single query
    /// Phase 6A.32: Batch query to prevent N+1 problem (Fix #3)
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// Used by events to fetch multiple email groups efficiently
    /// PostgreSQL optimizes WHERE id IN (...) queries very well
    /// </summary>
    public async Task<IReadOnlyList<EmailGroup>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIds"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByIdsAsync START: IdCount={IdCount}", ids?.Count() ?? 0);

            try
            {
                if (ids == null || !ids.Any())
                {
                    stopwatch.Stop();

                    _repoLogger.LogInformation(
                        "GetByIdsAsync COMPLETE: IdCount=0, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Array.Empty<EmailGroup>();
                }

                // Convert to list to avoid multiple enumeration
                var idList = ids.ToList();

                using (LogContext.PushProperty("IdCount", idList.Count))
                {
                    var groups = await _dbSet
                        .AsNoTracking()
                        .Where(g => idList.Contains(g.Id))
                        .ToListAsync(cancellationToken);

                    stopwatch.Stop();

                    _repoLogger.LogInformation(
                        "GetByIdsAsync COMPLETE: IdCount={IdCount}, Count={Count}, Duration={ElapsedMs}ms",
                        idList.Count,
                        groups.Count,
                        stopwatch.ElapsedMilliseconds);

                    return groups;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdsAsync FAILED: IdCount={IdCount}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ids?.Count() ?? 0,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
