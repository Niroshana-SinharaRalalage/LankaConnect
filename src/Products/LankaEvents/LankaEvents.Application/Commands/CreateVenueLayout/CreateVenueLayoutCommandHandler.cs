using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateVenueLayout;

public class CreateVenueLayoutCommandHandler : ICommandHandler<CreateVenueLayoutCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<CreateVenueLayoutCommandHandler> _logger;

    public CreateVenueLayoutCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<CreateVenueLayoutCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(CreateVenueLayoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating venue layout: Name={Name}, Type={LayoutType}, EventId={EventId}, IsTemplate={IsTemplate}",
            request.Name, request.LayoutType, request.EventId, request.IsTemplate);

        if (!Enum.TryParse<LayoutType>(request.LayoutType, ignoreCase: true, out var layoutType))
            return Result<VenueLayoutDto>.Failure($"Invalid layout type: '{request.LayoutType}'. Valid values: Theater, Banquet, Custom");

        var layoutResult = VenueLayout.Create(
            request.Name,
            layoutType,
            request.CreatedByUserId,
            request.EventId,
            request.IsTemplate);

        if (layoutResult.IsFailure)
            return Result<VenueLayoutDto>.Failure(layoutResult.Error);

        var layout = layoutResult.Value;

        // Add zones
        foreach (var zoneReq in request.Zones)
        {
            var zoneResult = layout.AddZone(zoneReq.Name, zoneReq.Color, zoneReq.SortOrder);
            if (zoneResult.IsFailure)
                return Result<VenueLayoutDto>.Failure(zoneResult.Error);
        }

        // Slice 4 Release N: tier → zone mapping moved to the polymorphic tier_assignments
        // junction. CreateVenueZoneRequest.TicketTierId is accepted for API back-compat but
        // ignored here; Slice 5 exposes POST /api/venue-layouts/{id}/tier-assignments as the
        // canonical mapping path. Presets (Slice 6) and canvas editor (Slice 8) will call it.

        await _venueLayoutRepository.AddAsync(layout, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Venue layout created: LayoutId={LayoutId}, Zones={ZoneCount}",
            layout.Id, layout.Zones.Count);

        // Slice 5 Chunk 13: always fromPreset=false here; Slice 6's preset command will
        // emit its own LayoutCreated with fromPreset=true.
        _metrics.LayoutCreated(layout.LayoutType, fromPreset: false);

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
                TicketTierId = null, // Slice 4: sourced from tier_assignments; write path lands in Slice 5
                SortOrder = z.SortOrder,
                EnabledSeatCount = z.EnabledSeatCount,
                TotalSeatCount = z.Seats.Count,
                Shape = z.Shape.ToString(),
                Geometry = z.Geometry,
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
            }).ToList()
        };
    }
}
