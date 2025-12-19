using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for MetroArea entity
/// Provides case-insensitive location-based queries for metro areas
/// Note: MetroArea is a read-only entity loaded from events.metro_areas table
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

            // Case-insensitive matching for city and state
            var result = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Name.ToLower() == city.ToLower() &&
                         m.State.ToLower() == state.ToLower(),
                    cancellationToken);

            _logger.Debug("Metro area for {City}, {State} {Result}",
                city, state, result != null ? "found" : "not found");

            return result;
        }
    }
}
