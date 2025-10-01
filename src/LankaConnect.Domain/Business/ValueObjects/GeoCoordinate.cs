using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class GeoCoordinate : ValueObject
{
    public decimal Latitude { get; }
    public decimal Longitude { get; }

    private GeoCoordinate(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Result<GeoCoordinate> Create(decimal latitude, decimal longitude)
    {
        if (latitude < -90 || latitude > 90)
            return Result<GeoCoordinate>.Failure("Latitude must be between -90 and 90 degrees");

        if (longitude < -180 || longitude > 180)
            return Result<GeoCoordinate>.Failure("Longitude must be between -180 and 180 degrees");

        return Result<GeoCoordinate>.Success(new GeoCoordinate(latitude, longitude));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"{Latitude}, {Longitude}";

    public double DistanceTo(GeoCoordinate other)
    {
        const double EarthRadiusKm = 6371.0;
        
        var lat1Rad = (double)(Latitude * (decimal)Math.PI / 180);
        var lon1Rad = (double)(Longitude * (decimal)Math.PI / 180);
        var lat2Rad = (double)(other.Latitude * (decimal)Math.PI / 180);
        var lon2Rad = (double)(other.Longitude * (decimal)Math.PI / 180);

        var dLat = lat2Rad - lat1Rad;
        var dLon = lon2Rad - lon1Rad;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }
}