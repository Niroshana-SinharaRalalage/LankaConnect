using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Application.Events.Queries.GetLayoutPresets;

/// <summary>
/// Slice 6 Chunk S6.2: metadata returned for a single preset in the preset-library modal.
/// Thumbnail is a static PNG path served from the web app's /public folder — the modal
/// does NOT render react-konva thumbnails (architect soft-issue B1).
/// </summary>
public record LayoutPresetDto(
    string Id,
    string Name,
    string Description,
    LayoutType LayoutType,
    int TotalCapacity,
    string ThumbnailUrl);
