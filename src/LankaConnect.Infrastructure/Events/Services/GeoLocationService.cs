using LankaConnect.Domain.Events.Services;

namespace LankaConnect.Infrastructure.Events.Services;

/// <summary>
/// Phase 6A.70: Implementation of geographic calculations using Haversine formula
/// Extracted from GetEventsQueryHandler to avoid code duplication and enable testing
/// </summary>
public class GeoLocationService : IGeoLocationService
{
    private const double EarthRadiusKm = 6371.0; // Earth's radius in kilometers
    private const double MilesToKmConversion = 1.60934; // 1 mile = 1.60934 km

    /// <summary>
    /// Calculates distance between two coordinates using Haversine formula
    /// Formula: distance = 2R × arcsin(√(sin²(Δlat/2) + cos(lat1) × cos(lat2) × sin²(Δlon/2)))
    /// where R is Earth's radius (6371 km)
    /// </summary>
    public double CalculateDistanceKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Checks if an event location is within a metro area's radius
    /// </summary>
    public bool IsWithinMetroRadius(
        decimal eventLatitude,
        decimal eventLongitude,
        decimal metroLatitude,
        decimal metroLongitude,
        int radiusMiles)
    {
        if (radiusMiles <= 0)
            return false;

        var distanceKm = CalculateDistanceKm(
            metroLatitude,
            metroLongitude,
            eventLatitude,
            eventLongitude);

        var radiusKm = radiusMiles * MilesToKmConversion;
        return distanceKm <= radiusKm;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
