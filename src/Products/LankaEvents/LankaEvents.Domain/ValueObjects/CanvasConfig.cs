using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Canvas rendering configuration for a venue layout — describes the virtual
/// coordinate space in which zones, tables, decorations, and seats are placed.
/// Persisted as flat columns on <c>venue_layouts</c> via EF Core OwnsOne; intentionally
/// NOT serialized as a nested JSONB to avoid Phase 6A.130 deserialization issues.
/// </summary>
public class CanvasConfig : ValueObject
{
    public const int MinDimension = 100;
    public const int MaxDimension = 10_000;
    public const double MinScale = 0.1;
    public const double MaxScale = 10.0;

    /// <summary>Canvas width in virtual pixels. [100, 10_000].</summary>
    public int Width { get; }

    /// <summary>Canvas height in virtual pixels. [100, 10_000].</summary>
    public int Height { get; }

    /// <summary>Default render scale (zoom) applied when the layout is opened. [0.1, 10.0].</summary>
    public double Scale { get; }

    /// <summary>
    /// Background color as a hex string (e.g. <c>#ffffff</c>, <c>#RRGGBBAA</c>).
    /// Stored verbatim; rendering layer decides how to apply.
    /// </summary>
    public string BackgroundColor { get; }

    private CanvasConfig(int width, int height, double scale, string backgroundColor)
    {
        Width = width;
        Height = height;
        Scale = scale;
        BackgroundColor = backgroundColor;
    }

    /// <summary>
    /// Default canvas used by seat generators and preset layouts.
    /// 1200×800 matches the canvas editor viewport at scale 1.
    /// </summary>
    public static CanvasConfig Default() =>
        new(width: 1200, height: 800, scale: 1.0, backgroundColor: "#ffffff");

    public static Result<CanvasConfig> Create(int width, int height, double scale, string? backgroundColor)
    {
        if (width < MinDimension || width > MaxDimension)
            return Result<CanvasConfig>.Failure(
                $"Canvas width must be between {MinDimension} and {MaxDimension} pixels");

        if (height < MinDimension || height > MaxDimension)
            return Result<CanvasConfig>.Failure(
                $"Canvas height must be between {MinDimension} and {MaxDimension} pixels");

        if (scale < MinScale || scale > MaxScale)
            return Result<CanvasConfig>.Failure(
                $"Canvas scale must be between {MinScale} and {MaxScale}");

        if (string.IsNullOrWhiteSpace(backgroundColor))
            return Result<CanvasConfig>.Failure("Background color is required");

        var color = backgroundColor.Trim();
        if (!IsHexColor(color))
            return Result<CanvasConfig>.Failure(
                "Background color must be a hex string (e.g. #ffffff or #RRGGBBAA)");

        return Result<CanvasConfig>.Success(new CanvasConfig(width, height, scale, color));
    }

    private static bool IsHexColor(string value)
    {
        if (value.Length is not (4 or 5 or 7 or 9)) return false;
        if (value[0] != '#') return false;
        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            var isHex = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
            if (!isHex) return false;
        }
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
        yield return Scale;
        yield return BackgroundColor;
    }
}
