using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Services;

/// <inheritdoc cref="ISeatHoldMetrics"/>
public class SeatHoldMetrics : ISeatHoldMetrics
{
    private readonly ILogger<SeatHoldMetrics> _logger;

    public SeatHoldMetrics(ILogger<SeatHoldMetrics> logger)
    {
        _logger = logger;
    }

    public void SeatHoldCreated(Guid eventId, int seatCount)
    {
        _logger.LogInformation(
            "Metric {MetricName} EventId={EventId} SeatCount={SeatCount}",
            "seat_hold.created",
            eventId,
            seatCount);
    }

    public void SeatHoldExpired(int expiredCount)
    {
        // Quiet path — emitted every cleanup pass even when count=0 so the
        // dashboard can prove the SeatHoldCleanupService is alive. Volume
        // bound: ~1 line/min per replica.
        _logger.LogInformation(
            "Metric {MetricName} ExpiredCount={ExpiredCount}",
            "seat_hold.expired",
            expiredCount);
    }

    public void SeatHoldConvertedToReservation(Guid eventId, Guid registrationId, int seatCount)
    {
        _logger.LogInformation(
            "Metric {MetricName} EventId={EventId} RegistrationId={RegistrationId} SeatCount={SeatCount}",
            "seat_hold.converted_to_reservation",
            eventId,
            registrationId,
            seatCount);
    }

    public void SeatConversionRaceLost(Guid eventId, Guid registrationId, Guid seatId)
    {
        _logger.LogWarning(
            "Metric {MetricName} EventId={EventId} RegistrationId={RegistrationId} SeatId={SeatId}",
            "seat_conversion.race_lost",
            eventId,
            registrationId,
            seatId);
    }
}
