using LankaConnect.Modules.CulturalIntelligence.Contracts.Services;

namespace LankaConnect.Modules.CulturalIntelligence.Infrastructure;

/// <summary>
/// Stub implementation of <see cref="ICulturalCalendar"/> retained during Wave 8.5 GAP-1
/// interface refactor (D-13 Option A, 2026-07-19). Signatures updated to consume
/// <see cref="EventCulturalContext"/> instead of the LankaEvents Product-layer
/// <c>Event</c> aggregate; behavior kept at the same neutral defaults.
///
/// SUPERSEDED BY <see cref="PoyaCalendarService"/> — the real seed-file-backed impl
/// that closes GAP-1. This stub file will be deleted in the follow-up commit that
/// wires PoyaCalendarService into DI.
/// </summary>
public class StubCulturalCalendar : ICulturalCalendar
{
    public bool IsPoyaDay(DateTime date) => false;

    public CulturalAppropriateness GetEventAppropriateness(EventCulturalContext context, DateTime date)
        => new CulturalAppropriateness(0.7);

    public string ClassifyEventType(EventCulturalContext context) => "General";

    public DiasporaFriendliness GetDiasporaFriendliness(EventCulturalContext context)
        => DiasporaFriendliness.Moderate;

    public CulturalAppropriateness CalculateAppropriateness(EventCulturalContext context, string culturalBackground)
        => new CulturalAppropriateness(0.7);

    public FestivalPeriod GetFestivalPeriod(string festivalName, int year)
        => new FestivalPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), festivalName);

    public bool IsOptimalFestivalTiming(EventCulturalContext context, FestivalPeriod period) => false;

    public EventNature ClassifyEventNature(EventCulturalContext context) => EventNature.Cultural;

    public IReadOnlyList<SignificantDate> GetSignificantDates(int year) => Array.Empty<SignificantDate>();

    public CalendarValidationResult ValidateEventAgainstCalendar(EventCulturalContext context)
        => new CalendarValidationResult(true, "Stub validation passed");
}
