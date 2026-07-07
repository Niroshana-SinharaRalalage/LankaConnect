using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects.Recommendations;

namespace LankaConnect.Modules.CulturalIntelligence.Infrastructure;

/// <summary>
/// Stub implementation of ICulturalCalendar for MVP
/// TODO: Replace with real cultural calendar integration in Phase 2
/// </summary>
public class StubCulturalCalendar : ICulturalCalendar
{
    public bool IsPoyaday(DateTime date) => false; // Stub: Return false for MVP

    public CulturalAppropriateness GetEventAppropriateness(Event @event, DateTime date)
        => new CulturalAppropriateness(0.7); // Stub: Default 70% appropriateness

    public string ClassifyEventType(Event @event) => "General"; // Stub: Default classification

    public DiasporaFriendliness GetDiasporaFriendliness(Event @event)
        => DiasporaFriendliness.Moderate; // Stub: Return enum value (Moderate diaspora-friendliness)

    public CulturalAppropriateness CalculateAppropriateness(Event @event, string culturalBackground)
        => new CulturalAppropriateness(0.7); // Stub: Default 70% appropriateness

    public FestivalPeriod GetFestivalPeriod(string festivalName, int year)
        => new FestivalPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), festivalName); // Stub: 7-day period

    public bool IsOptimalFestivalTiming(Event @event, FestivalPeriod period) => false; // Stub: Return false for MVP

    public EventNature ClassifyEventNature(Event @event) => EventNature.Cultural; // Stub: Default to Cultural nature

    public SignificantDate[] GetSignificantDates(int year) => Array.Empty<SignificantDate>(); // Stub: No significant dates for MVP

    public CalendarValidationResult ValidateEventAgainstCalendar(Event @event)
        => new CalendarValidationResult(true, "Stub validation passed"); // Stub: Always valid for MVP
}
