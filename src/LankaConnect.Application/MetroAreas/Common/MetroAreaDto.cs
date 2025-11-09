namespace LankaConnect.Application.MetroAreas.Common;

/// <summary>
/// Data Transfer Object for MetroArea
/// Used for API responses and client consumption
/// </summary>
public record MetroAreaDto
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Metro area name (e.g., "Cleveland", "Columbus")
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// State code (e.g., "OH", "NY", "PA")
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Center latitude coordinate
    /// </summary>
    public double CenterLatitude { get; init; }

    /// <summary>
    /// Center longitude coordinate
    /// </summary>
    public double CenterLongitude { get; init; }

    /// <summary>
    /// Radius in miles from center point
    /// </summary>
    public int RadiusMiles { get; init; }

    /// <summary>
    /// True if this represents an entire state (e.g., "All Ohio")
    /// </summary>
    public bool IsStateLevelArea { get; init; }

    /// <summary>
    /// Whether this metro area is actively available for selection
    /// </summary>
    public bool IsActive { get; init; }
}
