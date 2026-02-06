using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;

namespace LankaConnect.Domain.Badges;

/// <summary>
/// Value object representing badge position, size, and rotation configuration for a specific display location.
/// Phase 6A.31a: Position and size stored as percentage ratios (0.0-1.0) for responsive scaling.
/// </summary>
public class BadgeLocationConfig : ValueObject
{
    /// <summary>
    /// Horizontal position as percentage (0.0 = left edge, 1.0 = right edge).
    /// Badge is positioned with its right edge at this percentage point.
    /// </summary>
    public decimal PositionX { get; private set; }

    /// <summary>
    /// Vertical position as percentage (0.0 = top edge, 1.0 = bottom edge).
    /// Badge is positioned with its top edge at this percentage point.
    /// </summary>
    public decimal PositionY { get; private set; }

    /// <summary>
    /// Badge width as percentage of container width (0.05-1.0 = 5%-100%).
    /// </summary>
    public decimal SizeWidth { get; private set; }

    /// <summary>
    /// Badge height as percentage of container height (0.05-1.0 = 5%-100%).
    /// </summary>
    public decimal SizeHeight { get; private set; }

    /// <summary>
    /// Badge rotation in degrees (0-360).
    /// </summary>
    public decimal Rotation { get; private set; }

    /// <summary>
    /// Default configuration for Events Listing page (192px containers).
    /// TopRight position with 26% size ratio.
    /// </summary>
    public static BadgeLocationConfig DefaultListing =>
        new(positionX: 1.0m, positionY: 0.0m, sizeWidth: 0.26m, sizeHeight: 0.26m, rotation: 0m);

    /// <summary>
    /// Default configuration for Featured Banner (160px containers).
    /// TopRight position with 26% size ratio.
    /// </summary>
    public static BadgeLocationConfig DefaultFeatured =>
        new(positionX: 1.0m, positionY: 0.0m, sizeWidth: 0.26m, sizeHeight: 0.26m, rotation: 0m);

    /// <summary>
    /// Default configuration for Event Detail Hero (384px containers).
    /// TopRight position with 21% size ratio (smaller for large images).
    /// </summary>
    public static BadgeLocationConfig DefaultDetail =>
        new(positionX: 1.0m, positionY: 0.0m, sizeWidth: 0.21m, sizeHeight: 0.21m, rotation: 0m);

    /// <summary>
    /// Private parameterless constructor required by EF Core for OwnsOne value object hydration.
    /// EF Core will instantiate this and then set properties from database columns.
    /// Validation occurs only during domain object creation via public constructor.
    /// Phase 6A.31d: Added to fix HTTP 500 error caused by missing parameterless constructor.
    /// </summary>
    private BadgeLocationConfig()
    {
        // Set temporary defaults - EF Core will overwrite these with database values
        // These defaults are never used in production, only during EF Core materialization
        PositionX = 0m;
        PositionY = 0m;
        SizeWidth = 0.26m;
        SizeHeight = 0.26m;
        Rotation = 0m;
    }

    public BadgeLocationConfig(
        decimal positionX,
        decimal positionY,
        decimal sizeWidth,
        decimal sizeHeight,
        decimal rotation)
    {
        ValidatePercentage(positionX, nameof(positionX), min: 0m, max: 1m);
        ValidatePercentage(positionY, nameof(positionY), min: 0m, max: 1m);
        ValidatePercentage(sizeWidth, nameof(sizeWidth), min: 0.05m, max: 1m);
        ValidatePercentage(sizeHeight, nameof(sizeHeight), min: 0.05m, max: 1m);
        ValidateRotation(rotation);

        PositionX = positionX;
        PositionY = positionY;
        SizeWidth = sizeWidth;
        SizeHeight = sizeHeight;
        Rotation = rotation;
    }

    private static void ValidatePercentage(decimal value, string paramName, decimal min, decimal max)
    {
        if (value < min || value > max)
        {
            string fieldName = paramName switch
            {
                nameof(PositionX) => "Position X",
                nameof(PositionY) => "Position Y",
                nameof(SizeWidth) => "Size width",
                nameof(SizeHeight) => "Size height",
                _ => paramName
            };

            throw new ValidationException(fieldName, $"{fieldName} must be between {min} and {max}");
        }
    }

    private static void ValidateRotation(decimal degrees)
    {
        if (degrees < 0 || degrees > 360)
        {
            throw new ValidationException(nameof(Rotation), "Rotation must be between 0 and 360");
        }
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PositionX;
        yield return PositionY;
        yield return SizeWidth;
        yield return SizeHeight;
        yield return Rotation;
    }
}
