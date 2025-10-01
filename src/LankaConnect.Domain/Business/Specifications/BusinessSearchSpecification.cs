using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.Specifications;

public class BusinessSearchSpecification : ISpecification<Business>
{
    private readonly string? _searchTerm;
    private readonly string? _category;
    private readonly string? _city;
    private readonly string? _province;
    private readonly double? _latitude;
    private readonly double? _longitude;
    private readonly double? _radiusKm;
    private readonly decimal? _minRating;
    private readonly bool? _isVerified;

    public BusinessSearchSpecification(
        string? searchTerm,
        string? category,
        string? city,
        string? province,
        double? latitude,
        double? longitude,
        double? radiusKm,
        decimal? minRating,
        bool? isVerified)
    {
        _searchTerm = searchTerm;
        _category = category;
        _city = city;
        _province = province;
        _latitude = latitude;
        _longitude = longitude;
        _radiusKm = radiusKm;
        _minRating = minRating;
        _isVerified = isVerified;
    }

    public bool IsSatisfiedBy(Business business)
    {
        // Search term filter
        if (!string.IsNullOrEmpty(_searchTerm))
        {
            var searchLower = _searchTerm.ToLower();
            if (!business.Profile.Name.ToLower().Contains(searchLower) &&
                !business.Profile.Description.ToLower().Contains(searchLower))
                return false;
        }

        // Category filter
        if (!string.IsNullOrEmpty(_category) &&
            !business.Category.ToString().Equals(_category, StringComparison.OrdinalIgnoreCase))
            return false;

        // City filter
        if (!string.IsNullOrEmpty(_city) &&
            !business.Location.Address.City.Equals(_city, StringComparison.OrdinalIgnoreCase))
            return false;

        // Province filter
        if (!string.IsNullOrEmpty(_province) &&
            !business.Location.Address.State.Equals(_province, StringComparison.OrdinalIgnoreCase))
            return false;

        // Rating filter
        if (_minRating.HasValue && business.Rating.HasValue &&
            business.Rating.Value < _minRating.Value)
            return false;

        // Verification filter
        if (_isVerified.HasValue && business.IsVerified != _isVerified.Value)
            return false;

        // Location/distance filter (simplified - in production would use proper geospatial queries)
        if (_latitude.HasValue && _longitude.HasValue && _radiusKm.HasValue && business.Location.Coordinates != null)
        {
            var distance = CalculateDistance(_latitude.Value, _longitude.Value,
                (double)business.Location.Coordinates.Latitude, (double)business.Location.Coordinates.Longitude);
            if (distance > _radiusKm.Value)
                return false;
        }

        return true;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula for calculating distance between two points
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}