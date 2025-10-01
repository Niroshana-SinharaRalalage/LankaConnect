using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class BusinessLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; }

    // For EF Core
    private BusinessLocation() 
    { 
        Address = null!;
        Coordinates = null;
    }

    private BusinessLocation(Address address, GeoCoordinate? coordinates)
    {
        Address = address;
        Coordinates = coordinates;
    }

    public static Result<BusinessLocation> Create(Address address, GeoCoordinate? coordinates = null)
    {
        if (address == null)
            return Result<BusinessLocation>.Failure("Address is required");

        return Result<BusinessLocation>.Success(new BusinessLocation(address, coordinates));
    }

    public static Result<BusinessLocation> Create(string street, string city, string state, string zipCode, string country, decimal? latitude = null, decimal? longitude = null)
    {
        var addressResult = Address.Create(street, city, state, zipCode, country);
        if (!addressResult.IsSuccess)
            return Result<BusinessLocation>.Failure(addressResult.Error);

        GeoCoordinate? coordinates = null;
        if (latitude.HasValue && longitude.HasValue)
        {
            var coordinatesResult = GeoCoordinate.Create(latitude.Value, longitude.Value);
            if (!coordinatesResult.IsSuccess)
                return Result<BusinessLocation>.Failure(coordinatesResult.Error);
            coordinates = coordinatesResult.Value;
        }

        return Result<BusinessLocation>.Success(new BusinessLocation(addressResult.Value, coordinates));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
        if (Coordinates != null)
            yield return Coordinates;
    }

    public override string ToString()
    {
        return Coordinates != null 
            ? $"{Address} (GPS: {Coordinates})" 
            : Address.ToString();
    }

    public double? DistanceTo(BusinessLocation other)
    {
        if (Coordinates == null || other.Coordinates == null)
            return null;

        return Coordinates.DistanceTo(other.Coordinates);
    }
}