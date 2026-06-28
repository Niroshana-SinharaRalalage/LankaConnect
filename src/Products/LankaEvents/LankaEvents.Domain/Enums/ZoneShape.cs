namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Geometric shape of a venue zone on the canvas editor.
/// Determines how Geometry JSON is interpreted and how the zone is rendered.
/// </summary>
public enum ZoneShape
{
    /// <summary>
    /// Axis-aligned rectangle. Geometry: { x, y, width, height, rotation? }.
    /// Default shape for simple row-based seating zones.
    /// </summary>
    Rect = 0,

    /// <summary>
    /// Curved (arc) shape used for theater front rows that follow the stage edge.
    /// Geometry: { centerX, centerY, radius, startAngleDeg, sweepAngleDeg, rowCount }.
    /// </summary>
    Curve = 1,

    /// <summary>
    /// Arbitrary closed polygon for irregular zone outlines.
    /// Geometry: { points: [{ x, y }, ...] } with at least 3 points.
    /// </summary>
    Polygon = 2
}
