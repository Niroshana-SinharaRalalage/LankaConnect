namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Domain service for geographic distance calculations
/// Used for location-based event sorting and filtering
/// </summary>
public interface IGeoLocationService
{
    /// <summary>
    /// Calculates the great-circle distance between two geographic coordinates
    /// using the Haversine formula
    /// </summary>
    /// <param name="lat1">Latitude of first point in decimal degrees</param>
    /// <param name="lon1">Longitude of first point in decimal degrees</param>
    /// <param name="lat2">Latitude of second point in decimal degrees</param>
    /// <param name="lon2">Longitude of second point in decimal degrees</param>
    /// <returns>Distance in kilometers</returns>
    /// <remarks>
    /// Accuracy: ~0.5% error for distances under 500km
    /// Uses Earth radius of 6371 km
    /// Suitable for featured events sorting where exact precision is not critical
    /// </remarks>
    double CalculateDistanceKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
}
