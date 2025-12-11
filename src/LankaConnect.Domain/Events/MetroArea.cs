using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Metro Area entity - represents geographic service areas
/// Read-only entity for querying metro areas from events.metro_areas table
/// </summary>
public class MetroArea : BaseEntity
{
    public string Name { get; private set; }
    public string State { get; private set; }
    public double CenterLatitude { get; private set; }
    public double CenterLongitude { get; private set; }
    public int RadiusMiles { get; private set; }
    public bool IsStateLevelArea { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core constructor
    private MetroArea()
    {
        Name = null!;
        State = null!;
    }

    // Private constructor for controlled instantiation
    private MetroArea(
        Guid id,
        string name,
        string state,
        double centerLatitude,
        double centerLongitude,
        int radiusMiles,
        bool isStateLevelArea,
        bool isActive)
    {
        Id = id;
        Name = name;
        State = state;
        CenterLatitude = centerLatitude;
        CenterLongitude = centerLongitude;
        RadiusMiles = radiusMiles;
        IsStateLevelArea = isStateLevelArea;
        IsActive = isActive;
    }

    /// <summary>
    /// Factory method - primarily for testing
    /// In production, metro areas are loaded from database
    /// </summary>
    public static MetroArea Create(
        Guid id,
        string name,
        string state,
        double centerLatitude,
        double centerLongitude,
        int radiusMiles = 30,
        bool isStateLevelArea = false,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Metro area name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required", nameof(state));

        if (centerLatitude < -90 || centerLatitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(centerLatitude));

        if (centerLongitude < -180 || centerLongitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(centerLongitude));

        if (radiusMiles <= 0)
            throw new ArgumentException("Radius must be positive", nameof(radiusMiles));

        return new MetroArea(id, name, state, centerLatitude, centerLongitude, radiusMiles, isStateLevelArea, isActive);
    }
}
