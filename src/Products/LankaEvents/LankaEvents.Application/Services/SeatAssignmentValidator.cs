using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <inheritdoc cref="ISeatAssignmentValidator"/>
public class SeatAssignmentValidator : ISeatAssignmentValidator
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatReservationRepository _seatReservationRepository;
    private readonly ILogger<SeatAssignmentValidator> _logger;

    public SeatAssignmentValidator(
        IVenueLayoutRepository venueLayoutRepository,
        ISeatHoldRepository seatHoldRepository,
        ISeatReservationRepository seatReservationRepository,
        ILogger<SeatAssignmentValidator> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _seatHoldRepository = seatHoldRepository;
        _seatReservationRepository = seatReservationRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PendingSeatAssignment>>> ValidateAndBuildAssignmentsAsync(
        Guid eventId,
        string seatSessionId,
        IReadOnlyList<Guid> seatIds,
        int attendeeCount,
        CancellationToken cancellationToken = default)
    {
        // Up-front validation: cheap checks first to fail fast.
        if (string.IsNullOrWhiteSpace(seatSessionId))
            return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                "Seat-hold session id is required for assigned-seating registration");

        if (seatIds == null || seatIds.Count == 0)
            return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                "At least one seat must be selected for assigned-seating registration");

        if (seatIds.Count != attendeeCount)
            return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                $"Seat count {seatIds.Count} does not match attendee count {attendeeCount}. " +
                $"One seat per attendee is required for assigned-seating registration.");

        if (seatIds.Distinct().Count() != seatIds.Count)
            return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                "Duplicate seat ids in the request are not allowed");

        _logger.LogInformation(
            "[Phase 8 S8.2.B] Validating seat assignments — EventId={EventId}, SessionId={SessionId}, SeatCount={Count}",
            eventId, seatSessionId, seatIds.Count);

        // Step 1: load the event's assigned layout. Without it we have no way to
        // verify seat-id membership or look up labels.
        VenueLayout? layout;
        try
        {
            layout = await _venueLayoutRepository.GetAssignedLayoutForEventAsync(
                eventId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 8 S8.2.B] Failed to load assigned layout for event {EventId}", eventId);
            throw;
        }

        if (layout is null)
        {
            return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                "Event has no assigned-seating layout — selecting seats is not supported");
        }

        // Step 2: verify every requested seatId belongs to this layout (across
        // both zones and tables) and look up its label.
        var seatLookup = layout.Zones.SelectMany(z => z.Seats)
            .Concat(layout.Tables.SelectMany(t => t.Seats))
            .ToDictionary(s => s.Id, s => s);

        var seatLabels = new string[seatIds.Count];
        for (var i = 0; i < seatIds.Count; i++)
        {
            if (!seatLookup.TryGetValue(seatIds[i], out var seat))
            {
                _logger.LogWarning(
                    "[Phase 8 S8.2.B] Seat {SeatId} is not part of layout {LayoutId} (event {EventId})",
                    seatIds[i], layout.Id, eventId);
                return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                    $"Seat {seatIds[i]} is not part of this event's layout");
            }
            seatLabels[i] = seat.Label;
        }

        // Step 3: every seatId must be currently held in this session (rejects
        // borrowed IDs from a different buyer's session).
        IReadOnlyList<SeatHold> activeHolds;
        try
        {
            activeHolds = await _seatHoldRepository.GetActiveHoldsBySessionAsync(
                seatSessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 8 S8.2.B] Failed to load active holds for session {SessionId}", seatSessionId);
            throw;
        }

        var heldSeatIdsInSession = activeHolds.Select(h => h.SeatId).ToHashSet();
        foreach (var requestedSeatId in seatIds)
        {
            if (!heldSeatIdsInSession.Contains(requestedSeatId))
            {
                _logger.LogWarning(
                    "[Phase 8 S8.2.B] Seat {SeatId} is not held in session {SessionId} (event {EventId})",
                    requestedSeatId, seatSessionId, eventId);
                return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                    $"Seat {requestedSeatId} is not held in your session — re-select your seats and try again");
            }
        }

        // Step 4: defence in depth — none of the seats may be already reserved.
        // The DB unique index on seat_reservations.seat_id will also reject a
        // double-insert, but failing here gives a friendlier user message.
        IReadOnlyList<Guid> reservedSeatIds;
        try
        {
            reservedSeatIds = await _seatReservationRepository.GetReservedSeatIdsAsync(
                seatIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 8 S8.2.B] Failed to load reserved seats for event {EventId}", eventId);
            throw;
        }

        if (reservedSeatIds.Count > 0)
        {
            _logger.LogWarning(
                "[Phase 8 S8.2.B] {Count} requested seat(s) already reserved by another buyer — EventId={EventId}",
                reservedSeatIds.Count, eventId);
            return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(
                $"{reservedSeatIds.Count} of your selected seat(s) have already been reserved by another buyer — re-select your seats and try again");
        }

        // Step 5: build the PendingSeatAssignment list in input order.
        var assignments = new List<PendingSeatAssignment>(seatIds.Count);
        for (var i = 0; i < seatIds.Count; i++)
        {
            var assignmentResult = PendingSeatAssignment.Create(i, seatIds[i], seatLabels[i]);
            if (assignmentResult.IsFailure)
            {
                // Should be unreachable given the prior guards — but log loudly.
                _logger.LogError(
                    "[Phase 8 S8.2.B] PendingSeatAssignment.Create failed unexpectedly — Index={Index}, SeatId={SeatId}, SeatLabel={SeatLabel}, Error={Error}",
                    i, seatIds[i], seatLabels[i], assignmentResult.Error);
                return Result<IReadOnlyList<PendingSeatAssignment>>.Failure(assignmentResult.Error);
            }
            assignments.Add(assignmentResult.Value);
        }

        _logger.LogInformation(
            "[Phase 8 S8.2.B] Seat assignments validated successfully — EventId={EventId}, SessionId={SessionId}, SeatCount={Count}",
            eventId, seatSessionId, assignments.Count);

        return Result<IReadOnlyList<PendingSeatAssignment>>.Success(assignments);
    }
}
