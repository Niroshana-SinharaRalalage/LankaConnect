using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.CreateVenueLayout;

public class CreateVenueLayoutCommandHandler : ICommandHandler<CreateVenueLayoutCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateVenueLayoutCommandHandler> _logger;

    public CreateVenueLayoutCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateVenueLayoutCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _unitOfWork = unitOfWork;
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
            var zoneResult = layout.AddZone(zoneReq.Name, zoneReq.Color, zoneReq.TicketTierId, zoneReq.SortOrder);
            if (zoneResult.IsFailure)
                return Result<VenueLayoutDto>.Failure(zoneResult.Error);
        }

        await _venueLayoutRepository.AddAsync(layout, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Venue layout created: LayoutId={LayoutId}, Zones={ZoneCount}",
            layout.Id, layout.Zones.Count);

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
            Zones = layout.Zones.Select(z => new VenueZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                Color = z.Color,
                TicketTierId = z.TicketTierId,
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
            }).ToList()
        };
    }
}
