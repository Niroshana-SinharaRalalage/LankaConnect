namespace LankaConnect.Application.Badges.DTOs;

/// <summary>
/// DTO for BadgeLocationConfig value object
/// Phase 6A.31a: Per-location badge positioning and sizing
/// </summary>
public record BadgeLocationConfigDto
{
    /// <summary>
    /// Horizontal position as percentage (0.0 = left edge, 1.0 = right edge)
    /// </summary>
    public decimal PositionX { get; init; }

    /// <summary>
    /// Vertical position as percentage (0.0 = top edge, 1.0 = bottom edge)
    /// </summary>
    public decimal PositionY { get; init; }

    /// <summary>
    /// Badge width as percentage of container width (0.05-1.0 = 5%-100%)
    /// </summary>
    public decimal SizeWidth { get; init; }

    /// <summary>
    /// Badge height as percentage of container height (0.05-1.0 = 5%-100%)
    /// </summary>
    public decimal SizeHeight { get; init; }

    /// <summary>
    /// Badge rotation in degrees (0-360)
    /// </summary>
    public decimal Rotation { get; init; }
}
