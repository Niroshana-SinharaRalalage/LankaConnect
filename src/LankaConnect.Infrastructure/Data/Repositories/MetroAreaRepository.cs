using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Infrastructure.Common;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for MetroArea entity
/// Provides case-insensitive location-based queries for metro areas
/// Note: MetroArea is a read-only entity loaded from events.metro_areas table
/// Phase 6A Event Notifications: Supports both full state names and abbreviations
/// Phase 6A.X: Enhanced with comprehensive logging pattern
/// </summary>
public class MetroAreaRepository : Repository<MetroArea>, IMetroAreaRepository
{
    private readonly ILogger<MetroAreaRepository> _repoLogger;

    public MetroAreaRepository(
        AppDbContext context,
        ILogger<MetroAreaRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<MetroArea?> FindByLocationAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "FindByLocation"))
        using (LogContext.PushProperty("EntityType", "MetroArea"))
        using (LogContext.PushProperty("City", city))
        using (LogContext.PushProperty("State", state))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("FindByLocationAsync START: City={City}, State={State}", city, state);

            try
            {
                // Phase 6A Fix: Normalize state to abbreviation for matching
                // Database stores 2-letter abbreviations (e.g., "CA"), but events may have full names (e.g., "California")
                var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

                _repoLogger.LogDebug("Normalized state {State} to abbreviation {Abbreviation}", state, stateAbbreviation ?? "null");

                MetroArea? result = null;

                if (!string.IsNullOrEmpty(stateAbbreviation))
                {
                    // Match using normalized abbreviation
                    result = await _dbSet
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            m => m.Name.ToLower() == city.ToLower() &&
                                 m.State.ToLower() == stateAbbreviation.ToLower(),
                            cancellationToken);
                }

                // Fallback: try exact match if normalization failed (for non-US states)
                if (result == null)
                {
                    result = await _dbSet
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            m => m.Name.ToLower() == city.ToLower() &&
                                 m.State.ToLower() == state.ToLower(),
                            cancellationToken);
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "FindByLocationAsync COMPLETE: City={City}, State={State}, StateAbbreviation={StateAbbreviation}, Found={Found}, Duration={ElapsedMs}ms",
                    city,
                    state,
                    stateAbbreviation ?? "null",
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "FindByLocationAsync FAILED: City={City}, State={State}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    city,
                    state,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.70: Gets all active metro areas in a state for geo-spatial matching
    /// Phase 6A.X: Enhanced with comprehensive logging pattern
    /// </summary>
    public async Task<IReadOnlyList<MetroArea>> GetMetroAreasInStateAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetMetroAreasInState"))
        using (LogContext.PushProperty("EntityType", "MetroArea"))
        using (LogContext.PushProperty("State", state))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetMetroAreasInStateAsync START: State={State}", state);

            try
            {
                // Normalize state to abbreviation for matching
                var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

                _repoLogger.LogDebug("Normalized state {State} to abbreviation {Abbreviation}",
                    state, stateAbbreviation ?? "null");

                IReadOnlyList<MetroArea> result;

                if (!string.IsNullOrEmpty(stateAbbreviation))
                {
                    // Match using normalized abbreviation
                    result = await _dbSet
                        .AsNoTracking()
                        .Where(m => m.State.ToLower() == stateAbbreviation.ToLower() && m.IsActive)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    // Fallback: try exact match if normalization failed (for non-US states)
                    result = await _dbSet
                        .AsNoTracking()
                        .Where(m => m.State.ToLower() == state.ToLower() && m.IsActive)
                        .ToListAsync(cancellationToken);
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetMetroAreasInStateAsync COMPLETE: State={State}, StateAbbreviation={StateAbbreviation}, Count={Count}, Duration={ElapsedMs}ms",
                    state,
                    stateAbbreviation ?? "null",
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetMetroAreasInStateAsync FAILED: State={State}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    state,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
