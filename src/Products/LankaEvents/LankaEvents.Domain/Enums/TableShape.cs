namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Geometric shape of a banquet/dining table on the canvas editor.
/// Determines how seats are positioned around the table and how capacity is validated.
/// </summary>
public enum TableShape
{
    /// <summary>
    /// Round table with seats distributed radially around the circumference.
    /// Geometry: { centerX, centerY, radius, rotation? }.
    /// Each seat has an AngleDeg position (0°=3 o'clock, sweeping clockwise).
    /// </summary>
    Round = 0,

    /// <summary>
    /// Square table with seats distributed evenly along each side.
    /// Geometry: { centerX, centerY, side, rotation? }.
    /// Capacity must be a multiple of 4 (one seat per side minimum).
    /// </summary>
    Square = 1,

    /// <summary>
    /// Rectangular table (often used for head tables or classroom-style rows).
    /// Geometry: { centerX, centerY, width, height, rotation? }.
    /// Seats distributed along the long sides; short sides optionally occupied.
    /// </summary>
    Rect = 2
}
