using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Services;

/// <inheritdoc />
public class StructuralEditGuard : IStructuralEditGuard
{
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatReservationRepository _seatReservationRepository;
    private readonly ILogger<StructuralEditGuard> _logger;

    public StructuralEditGuard(
        ISeatHoldRepository seatHoldRepository,
        ISeatReservationRepository seatReservationRepository,
        ILogger<StructuralEditGuard> logger)
    {
        _seatHoldRepository = seatHoldRepository;
        _seatReservationRepository = seatReservationRepository;
        _logger = logger;
    }

    public async Task<Result> CheckSeatsAsync(IEnumerable<Guid> seatIds, CancellationToken cancellationToken)
    {
        var materialized = seatIds as IReadOnlyCollection<Guid> ?? seatIds?.ToList() ?? (IReadOnlyCollection<Guid>)Array.Empty<Guid>();

        if (materialized.Count == 0)
        {
            return Result.Success();
        }

        IReadOnlyList<Guid> heldSeatIds;
        IReadOnlyList<Guid> reservedSeatIds;
        try
        {
            heldSeatIds = await _seatHoldRepository.GetHeldSeatIdsAsync(materialized, cancellationToken);
            reservedSeatIds = await _seatReservationRepository.GetReservedSeatIdsAsync(materialized, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "StructuralEditGuard: failed to query seat hold/reservation state. AffectedSeatCount={Count}",
                materialized.Count);
            throw;
        }

        var heldCount = heldSeatIds.Count;
        var reservedCount = reservedSeatIds.Count;

        if (heldCount == 0 && reservedCount == 0)
        {
            return Result.Success();
        }

        _logger.LogWarning(
            "StructuralEditGuard: structural edit blocked — {HeldCount} seat(s) held, {ReservedCount} seat(s) reserved. " +
            "AffectedSeatCount={Count}",
            heldCount, reservedCount, materialized.Count);

        // Message format intentionally mentions both counts so the UI can show a diagnostic
        // even when only one type is in play (the other will read "0").
        var message =
            $"Cannot modify layout structure: {heldCount} seat(s) currently held, " +
            $"{reservedCount} seat(s) reserved. Wait for holds to expire or cancel affected registrations first.";

        return Result.StructuralEditRejected(message);
    }
}
