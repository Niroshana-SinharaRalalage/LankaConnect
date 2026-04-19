using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing an event's physical location.
/// Composes optional venue Name + required Address + optional GeoCoordinate (Epic 2 Phase 1; Phase 7C.1).
/// Coordinates are optional until geocoded. Name is optional (e.g., "Grand Ballroom").
/// </summary>
public class EventLocation : ValueObject
{
    private const int MaxNameLength = 150;

    public string? Name { get; }
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; }

    // EF Core constructor
    private EventLocation()
    {
        Address = null!;
    }

    private EventLocation(string? name, Address address, GeoCoordinate? coordinates)
    {
        Name = name;
        Address = address;
        Coordinates = coordinates;
    }

    /// <summary>
    /// Creates an EventLocation with required address, optional venue name, and optional coordinates.
    /// </summary>
    public static Result<EventLocation> Create(Address address, GeoCoordinate? coordinates = null, string? name = null)
    {
        if (address == null)
            return Result<EventLocation>.Failure("Address is required");

        string? normalizedName = null;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var trimmed = name.Trim();
            if (trimmed.Length > MaxNameLength)
                return Result<EventLocation>.Failure($"Location name cannot exceed {MaxNameLength} characters");
            normalizedName = trimmed;
        }

        return Result<EventLocation>.Success(new EventLocation(normalizedName, address, coordinates));
    }

    /// <summary>
    /// Returns a new EventLocation with updated coordinates (immutable pattern).
    /// Preserves Name and Address.
    /// </summary>
    public Result<EventLocation> WithCoordinates(GeoCoordinate coordinates)
    {
        if (coordinates == null)
            return Result<EventLocation>.Failure("Coordinates cannot be null");

        return Result<EventLocation>.Success(new EventLocation(Name, Address, coordinates));
    }

    /// <summary>
    /// Checks if this location has geographic coordinates set.
    /// </summary>
    public bool HasCoordinates() => Coordinates != null;

    /// <summary>
    /// Checks if this location has a venue name set.
    /// </summary>
    public bool HasName() => !string.IsNullOrWhiteSpace(Name);

    public override IEnumerable<object> GetEqualityComponents()
    {
        if (Name != null)
            yield return Name;

        yield return Address;

        if (Coordinates != null)
            yield return Coordinates;
    }

    public override string ToString()
    {
        var addressString = Address.ToString();
        var namePart = HasName() ? $"{Name}, " : string.Empty;

        if (Coordinates != null)
            return $"{namePart}{addressString} ({Coordinates})";

        return $"{namePart}{addressString} (coordinates not set)";
    }
}
