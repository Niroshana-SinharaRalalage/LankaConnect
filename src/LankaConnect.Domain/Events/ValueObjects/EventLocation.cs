using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing an event's physical location
/// Composes Address and GeoCoordinate following DRY principle (Epic 2 Phase 1)
/// Coordinates are optional until geocoded
/// </summary>
public class EventLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; }

    // EF Core constructor
    private EventLocation()
    {
        Address = null!;
    }

    private EventLocation(Address address, GeoCoordinate? coordinates = null)
    {
        Address = address;
        Coordinates = coordinates;
    }

    /// <summary>
    /// Creates an EventLocation with required address and optional coordinates
    /// </summary>
    public static Result<EventLocation> Create(Address address, GeoCoordinate? coordinates = null)
    {
        if (address == null)
            return Result<EventLocation>.Failure("Address is required");

        return Result<EventLocation>.Success(new EventLocation(address, coordinates));
    }

    /// <summary>
    /// Returns a new EventLocation with updated coordinates (immutable pattern)
    /// </summary>
    public Result<EventLocation> WithCoordinates(GeoCoordinate coordinates)
    {
        if (coordinates == null)
            return Result<EventLocation>.Failure("Coordinates cannot be null");

        return Result<EventLocation>.Success(new EventLocation(Address, coordinates));
    }

    /// <summary>
    /// Checks if this location has geographic coordinates set
    /// </summary>
    public bool HasCoordinates() => Coordinates != null;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;

        if (Coordinates != null)
            yield return Coordinates;
    }

    public override string ToString()
    {
        var addressString = Address.ToString();

        if (Coordinates != null)
            return $"{addressString} ({Coordinates})";

        return $"{addressString} (coordinates not set)";
    }
}
