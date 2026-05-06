namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 7H — named-metric emission for the seat-hold lifecycle.
///
/// Architect §S6 (MVP gate) calls for three hold-lifecycle metrics on the
/// dashboard: <c>seat_hold.created</c>, <c>seat_hold.expired</c>, and
/// <c>seat_hold.converted_to_reservation</c>. Phase 7H emitted the first two;
/// Phase 8 S8.2.C wires up the third (now that the webhook actually converts
/// holds → reservations) plus a complementary <c>seat_conversion.race_lost</c>
/// metric for the rare case where a concurrent buyer wins the same seat.
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

    /// <summary>
    /// Phase 8 S8.2.C — fires once per registration whose pending seat
    /// assignments were converted to <c>SeatReservation</c> rows on payment
    /// completion (architect tag <c>seat_hold.converted_to_reservation</c>).
    /// Closes the metric gap from Phase 7H. Emitted from the webhook handler
    /// inside the same transaction as <c>CompletePayment</c>.
    /// </summary>
    void SeatHoldConvertedToReservation(Guid eventId, Guid registrationId, int seatCount);

    /// <summary>
    /// Phase 8 S8.2.C — fires once per seat that the webhook tried to
    /// convert but found already reserved by a concurrent buyer (architect
    /// tag <c>seat_conversion.race_lost</c>). Vanishingly rare; payment
    /// confirms regardless and the registration ends "confirmed-but-unseated".
    /// Used for ops dashboards / alerting on manual-reseat workload.
    /// </summary>
    void SeatConversionRaceLost(Guid eventId, Guid registrationId, Guid seatId);
}
