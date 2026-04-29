using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.HoldSeats;

public class HoldSeatsCommandHandler : ICommandHandler<HoldSeatsCommand, HoldSeatsResult>
{
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatReservationRepository _seatReservationRepository;
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HoldSeatsCommandHandler> _logger;

    public HoldSeatsCommandHandler(
        ISeatHoldRepository seatHoldRepository,
        ISeatReservationRepository seatReservationRepository,
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<HoldSeatsCommandHandler> logger)
    {
        _seatHoldRepository = seatHoldRepository;
        _seatReservationRepository = seatReservationRepository;
        _venueLayoutRepository = venueLayoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<HoldSeatsResult>> Handle(HoldSeatsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Holding seats: EventId={EventId}, UserId={UserId}, SeatCount={SeatCount}, SessionId={SessionId}",
            request.EventId, request.UserId, request.SeatIds.Count, request.SessionId);

        if (request.SeatIds.Count == 0)
            return Result<HoldSeatsResult>.Failure("At least one seat must be selected");

        if (request.SeatIds.Count > 10)
            return Result<HoldSeatsResult>.Failure("Cannot hold more than 10 seats at once");

        // Verify the layout exists for this event and seats belong to it
        var layout = await _venueLayoutRepository.GetAssignedLayoutForEventAsync(request.EventId, cancellationToken);
        if (layout == null)
            return Result<HoldSeatsResult>.Failure("No venue layout found for this event");

        // Validate all seat IDs belong to this layout and are enabled.
        // Slice 2+3 split seats into two buckets (Seat.VenueZoneId XOR Seat.VenueTableId);
        // the hold check must consider BOTH — banquet/table-based events would otherwise
        // reject every table seat as "not belonging to this event".
        var layoutSeatIds = layout.Zones.SelectMany(z => z.Seats)
            .Concat(layout.Tables.SelectMany(t => t.Seats))
            .Where(s => s.IsEnabled)
            .Select(s => s.Id)
            .ToHashSet();

        var invalidSeats = request.SeatIds.Where(id => !layoutSeatIds.Contains(id)).ToList();
        if (invalidSeats.Count > 0)
            return Result<HoldSeatsResult>.Failure("One or more selected seats are not available or don't belong to this event");

        // Check for existing holds on these seats
        var heldSeatIds = await _seatHoldRepository.GetHeldSeatIdsAsync(request.SeatIds, cancellationToken);
        if (heldSeatIds.Count > 0)
            return Result<HoldSeatsResult>.Failure("One or more selected seats are already held by another user");

        // Check for existing reservations on these seats
        var reservedSeatIds = await _seatReservationRepository.GetReservedSeatIdsAsync(request.SeatIds, cancellationToken);
        if (reservedSeatIds.Count > 0)
            return Result<HoldSeatsResult>.Failure("One or more selected seats are already reserved");

        // Release any existing holds for this user's session first
        var existingHolds = await _seatHoldRepository.GetActiveHoldsBySessionAsync(request.SessionId, cancellationToken);
        foreach (var existingHold in existingHolds)
        {
            existingHold.Release();
        }

        // Create new holds
        var holds = new List<SeatHold>();
        foreach (var seatId in request.SeatIds)
        {
            var holdResult = SeatHold.Create(seatId, request.UserId, request.SessionId);
            if (holdResult.IsFailure)
                return Result<HoldSeatsResult>.Failure(holdResult.Error);

            holds.Add(holdResult.Value);
        }

        await _seatHoldRepository.AddRangeAsync(holds, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var expiresAt = holds.First().ExpiresAt;

        _logger.LogInformation(
            "Seats held successfully: EventId={EventId}, UserId={UserId}, HeldCount={Count}, ExpiresAt={ExpiresAt}",
            request.EventId, request.UserId, holds.Count, expiresAt);

        return Result<HoldSeatsResult>.Success(new HoldSeatsResult(
            request.SeatIds,
            expiresAt,
            request.SessionId));
    }
}
