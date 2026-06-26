using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Scheduling.Domain.ValueObjects;

/// <summary>
/// Reusable scheduling primitive: wraps the (start, end, timezone) triple that any
/// time-bounded resource needs. Wave 4.8.a (2026-06-26) extracted from the Event aggregate's
/// inline <c>StartDate</c> / <c>EndDate</c> / <c>TimeZoneId</c> fields.
/// </summary>
/// <remarks>
/// <para>
/// Both <see cref="StartDate"/> and <see cref="EndDate"/> are nullable to support the
/// LankaEvents "TBD" / "Planning" lifecycle (organizers commit the schedule only at
/// status transition Planning → Draft per Wave 8YA-2). <see cref="TimeZoneId"/> is an
/// IANA identifier (e.g. <c>"America/New_York"</c>) sourced from the event's location
/// US state; null when no location is set yet.
/// </para>
/// <para>
/// When both <see cref="StartDate"/> and <see cref="EndDate"/> are present, the invariant
/// <c>StartDate &lt;= EndDate</c> must hold. Validated at construction.
/// </para>
/// <para>
/// Future products (LankaTemples weekly puja, LankaSeyla one-off appointment) reuse this
/// VO so their own aggregates do not re-implement the nullable-date + timezone pattern.
/// </para>
/// </remarks>
public sealed class ScheduledOccurrence : ValueObject
{
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }
    public string? TimeZoneId { get; }

    private ScheduledOccurrence() { }

    private ScheduledOccurrence(DateTime? startDate, DateTime? endDate, string? timeZoneId)
    {
        StartDate = startDate;
        EndDate = endDate;
        TimeZoneId = timeZoneId;
    }

    public static Result<ScheduledOccurrence> Create(DateTime? startDate, DateTime? endDate, string? timeZoneId = null)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            return Result<ScheduledOccurrence>.Failure(new Error("Scheduling.Occurrence.InvalidDateRange", "EndDate must be on or after StartDate"));

        return Result<ScheduledOccurrence>.Success(new ScheduledOccurrence(startDate, endDate, timeZoneId));
    }

    /// <summary>
    /// Factory for the LankaEvents "TBD / Planning" state where dates are not yet committed.
    /// </summary>
    public static ScheduledOccurrence Tbd(string? timeZoneId = null) =>
        new ScheduledOccurrence(null, null, timeZoneId);

    /// <summary>
    /// True when both dates are set AND the start has not yet occurred (UTC).
    /// </summary>
    public bool IsUpcoming(DateTime nowUtc) =>
        StartDate.HasValue && StartDate.Value > nowUtc;

    /// <summary>
    /// True when both dates are set AND the end is in the past (UTC).
    /// </summary>
    public bool IsPast(DateTime nowUtc) =>
        EndDate.HasValue && EndDate.Value < nowUtc;

    public bool HasCommittedSchedule => StartDate.HasValue && EndDate.HasValue;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
        yield return TimeZoneId;
    }
}
