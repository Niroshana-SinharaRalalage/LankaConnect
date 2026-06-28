using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Repository interface for MetroArea entity
/// Provides location-based query methods for metro areas
/// Note: MetroArea is a read-only entity loaded from events.metro_areas table
/// </summary>
public interface IMetroAreaRepository : IRepository<MetroArea>
{
    /// <summary>
    /// Finds a metro area by city and state (case-insensitive)
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="state">State name or abbreviation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Metro area if found, null otherwise</returns>
    Task<MetroArea?> FindByLocationAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.70: Gets all active metro areas in a state with their coordinates and radius
    /// Used for geo-spatial newsletter subscriber matching
    /// </summary>
    /// <param name="state">State name or abbreviation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of metro areas with geo-spatial data</returns>
    Task<IReadOnlyList<MetroArea>> GetMetroAreasInStateAsync(
        string state,
        CancellationToken cancellationToken = default);
}
