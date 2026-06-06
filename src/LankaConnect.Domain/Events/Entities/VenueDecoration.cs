using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a non-seating decorative or structural element inside a venue layout
/// — stage, dance floor, aisle, door, wall, text label, or image. Decorations are
/// rendered for spatial context only and are NEVER selectable as seats.
///
/// Geometry and per-kind properties are stored as raw JSON strings to keep the
/// domain free of mutable nested value objects (avoids Phase 6A.130 deserialization
/// issues with EF Core <c>OwnsOne().ToJson()</c>).
/// </summary>
public class VenueDecoration : LegacyBaseEntity
{
    public const int MaxLabelLength = 100;

    public Guid VenueLayoutId { get; private set; }

    public DecorationKind Kind { get; private set; }

    /// <summary>
    /// Optional human-readable label (e.g. "Main Stage", "West Aisle"). Required
    /// only for <see cref="DecorationKind.Text"/> decorations.
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// JSON document describing placement on the canvas, as a string.
    /// Common payload: <c>{ "x", "y", "width", "height", "rotation" }</c>.
    /// Defaults to <c>"{}"</c> — canvas editor fills in at save time.
    /// </summary>
    public string Geometry { get; private set; } = "{}";

    /// <summary>
    /// JSON document for kind-specific properties as a string:
    /// <list type="bullet">
    ///   <item>Text: <c>{ "fontSize", "color", "fontWeight" }</c></item>
    ///   <item>Image: <c>{ "imageUrl", "opacity" }</c></item>
    ///   <item>Other kinds typically empty (<c>"{}"</c>)</item>
    /// </list>
    /// </summary>
    public string Properties { get; private set; } = "{}";

    public int SortOrder { get; private set; }

    // EF Core parameterless constructor
    private VenueDecoration() { }

    private VenueDecoration(
        Guid venueLayoutId,
        DecorationKind kind,
        string? label,
        string geometry,
        string properties,
        int sortOrder)
    {
        VenueLayoutId = venueLayoutId;
        Kind = kind;
        Label = label;
        Geometry = geometry;
        Properties = properties;
        SortOrder = sortOrder;
    }

    public static Result<VenueDecoration> Create(
        Guid venueLayoutId,
        DecorationKind kind,
        string? label,
        int sortOrder,
        string? geometry = null,
        string? properties = null)
    {
        if (venueLayoutId == Guid.Empty)
            return Result<VenueDecoration>.Failure("Venue layout ID is required");

        if (sortOrder < 0)
            return Result<VenueDecoration>.Failure("Sort order cannot be negative");

        if (kind == DecorationKind.Text && string.IsNullOrWhiteSpace(label))
            return Result<VenueDecoration>.Failure("Text decoration requires a non-empty label");

        if (!string.IsNullOrWhiteSpace(label) && label.Trim().Length > MaxLabelLength)
            return Result<VenueDecoration>.Failure(
                $"Decoration label cannot exceed {MaxLabelLength} characters");

        var normalizedLabel = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        return Result<VenueDecoration>.Success(new VenueDecoration(
            venueLayoutId: venueLayoutId,
            kind: kind,
            label: normalizedLabel,
            geometry: NormalizeJson(geometry),
            properties: NormalizeJson(properties),
            sortOrder: sortOrder));
    }

    public Result Update(
        DecorationKind kind,
        string? label,
        int sortOrder,
        string? geometry,
        string? properties)
    {
        if (sortOrder < 0)
            return Result.Failure("Sort order cannot be negative");

        if (kind == DecorationKind.Text && string.IsNullOrWhiteSpace(label))
            return Result.Failure("Text decoration requires a non-empty label");

        if (!string.IsNullOrWhiteSpace(label) && label.Trim().Length > MaxLabelLength)
            return Result.Failure($"Decoration label cannot exceed {MaxLabelLength} characters");

        Kind = kind;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        SortOrder = sortOrder;
        Geometry = NormalizeJson(geometry);
        Properties = NormalizeJson(properties);
        return Result.Success();
    }

    private static string NormalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "{}";
        var trimmed = value.Trim();
        if (trimmed[0] != '{' && trimmed[0] != '[') return "{}";
        return trimmed;
    }
}
