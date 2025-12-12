namespace LankaConnect.Domain.Badges.Enums;

/// <summary>
/// Position where the badge overlay should be displayed on event images
/// </summary>
public enum BadgePosition
{
    /// <summary>
    /// Top-left corner of the event image
    /// </summary>
    TopLeft = 0,

    /// <summary>
    /// Top-right corner of the event image (most common for promotional badges)
    /// </summary>
    TopRight = 1,

    /// <summary>
    /// Bottom-left corner of the event image
    /// </summary>
    BottomLeft = 2,

    /// <summary>
    /// Bottom-right corner of the event image
    /// </summary>
    BottomRight = 3
}
