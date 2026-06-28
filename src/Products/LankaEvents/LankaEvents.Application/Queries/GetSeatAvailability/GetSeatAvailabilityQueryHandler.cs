using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetSeatAvailability;

public class GetSeatAvailabilityQueryHandler : IQueryHandler<GetSeatAvailabilityQuery, List<SeatAvailabilityDto>>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatReservationRepository _seatReservationRepository;
    private readonly ILogger<GetSeatAvailabilityQueryHandler> _logger;

    public GetSeatAvailabilityQueryHandler(
        IVenueLayoutRepository venueLayoutRepository,
        ISeatHoldRepository seatHoldRepository,
        ISeatReservationRepository seatReservationRepository,
        ILogger<GetSeatAvailabilityQueryHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _seatHoldRepository = seatHoldRepository;
        _seatReservationRepository = seatReservationRepository;
        _logger = logger;
    }

    public async Task<Result<List<SeatAvailabilityDto>>> Handle(GetSeatAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (request.EventId == Guid.Empty)
            return Result<List<SeatAvailabilityDto>>.Failure("Event ID is required");

        var layout = await _venueLayoutRepository.GetAssignedLayoutForEventAsync(request.EventId, cancellationToken);
        if (layout == null)
            return Result<List<SeatAvailabilityDto>>.Failure("No venue layout found for this event");

        // Get all seat IDs from the layout
        var allSeats = layout.Zones
            .SelectMany(z => z.Seats.Select(s => new { Seat = s, Zone = z }))
            .ToList();

        var allSeatIds = allSeats.Select(x => x.Seat.Id).ToList();

        // Fetch runtime state in parallel
        var heldSeatIdsTask = _seatHoldRepository.GetHeldSeatIdsAsync(allSeatIds, cancellationToken);
        var reservedSeatIdsTask = _seatReservationRepository.GetReservedSeatIdsAsync(allSeatIds, cancellationToken);

        await Task.WhenAll(heldSeatIdsTask, reservedSeatIdsTask);

        var heldSeatIds = (await heldSeatIdsTask).ToHashSet();
        var reservedSeatIds = (await reservedSeatIdsTask).ToHashSet();

        // Combine structural + runtime status
        var result = allSeats.Select(x =>
        {
            string status;
            if (!x.Seat.IsEnabled)
                status = "Disabled";
            else if (reservedSeatIds.Contains(x.Seat.Id))
                status = "Reserved";
            else if (heldSeatIds.Contains(x.Seat.Id))
                status = "Held";
            else
                status = "Available";

            return new SeatAvailabilityDto
            {
                Id = x.Seat.Id,
                Label = x.Seat.Label,
                Row = x.Seat.Row,
                Number = x.Seat.Number,
                IsEnabled = x.Seat.IsEnabled,
                IsAccessible = x.Seat.IsAccessible,
                X = x.Seat.X,
                Y = x.Seat.Y,
                Status = status,
                ZoneId = x.Zone.Id,
                ZoneName = x.Zone.Name,
                ZoneColor = x.Zone.Color,
                TicketTierId = null // Slice 4: zone→tier via tier_assignments; Slice 5 wires the read path
            };
        }).ToList();

        _logger.LogDebug(
            "Seat availability: EventId={EventId}, Total={Total}, Available={Available}, Held={Held}, Reserved={Reserved}",
            request.EventId,
            result.Count,
            result.Count(s => s.Status == "Available"),
            result.Count(s => s.Status == "Held"),
            result.Count(s => s.Status == "Reserved"));

        return Result<List<SeatAvailabilityDto>>.Success(result);
    }
}
