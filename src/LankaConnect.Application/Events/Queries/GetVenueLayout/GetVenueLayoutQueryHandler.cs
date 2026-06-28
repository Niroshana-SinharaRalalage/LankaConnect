using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetVenueLayout;

public class GetVenueLayoutQueryHandler : IQueryHandler<GetVenueLayoutQuery, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetVenueLayoutQueryHandler> _logger;

    public GetVenueLayoutQueryHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IEventRepository eventRepository,
        ILogger<GetVenueLayoutQueryHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _eventRepository = eventRepository;
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
            layout = await _venueLayoutRepository.GetAssignedLayoutForEventAsync(request.EventId.Value, cancellationToken);
        }
        else
        {
            return Result<VenueLayoutDto>.Failure("Either LayoutId or EventId must be provided");
        }

        if (layout == null)
            return Result<VenueLayoutDto>.Failure("Venue layout not found");

        // Slice 5 Chunk 8: populate zone/table → tier-assignment read path by loading the
        // owning event (if any) and building a polymorphic reverse-lookup from
        // assignable-id → tier-ids. Template layouts (EventId == null) have no tier
        // assignments — lookup stays empty.
        var tiersByAssignable = new Dictionary<Guid, List<Guid>>();
        if (layout.EventId.HasValue)
        {
            var @event = await _eventRepository.GetByIdAsync(layout.EventId.Value, trackChanges: false, cancellationToken);
            if (@event != null)
            {
                foreach (var tier in @event.TicketTiers)
                {
                    foreach (var assignment in tier.Assignments)
                    {
                        if (!tiersByAssignable.TryGetValue(assignment.AssignableId, out var list))
                        {
                            list = new List<Guid>();
                            tiersByAssignable[assignment.AssignableId] = list;
                        }
                        list.Add(tier.Id);
                    }
                }
            }
        }

        return Result<VenueLayoutDto>.Success(MapToDto(layout, tiersByAssignable));
    }

    private static VenueLayoutDto MapToDto(VenueLayout layout, Dictionary<Guid, List<Guid>> tiersByAssignable)
    {
        List<Guid> TierIdsFor(Guid assignableId) =>
            tiersByAssignable.TryGetValue(assignableId, out var tiers) ? tiers : new List<Guid>();

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
                TicketTierId = null, // Slice 4: legacy scalar always null — use TicketTierIds instead
                SortOrder = z.SortOrder,
                EnabledSeatCount = z.EnabledSeatCount,
                TotalSeatCount = z.Seats.Count,
                Shape = z.Shape.ToString(),
                Geometry = z.Geometry,
                TicketTierIds = TierIdsFor(z.Id),
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
                TicketTierIds = TierIdsFor(t.Id),
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
