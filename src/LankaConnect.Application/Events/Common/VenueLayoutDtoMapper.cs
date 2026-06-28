using LankaConnect.Products.LankaEvents.Domain.Entities;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Slice 6 Chunk S6.3: single-source-of-truth projection from the
/// <see cref="VenueLayout"/> aggregate onto <see cref="VenueLayoutDto"/>.
/// Includes zones + tables + decorations + seats + canvas + tier assignments.
/// The preset factory (Slice 6) and canvas editor (Slice 8) return fresh layouts
/// with no tier assignments yet — callers with an assignment dictionary pass it;
/// callers without pass null and get an empty <c>TicketTierIds</c> list per zone/table.
/// </summary>
public static class VenueLayoutDtoMapper
{
    public static VenueLayoutDto Map(
        VenueLayout layout,
        IReadOnlyDictionary<Guid, List<Guid>>? tiersByAssignable = null)
    {
        List<Guid> TierIdsFor(Guid assignableId) =>
            tiersByAssignable is not null && tiersByAssignable.TryGetValue(assignableId, out var tiers)
                ? tiers
                : new List<Guid>();

        return new VenueLayoutDto
        {
            Id = layout.Id,
            Name = layout.Name,
            EventId = layout.EventId,
            LayoutType = layout.LayoutType.ToString(),
            IsTemplate = layout.IsTemplate,
            CreatedByUserId = layout.CreatedByUserId,
            TotalCapacity = layout.TotalCapacity,
            CreatedAt = layout.CreatedAt,
            UpdatedAt = layout.UpdatedAt,
            RowVersion = layout.RowVersion,
            Canvas = new CanvasConfigDto
            {
                Width = layout.Canvas.Width,
                Height = layout.Canvas.Height,
                Scale = layout.Canvas.Scale,
                BackgroundColor = layout.Canvas.BackgroundColor,
            },
            Zones = layout.Zones.Select(z => new VenueZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                Color = z.Color,
                TicketTierId = null,
                SortOrder = z.SortOrder,
                EnabledSeatCount = z.EnabledSeatCount,
                TotalSeatCount = z.Seats.Count,
                Shape = z.Shape.ToString(),
                Geometry = z.Geometry,
                TicketTierIds = TierIdsFor(z.Id),
                Seats = z.Seats.Select(MapSeat).ToList(),
            }).ToList(),
            Tables = layout.Tables.Select(t => new VenueTableDto
            {
                Id = t.Id,
                VenueZoneId = t.VenueZoneId,
                Label = t.Label,
                Shape = t.Shape.ToString(),
                Geometry = t.Geometry,
                Capacity = t.Capacity,
                SortOrder = t.SortOrder,
                EnabledSeatCount = t.EnabledSeatCount,
                TotalSeatCount = t.Seats.Count,
                TicketTierIds = TierIdsFor(t.Id),
                Seats = t.Seats.Select(MapSeat).ToList(),
            }).ToList(),
            Decorations = layout.Decorations.Select(d => new VenueDecorationDto
            {
                Id = d.Id,
                Kind = d.Kind.ToString(),
                Label = d.Label,
                Geometry = d.Geometry,
                Properties = d.Properties,
                SortOrder = d.SortOrder,
            }).ToList(),
        };
    }

    private static SeatDto MapSeat(Seat s) => new()
    {
        Id = s.Id,
        Row = s.Row,
        Number = s.Number,
        Label = s.Label,
        SortOrder = s.SortOrder,
        IsEnabled = s.IsEnabled,
        IsAccessible = s.IsAccessible,
        X = s.X,
        Y = s.Y,
    };
}
