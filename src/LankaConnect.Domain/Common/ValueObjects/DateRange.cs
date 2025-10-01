using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing a date range with cultural intelligence and disaster recovery features.
/// Supports temporal operations for the LankaConnect platform's cultural calendar and backup systems.
/// </summary>
public sealed class DateRange : ValueObject
{
    /// <summary>
    /// Gets the start date of the range.
    /// </summary>
    public DateTime StartDate { get; }

    /// <summary>
    /// Gets the end date of the range.
    /// </summary>
    public DateTime EndDate { get; }

    /// <summary>
    /// Gets the duration of the date range.
    /// </summary>
    public TimeSpan Duration => EndDate - StartDate;

    /// <summary>
    /// Gets the cultural event type for specialized calendar handling.
    /// </summary>
    public string? CulturalEventType { get; private set; }

    /// <summary>
    /// Gets the cultural region for diaspora community targeting.
    /// </summary>
    public string? CulturalRegion { get; private set; }

    /// <summary>
    /// Gets whether this date range is culturally significant.
    /// </summary>
    public bool IsCulturallySignificant => !string.IsNullOrEmpty(CulturalEventType);

    /// <summary>
    /// Gets the backup type for disaster recovery windows.
    /// </summary>
    public string? BackupType { get; private set; }

    /// <summary>
    /// Gets the traffic profile for backup scheduling.
    /// </summary>
    public string? TrafficProfile { get; private set; }

    /// <summary>
    /// Gets whether this date range represents a monthly period.
    /// </summary>
    public bool IsMonthlyRange
    {
        get
        {
            if (StartDate.Day != 1) return false;
            var nextMonth = StartDate.AddMonths(1);
            var lastDayOfMonth = nextMonth.AddDays(-1);
            return EndDate.Date == lastDayOfMonth.Date;
        }
    }

    /// <summary>
    /// Private constructor for value object creation.
    /// </summary>
    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Factory method to create a new DateRange with validation.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <returns>A new DateRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when end date is before start date.</exception>
    public static DateRange Create(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("The end date must be greater than or equal to start date");

        return new DateRange(startDate, endDate);
    }

    /// <summary>
    /// Sets cultural intelligence context for specialized calendar monitoring.
    /// </summary>
    /// <param name="culturalEventType">The cultural event type (e.g., Vesak_Poya).</param>
    /// <param name="culturalRegion">The cultural region (e.g., Sri_Lankan_Buddhism).</param>
    public void SetCulturalContext(string culturalEventType, string culturalRegion)
    {
        CulturalEventType = culturalEventType;
        CulturalRegion = culturalRegion;
    }

    /// <summary>
    /// Sets backup context for disaster recovery operations.
    /// </summary>
    /// <param name="backupType">The backup type (e.g., Daily_Full_Backup).</param>
    /// <param name="trafficProfile">The traffic profile (e.g., Low_Traffic_Window).</param>
    public void SetBackupContext(string backupType, string trafficProfile)
    {
        BackupType = backupType;
        TrafficProfile = trafficProfile;
    }

    /// <summary>
    /// Checks if the date range contains the specified date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is within the range, false otherwise.</returns>
    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    /// <summary>
    /// Checks if this date range overlaps with another date range.
    /// </summary>
    /// <param name="other">The other date range to check against.</param>
    /// <returns>True if the ranges overlap, false otherwise.</returns>
    public bool Overlaps(DateRange other)
    {
        if (other == null) return false;
        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }

    /// <summary>
    /// Gets the intersection of this date range with another date range.
    /// </summary>
    /// <param name="other">The other date range to intersect with.</param>
    /// <returns>The intersecting date range, or null if no intersection exists.</returns>
    public DateRange? Intersection(DateRange other)
    {
        if (other == null || !Overlaps(other)) return null;

        var intersectionStart = StartDate > other.StartDate ? StartDate : other.StartDate;
        var intersectionEnd = EndDate < other.EndDate ? EndDate : other.EndDate;

        return Create(intersectionStart, intersectionEnd);
    }

    /// <summary>
    /// Gets the union of this date range with another date range.
    /// </summary>
    /// <param name="other">The other date range to union with.</param>
    /// <returns>The union date range covering both ranges.</returns>
    /// <exception cref="ArgumentException">Thrown when ranges don't overlap or touch.</exception>
    public DateRange Union(DateRange other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (!Overlaps(other) && !IsAdjacent(other))
            throw new ArgumentException("Cannot create union of non-overlapping, non-adjacent date ranges");

        var unionStart = StartDate < other.StartDate ? StartDate : other.StartDate;
        var unionEnd = EndDate > other.EndDate ? EndDate : other.EndDate;

        return Create(unionStart, unionEnd);
    }

    /// <summary>
    /// Checks if this date range is adjacent to another date range.
    /// </summary>
    /// <param name="other">The other date range to check against.</param>
    /// <returns>True if the ranges are adjacent, false otherwise.</returns>
    public bool IsAdjacent(DateRange other)
    {
        if (other == null) return false;
        return EndDate.AddDays(1).Date == other.StartDate.Date || 
               other.EndDate.AddDays(1).Date == StartDate.Date;
    }

    /// <summary>
    /// Shifts the date range by the specified time span.
    /// </summary>
    /// <param name="shift">The time span to shift by.</param>
    /// <returns>A new DateRange shifted by the specified amount.</returns>
    public DateRange Shift(TimeSpan shift)
    {
        return Create(StartDate.Add(shift), EndDate.Add(shift));
    }

    /// <summary>
    /// Expands the date range by the specified time span on both ends.
    /// </summary>
    /// <param name="expansion">The time span to expand by.</param>
    /// <returns>A new DateRange expanded by the specified amount.</returns>
    public DateRange Expand(TimeSpan expansion)
    {
        return Create(StartDate.Subtract(expansion), EndDate.Add(expansion));
    }

    /// <summary>
    /// Gets the equality components for value object comparison.
    /// </summary>
    /// <returns>The components used for equality comparison.</returns>
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
        yield return CulturalEventType ?? string.Empty;
        yield return CulturalRegion ?? string.Empty;
        yield return BackupType ?? string.Empty;
        yield return TrafficProfile ?? string.Empty;
    }

    /// <summary>
    /// Returns a string representation of the date range.
    /// </summary>
    /// <returns>A string describing the date range.</returns>
    public override string ToString()
    {
        var result = $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({Duration.Days} days)";
        
        if (IsCulturallySignificant)
            result += $" [Cultural: {CulturalEventType}]";
            
        if (!string.IsNullOrEmpty(BackupType))
            result += $" [Backup: {BackupType}]";
            
        return result;
    }

}