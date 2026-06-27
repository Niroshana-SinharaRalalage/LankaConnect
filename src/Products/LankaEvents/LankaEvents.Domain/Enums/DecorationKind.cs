namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Kind of non-seating decorative or structural element placed on the venue canvas.
/// Decorations are rendered for spatial context but are never selectable as seats.
/// </summary>
public enum DecorationKind
{
    /// <summary>
    /// Performance stage. Usually placed at the focal point of a theater layout.
    /// Geometry: { x, y, width, height, rotation? }.
    /// </summary>
    Stage = 0,

    /// <summary>
    /// Dance floor area in a banquet or mixed layout.
    /// Geometry: { x, y, width, height, rotation? }.
    /// </summary>
    DanceFloor = 1,

    /// <summary>
    /// Walkway/aisle separating seating zones. Non-selectable, used for wayfinding.
    /// Geometry: { x, y, width, height, rotation? }.
    /// </summary>
    Aisle = 2,

    /// <summary>
    /// Entry/exit door marker.
    /// Geometry: { x, y, width, height, rotation? }.
    /// </summary>
    Door = 3,

    /// <summary>
    /// Structural wall or divider.
    /// Geometry: { x, y, width, height, rotation? }.
    /// </summary>
    Wall = 4,

    /// <summary>
    /// Free-form text label (section name, zone marker, "Reserved" signage).
    /// Geometry: { x, y, width, height, rotation? }. Properties: { label, fontSize, color }.
    /// </summary>
    Text = 5,

    /// <summary>
    /// Image/logo placed on the canvas (e.g., sponsor banner, venue map overlay).
    /// Geometry: { x, y, width, height, rotation? }. Properties: { imageUrl, opacity }.
    /// </summary>
    Image = 6
}
