using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Scheduling.Domain.ValueObjects;

/// <summary>
/// Reusable scheduling primitive: represents a repeating schedule pattern.
/// Wave 4.8.a (2026-06-26) NET-NEW type — no inline representation existed in the Event
/// aggregate because LankaEvents only modeled single-occurrence events. Future products
/// (LankaTemples weekly puja, LankaSeyla weekday opening hours) compose this VO from day one.
/// </summary>
/// <remarks>
/// <para>
/// Frequency model mirrors the iCalendar RRULE subset that the FE date-picker emits:
/// <see cref="RecurrenceFrequency.None"/> (single occurrence — the LankaEvents default),
/// <see cref="RecurrenceFrequency.Daily"/>, <see cref="RecurrenceFrequency.Weekly"/>,
/// <see cref="RecurrenceFrequency.Monthly"/>.
/// </para>
/// <para>
/// <see cref="Interval"/> = 1 means "every period"; 2 means "every other period"; etc.
/// <see cref="UntilDate"/> bounds the recurrence; null = open-ended (caller must terminate).
/// </para>
/// <para>
/// Materialization of concrete occurrence dates is a separate concern (Scheduling.Application
/// will host the <c>IOccurrenceMaterializer</c> service in W4.8.c). This VO is the spec only.
/// </para>
/// </remarks>
public sealed class RecurrenceRule : ValueObject
{
    public RecurrenceFrequency Frequency { get; }
    public int Interval { get; }
    public DateTime? UntilDate { get; }

    private RecurrenceRule() { }

    private RecurrenceRule(RecurrenceFrequency frequency, int interval, DateTime? untilDate)
    {
        Frequency = frequency;
        Interval = interval;
        UntilDate = untilDate;
    }

    /// <summary>
    /// Default for single-occurrence schedules (LankaEvents back-compat).
    /// </summary>
    public static RecurrenceRule None => new RecurrenceRule(RecurrenceFrequency.None, 1, null);

    public static Result<RecurrenceRule> Create(RecurrenceFrequency frequency, int interval = 1, DateTime? untilDate = null)
    {
        if (interval < 1)
            return Result<RecurrenceRule>.Failure(new Error("Scheduling.Recurrence.InvalidInterval", "Interval must be at least 1"));
        if (frequency == RecurrenceFrequency.None && (interval != 1 || untilDate.HasValue))
            return Result<RecurrenceRule>.Failure(new Error("Scheduling.Recurrence.NoneWithExtras", "RecurrenceFrequency.None must have interval=1 and no UntilDate"));
        return Result<RecurrenceRule>.Success(new RecurrenceRule(frequency, interval, untilDate));
    }

    public bool IsRecurring => Frequency != RecurrenceFrequency.None;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Frequency;
        yield return Interval;
        yield return UntilDate;
    }
}

public enum RecurrenceFrequency : byte
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
}
