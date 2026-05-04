namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 7H — named-metric emission for the seat-hold lifecycle.
///
/// Architect §S6 (MVP gate) calls for three hold-lifecycle metrics on the
/// dashboard: <c>seat_hold.created</c>, <c>seat_hold.expired</c>, and
/// <c>seat_hold.converted_to_reservation</c>. The first two are emitted by
/// this interface. The third is currently unimplementable because no code
/// path actually creates <c>SeatReservation</c> rows yet (the read-side
/// guards query that table but the write-side conversion is missing) —
/// tracked as a separate feature gap.
///
/// Channel: structured Serilog log events with the same <c>Metric {MetricName}</c>
/// template as <see cref="LayoutMetrics"/>, so log-analytics queries pick them
/// up by <c>MetricName</c> property without bespoke parsing.
/// </summary>
public interface ISeatHoldMetrics
{
    /// <summary>
    /// Fires once per successful hold operation (architect dashboard tag
    /// <c>seat_hold.created</c>). The session id is intentionally NOT a tag
    /// (high-cardinality user-supplied value); the count is.
    /// </summary>
    void SeatHoldCreated(Guid eventId, int seatCount);

    /// <summary>
    /// Fires from the background <c>SeatHoldCleanupService</c> when expired
    /// holds are released (architect tag <c>seat_hold.expired</c>). Reports
    /// the number expired in this pass — emit at the end of the cleanup
    /// loop, not per-row, to keep log volume bounded.
    /// </summary>
    void SeatHoldExpired(int expiredCount);
}
