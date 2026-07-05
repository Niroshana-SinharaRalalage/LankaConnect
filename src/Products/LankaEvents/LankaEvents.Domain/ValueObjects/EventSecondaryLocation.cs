using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Value object representing an event's optional secondary location
/// (e.g., parking lot or a secondary venue). Composes an EventLocation with
/// a SecondaryLocationType to keep the type + location invariant atomic.
/// Phase 7C.1: Location Name + Secondary Location feature.
/// </summary>
public class EventSecondaryLocation : ValueObject
{
    public SecondaryLocationType Type { get; }
    public EventLocation Location { get; }

    // EF Core constructor
    private EventSecondaryLocation()
    {
        Location = null!;
    }

    private EventSecondaryLocation(SecondaryLocationType type, EventLocation location)
    {
        Type = type;
        Location = location;
    }

    /// <summary>
    /// Creates a secondary location with a required type and a required inner EventLocation.
    /// </summary>
    public static Result<EventSecondaryLocation> Create(SecondaryLocationType type, EventLocation location)
    {
        if (!Enum.IsDefined(typeof(SecondaryLocationType), type))
            return Result<EventSecondaryLocation>.Failure($"Invalid secondary location type: {type}");

        if (location == null)
            return Result<EventSecondaryLocation>.Failure("Location is required for a secondary location");

        return Result<EventSecondaryLocation>.Success(new EventSecondaryLocation(type, location));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return Location;
    }

    public override string ToString() => $"{Type}: {Location}";
}
