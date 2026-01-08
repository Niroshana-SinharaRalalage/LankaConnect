using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Infrastructure.Common;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for MetroArea entity
/// Provides case-insensitive location-based queries for metro areas
/// Note: MetroArea is a read-only entity loaded from events.metro_areas table
/// Phase 6A Event Notifications: Supports both full state names and abbreviations
/// </summary>
public class MetroAreaRepository : Repository<MetroArea>, IMetroAreaRepository
{
    public MetroAreaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<MetroArea?> FindByLocationAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "FindByLocation"))
        using (LogContext.PushProperty("City", city))
        using (LogContext.PushProperty("State", state))
        {
            _logger.Debug("Finding metro area for city {City}, state {State}", city, state);

            // Phase 6A Fix: Normalize state to abbreviation for matching
            // Database stores 2-letter abbreviations (e.g., "CA"), but events may have full names (e.g., "California")
            var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

            _logger.Debug("Normalized state {State} to abbreviation {Abbreviation}", state, stateAbbreviation ?? "null");

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

            _logger.Debug("Metro area for {City}, {State} {Result}",
                city, state, result != null ? "found" : "not found");

            return result;
        }
    }

    /// <summary>
    /// Phase 6A.70: Gets all active metro areas in a state for geo-spatial matching
    /// </summary>
    public async Task<IReadOnlyList<MetroArea>> GetMetroAreasInStateAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetMetroAreasInState"))
        using (LogContext.PushProperty("State", state))
        {
            _logger.Debug("[Phase 6A.70] Getting all metro areas for state {State}", state);

            // Normalize state to abbreviation for matching
            var stateAbbreviation = USStateHelper.NormalizeToAbbreviation(state);

            _logger.Debug("[Phase 6A.70] Normalized state {State} to abbreviation {Abbreviation}",
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

            _logger.Debug("[Phase 6A.70] Found {Count} active metro areas for state {State}",
                result.Count, state);

            return result;
        }
    }
}
