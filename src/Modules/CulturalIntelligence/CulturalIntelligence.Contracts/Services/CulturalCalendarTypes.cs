namespace LankaConnect.Modules.CulturalIntelligence.Contracts.Services;

// Wave 8.5 Tech Lead D-13 (Option A, 2026-07-19):
// Cultural-calendar VOs promoted from LankaEvents.Domain (ContactInfoPrimitives.cs
// + ValueObjects/Recommendations/EventRecommendation.cs) into CulturalIntelligence.Contracts
// so ICulturalCalendar's method signatures no longer type-depend on the LankaEvents Product
// layer. These are record types with value semantics — no ValueObject base needed at the
// Contracts boundary.

/// <summary>
/// Cultural appropriateness score expressed as a 0.0–1.0 value.
/// </summary>
public sealed record CulturalAppropriateness(double Value)
{
    public override string ToString() => Value.ToString("F2");
}

/// <summary>
/// Diaspora-friendliness classification for an event.
/// </summary>
public enum DiasporaFriendliness
{
    Traditional = 0,
    Moderate = 1,
    High = 2,
    VeryHigh = 3
}

/// <summary>
/// Broad classification of an event's cultural nature.
/// </summary>
public enum EventNature
{
    Religious = 0,
    Cultural = 1,
    Secular = 2,
    Mixed = 3
}

/// <summary>
/// A festival timing window (start/end + display name).
/// </summary>
public sealed record FestivalPeriod(DateTime StartDate, DateTime EndDate, string Name = "")
{
    public TimeSpan Duration => EndDate - StartDate;
}

/// <summary>
/// A culturally significant date within a year.
/// </summary>
public sealed record SignificantDate(string Name, DateTime Date, SignificanceLevel Level);

/// <summary>
/// Relative importance ranking for a cultural date.
/// </summary>
public enum SignificanceLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Result of validating an event against the cultural calendar.
/// </summary>
public sealed record CalendarValidationResult(
    bool IsValid,
    string Reason,
    IReadOnlyList<string>? Suggestions = null);
