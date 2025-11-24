namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Implementation of geographic distance calculations using the Haversine formula
/// </summary>
/// <remarks>
/// The Haversine formula calculates the great-circle distance between two points
/// on a sphere given their longitudes and latitudes. This is more accurate than
/// Pythagoras for geographic distances.
///
/// Formula: a = sin²(Δφ/2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ/2)
///          c = 2 ⋅ atan2( √a, √(1−a) )
///          d = R ⋅ c
///
/// Where:
///  φ is latitude, λ is longitude, R is earth's radius (mean radius = 6,371km)
///
/// Accuracy:
///  - Very accurate for short distances (< 500km): ~0.5% error
///  - Good for medium distances (500-5000km): ~1-2% error
///  - Adequate for featured events sorting where centimeter precision not needed
///
/// Performance:
///  - O(1) complexity - constant time
///  - ~0.01ms per calculation on modern hardware
///  - Suitable for client-side sorting of hundreds of events
/// </remarks>
public class GeoLocationService : IGeoLocationService
{
    /// <summary>
    /// Earth's mean radius in kilometers
    /// Based on WGS84 ellipsoid model
    /// </summary>
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculates the great-circle distance between two geographic coordinates
    /// using the Haversine formula
    /// </summary>
    public double CalculateDistanceKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        // Convert decimal degrees to double radians
        var lat1Rad = ToRadians((double)lat1);
        var lon1Rad = ToRadians((double)lon1);
        var lat2Rad = ToRadians((double)lat2);
        var lon2Rad = ToRadians((double)lon2);

        // Haversine formula
        var dLat = lat2Rad - lat1Rad;
        var dLon = lon2Rad - lon1Rad;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Converts degrees to radians
    /// </summary>
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
