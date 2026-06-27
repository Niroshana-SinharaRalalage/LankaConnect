using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.GenerateSeats;

public class GenerateSeatsCommandHandler : ICommandHandler<GenerateSeatsCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateSeatsCommandHandler> _logger;

    public GenerateSeatsCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<GenerateSeatsCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(GenerateSeatsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating seats: LayoutId={LayoutId}, ZoneId={ZoneId}, Type={Type}, Units={Units}, SeatsPerUnit={SeatsPerUnit}",
            request.LayoutId, request.ZoneId, request.GenerationType, request.RowsOrTables, request.SeatsPerUnit);

        var layout = await _venueLayoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        if (layout == null)
            return Result<VenueLayoutDto>.Failure("Venue layout not found");

        Result result;

        switch (request.GenerationType.ToLowerInvariant())
        {
            case "theater":
                var startRow = request.StartLabel ?? "A";
                result = layout.GenerateTheaterSeats(request.ZoneId, request.RowsOrTables, request.SeatsPerUnit, startRow);
                break;

            case "banquet":
                var startTable = 1;
                if (!string.IsNullOrWhiteSpace(request.StartLabel) && int.TryParse(request.StartLabel, out var parsed))
                    startTable = parsed;
                result = layout.GenerateBanquetSeats(request.ZoneId, request.RowsOrTables, request.SeatsPerUnit, startTable);
                break;

            default:
                return Result<VenueLayoutDto>.Failure($"Invalid generation type: '{request.GenerationType}'. Use 'Theater' or 'Banquet'");
        }

        if (result.IsFailure)
            return Result<VenueLayoutDto>.Failure(result.Error);

        _venueLayoutRepository.Update(layout);
        await _unitOfWork.CommitAsync(cancellationToken);

        var zone = layout.GetZone(request.ZoneId);
        _logger.LogInformation(
            "Seats generated: LayoutId={LayoutId}, ZoneId={ZoneId}, SeatCount={SeatCount}",
            layout.Id, request.ZoneId, zone?.Seats.Count ?? 0);

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
                TicketTierId = null, // Slice 4: tier mapping lives in tier_assignments; Slice 5 adds the read path
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
