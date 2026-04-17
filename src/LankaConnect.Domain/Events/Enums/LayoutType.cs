namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Represents the physical layout type for a venue.
/// Determines how seats are generated and rendered on the seat map.
/// </summary>
public enum LayoutType
{
    /// <summary>
    /// Rows of seats facing a stage/screen.
    /// Labels: "A1", "A2", "B1", etc. (row letter + seat number).
    /// Zones map to row ranges (e.g., VIP = rows A–C).
    /// </summary>
    Theater = 0,

    /// <summary>
    /// Round or rectangular tables with seats around them.
    /// Labels: "T1-S1", "T1-S2", etc. (table number + seat number).
    /// Zones map to table groups.
    /// </summary>
    Banquet = 1,

    /// <summary>
    /// Manual seat placement with arbitrary coordinates.
    /// Reserved for future implementation.
    /// </summary>
    Custom = 2
}
