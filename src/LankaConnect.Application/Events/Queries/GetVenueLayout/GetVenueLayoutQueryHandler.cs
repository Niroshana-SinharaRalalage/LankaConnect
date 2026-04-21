using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetVenueLayout;

public class GetVenueLayoutQueryHandler : IQueryHandler<GetVenueLayoutQuery, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly ILogger<GetVenueLayoutQueryHandler> _logger;

    public GetVenueLayoutQueryHandler(
        IVenueLayoutRepository venueLayoutRepository,
        ILogger<GetVenueLayoutQueryHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(GetVenueLayoutQuery request, CancellationToken cancellationToken)
    {
        VenueLayout? layout;

        if (request.LayoutId.HasValue)
        {
            layout = await _venueLayoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId.Value, cancellationToken);
        }
        else if (request.EventId.HasValue)
        {
            layout = await _venueLayoutRepository.GetByEventIdAsync(request.EventId.Value, cancellationToken);
        }
        else
        {
            return Result<VenueLayoutDto>.Failure("Either LayoutId or EventId must be provided");
        }

        if (layout == null)
            return Result<VenueLayoutDto>.Failure("Venue layout not found");

        return Result<VenueLayoutDto>.Success(MapToDto(layout));
    }

    private static VenueLayoutDto MapToDto(VenueLayout layout)
    {
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
            Zones = layout.Zones.Select(z => new VenueZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                Color = z.Color,
                TicketTierId = null, // Slice 4: zone→tier mapping moved to tier_assignments; Slice 5 wires the read path with event tier join
                SortOrder = z.SortOrder,
                EnabledSeatCount = z.EnabledSeatCount,
                TotalSeatCount = z.Seats.Count,
                Seats = z.Seats.Select(s => new SeatDto
                {
                    Id = s.Id,
                    Row = s.Row,
                    Number = s.Number,
                    Label = s.Label,
                    SortOrder = s.SortOrder,
                    IsEnabled = s.IsEnabled,
                    IsAccessible = s.IsAccessible,
                    X = s.X,
                    Y = s.Y
                }).ToList()
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
                Seats = t.Seats.Select(s => new SeatDto
                {
                    Id = s.Id,
                    Row = s.Row,
                    Number = s.Number,
                    Label = s.Label,
                    SortOrder = s.SortOrder,
                    IsEnabled = s.IsEnabled,
                    IsAccessible = s.IsAccessible,
                    X = s.X,
                    Y = s.Y
                }).ToList()
            }).ToList(),
            Decorations = layout.Decorations.Select(d => new VenueDecorationDto
            {
                Id = d.Id,
                Kind = d.Kind.ToString(),
                Label = d.Label,
                Geometry = d.Geometry,
                Properties = d.Properties,
                SortOrder = d.SortOrder
            }).ToList()
        };
    }
}
