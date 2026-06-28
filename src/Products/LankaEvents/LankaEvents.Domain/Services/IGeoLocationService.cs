namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Domain service for geographic distance calculations
/// Used for location-based event sorting and filtering
/// Phase 6A.70: Extended to support metro area radius matching for newsletter emails
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

    /// <summary>
    /// Phase 6A.70: Checks if an event location is within a metro area's radius
    /// Used for newsletter subscriber recipient matching
    /// </summary>
    /// <param name="eventLatitude">Event location latitude</param>
    /// <param name="eventLongitude">Event location longitude</param>
    /// <param name="metroLatitude">Metro area center latitude</param>
    /// <param name="metroLongitude">Metro area center longitude</param>
    /// <param name="radiusMiles">Metro area radius in miles</param>
    /// <returns>True if event location is within metro area radius</returns>
    bool IsWithinMetroRadius(
        decimal eventLatitude,
        decimal eventLongitude,
        decimal metroLatitude,
        decimal metroLongitude,
        int radiusMiles);
}
