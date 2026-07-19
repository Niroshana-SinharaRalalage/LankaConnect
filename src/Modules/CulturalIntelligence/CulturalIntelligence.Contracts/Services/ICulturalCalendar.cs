namespace LankaConnect.Modules.CulturalIntelligence.Contracts.Services;

/// <summary>
/// Cultural-calendar contract for Buddhist / Hindu / Sri Lankan poya-day awareness.
///
/// Wave 8.5 Tech Lead D-13 (Option A, 2026-07-19):
/// Promoted from <c>LankaConnect.Products.LankaEvents.Domain.Services.ICulturalCalendar</c>
/// into CulturalIntelligence.Contracts (Consult #15 PASS C — interface + DTO in Contracts).
/// Method signatures refactored to take primitives + <see cref="EventCulturalContext"/>
/// instead of the LankaEvents Product-layer <c>Event</c> aggregate so this Capability
/// contract no longer depends on any Product.
///
/// Consumers (currently <c>EventRecommendationEngine</c> in LankaEvents.Domain) construct
/// <see cref="EventCulturalContext"/> at the call boundary from their local Event data.
/// </summary>
public interface ICulturalCalendar
{
    /// <summary>Is the given date a poya (Buddhist full-moon) day?</summary>
    bool IsPoyaDay(DateTime date);

    /// <summary>
    /// Cultural appropriateness of holding <paramref name="context"/> on <paramref name="date"/>.
    /// </summary>
    CulturalAppropriateness GetEventAppropriateness(EventCulturalContext context, DateTime date);

    /// <summary>
    /// Coarse classification label for the event (e.g. "Religious", "Cultural", "Secular", "Mixed").
    /// </summary>
    string ClassifyEventType(EventCulturalContext context);

    /// <summary>Diaspora-friendliness of the event.</summary>
    DiasporaFriendliness GetDiasporaFriendliness(EventCulturalContext context);

    /// <summary>
    /// Cultural appropriateness of the event relative to a user's cultural background.
    /// </summary>
    CulturalAppropriateness CalculateAppropriateness(EventCulturalContext context, string culturalBackground);

    /// <summary>Timing window for a named festival in the given year.</summary>
    FestivalPeriod GetFestivalPeriod(string festivalName, int year);

    /// <summary>Does the event fall inside the optimal window for the given festival period?</summary>
    bool IsOptimalFestivalTiming(EventCulturalContext context, FestivalPeriod period);

    /// <summary>Nature classification for the event (Religious / Cultural / Secular / Mixed).</summary>
    EventNature ClassifyEventNature(EventCulturalContext context);

    /// <summary>All culturally significant dates within the given year (poya days, festivals, etc.).</summary>
    IReadOnlyList<SignificantDate> GetSignificantDates(int year);

    /// <summary>Validate the event against the cultural calendar; reports conflicts and suggestions.</summary>
    CalendarValidationResult ValidateEventAgainstCalendar(EventCulturalContext context);
}
