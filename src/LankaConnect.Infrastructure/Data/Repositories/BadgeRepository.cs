using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Badges;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Badge aggregate root
/// Phase 6A.X: Enhanced with comprehensive logging pattern
/// </summary>
public class BadgeRepository : Repository<Badge>, IBadgeRepository
{
    private readonly ILogger<BadgeRepository> _repoLogger;

    public BadgeRepository(
        AppDbContext context,
        ILogger<BadgeRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IEnumerable<Badge>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllActive"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetAllActiveAsync START");

            try
            {
                var badges = await _dbSet
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.DisplayOrder)
                    .ThenBy(b => b.Name)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAllActiveAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    badges.Count,
                    stopwatch.ElapsedMilliseconds);

                return badges;
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

    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<Badge?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByName"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        using (LogContext.PushProperty("BadgeName", name))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByNameAsync START: BadgeName={BadgeName}", name);

            try
            {
                var badge = await _dbSet
                    .Where(b => b.Name.ToLower() == name.ToLower())
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByNameAsync COMPLETE: BadgeName={BadgeName}, Found={Found}, Duration={ElapsedMs}ms",
                    name,
                    badge != null,
                    stopwatch.ElapsedMilliseconds);

                return badge;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByNameAsync FAILED: BadgeName={BadgeName}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    name,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "ExistsByName"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        using (LogContext.PushProperty("BadgeName", name))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("ExistsByNameAsync START: BadgeName={BadgeName}", name);

            try
            {
                var exists = await _dbSet
                    .AnyAsync(b => b.Name.ToLower() == name.ToLower(), cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "ExistsByNameAsync COMPLETE: BadgeName={BadgeName}, Exists={Exists}, Duration={ElapsedMs}ms",
                    name,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "ExistsByNameAsync FAILED: BadgeName={BadgeName}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    name,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetNextDisplayOrder"))
        using (LogContext.PushProperty("EntityType", "Badge"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetNextDisplayOrderAsync START");

            try
            {
                var maxDisplayOrder = await _dbSet
                    .MaxAsync(b => (int?)b.DisplayOrder, cancellationToken) ?? 0;

                var nextOrder = maxDisplayOrder + 1;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetNextDisplayOrderAsync COMPLETE: MaxDisplayOrder={MaxDisplayOrder}, NextOrder={NextOrder}, Duration={ElapsedMs}ms",
                    maxDisplayOrder,
                    nextOrder,
                    stopwatch.ElapsedMilliseconds);

                return nextOrder;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetNextDisplayOrderAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
