using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing a cultural event synchronized with Google Calendar
/// Integrates Buddhist/Hindu celebrations with diaspora customization
/// </summary>
public sealed class GoogleCalendarCulturalEvent : ValueObject
{
    public CulturalEventType EventType { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public CulturalSignificance Significance { get; }
    public MultilingualDescription Description { get; }
    public DiasporaRelevance DiasporaRelevance { get; }
    public ReligiousObservanceLevel ObservanceLevel { get; }
    public GoogleCalendarEventId? CalendarEventId { get; }
    public CulturalEventFrequency Frequency { get; }
    public TempleScheduleIntegration? TempleIntegration { get; }

    private GoogleCalendarCulturalEvent(
        CulturalEventType eventType,
        DateTime startTime,
        DateTime endTime,
        CulturalSignificance significance,
        MultilingualDescription description,
        DiasporaRelevance diasporaRelevance,
        ReligiousObservanceLevel observanceLevel,
        CulturalEventFrequency frequency,
        GoogleCalendarEventId? calendarEventId = null,
        TempleScheduleIntegration? templeIntegration = null)
    {
        EventType = eventType;
        StartTime = startTime;
        EndTime = endTime;
        Significance = significance;
        Description = description;
        DiasporaRelevance = diasporaRelevance;
        ObservanceLevel = observanceLevel;
        Frequency = frequency;
        CalendarEventId = calendarEventId;
        TempleIntegration = templeIntegration;
    }

    /// <summary>
    /// Creates a Buddhist cultural event with lunar calendar precision
    /// </summary>
    public static Result<GoogleCalendarCulturalEvent> CreateBuddhistEvent(
        CulturalEventType eventType,
        DateTime startTime,
        DateTime endTime,
        MultilingualDescription description,
        DiasporaRelevance diasporaRelevance,
        TempleScheduleIntegration? templeIntegration = null)
    {
        if (!IsBuddhistEvent(eventType))
            return Result<GoogleCalendarCulturalEvent>.Failure("Event type is not a Buddhist cultural event");

        if (startTime >= endTime)
            return Result<GoogleCalendarCulturalEvent>.Failure("Start time must be before end time");

        var significance = GetBuddhistSignificance(eventType);
        var observanceLevel = GetBuddhistObservanceLevel(eventType);
        var frequency = GetEventFrequency(eventType);

        return Result<GoogleCalendarCulturalEvent>.Success(new GoogleCalendarCulturalEvent(
            eventType, startTime, endTime, significance, description, 
            diasporaRelevance, observanceLevel, frequency, null, templeIntegration));
    }

    /// <summary>
    /// Creates a Hindu cultural event with astronomical festival timing
    /// </summary>
    public static Result<GoogleCalendarCulturalEvent> CreateHinduEvent(
        CulturalEventType eventType,
        DateTime startTime,
        DateTime endTime,
        MultilingualDescription description,
        DiasporaRelevance diasporaRelevance,
        TempleScheduleIntegration? templeIntegration = null)
    {
        if (!IsHinduEvent(eventType))
            return Result<GoogleCalendarCulturalEvent>.Failure("Event type is not a Hindu cultural event");

        if (startTime >= endTime)
            return Result<GoogleCalendarCulturalEvent>.Failure("Start time must be before end time");

        var significance = GetHinduSignificance(eventType);
        var observanceLevel = GetHinduObservanceLevel(eventType);
        var frequency = GetEventFrequency(eventType);

        return Result<GoogleCalendarCulturalEvent>.Success(new GoogleCalendarCulturalEvent(
            eventType, startTime, endTime, significance, description,
            diasporaRelevance, observanceLevel, frequency, null, templeIntegration));
    }

    /// <summary>
    /// Creates a general cultural event (Sri Lankan New Year, cultural festivals)
    /// </summary>
    public static Result<GoogleCalendarCulturalEvent> Create(
        CulturalEventType eventType,
        DateTime startTime,
        DateTime endTime,
        CulturalSignificance significance,
        MultilingualDescription description,
        DiasporaRelevance diasporaRelevance,
        ReligiousObservanceLevel observanceLevel = ReligiousObservanceLevel.Medium,
        TempleScheduleIntegration? templeIntegration = null)
    {
        if (startTime >= endTime)
            return Result<GoogleCalendarCulturalEvent>.Failure("Start time must be before end time");

        var frequency = GetEventFrequency(eventType);

        return Result<GoogleCalendarCulturalEvent>.Success(new GoogleCalendarCulturalEvent(
            eventType, startTime, endTime, significance, description,
            diasporaRelevance, observanceLevel, frequency, null, templeIntegration));
    }

    /// <summary>
    /// Creates event with existing Google Calendar integration
    /// </summary>
    public GoogleCalendarCulturalEvent WithCalendarIntegration(GoogleCalendarEventId calendarEventId)
    {
        return new GoogleCalendarCulturalEvent(
            EventType, StartTime, EndTime, Significance, Description,
            DiasporaRelevance, ObservanceLevel, Frequency, calendarEventId, TempleIntegration);
    }

    /// <summary>
    /// Determines if the event conflicts with cultural observance periods
    /// </summary>
    public bool ConflictsWith(DateTime proposedTime, TimeSpan duration)
    {
        var proposedEndTime = proposedTime.Add(duration);
        
        // Check direct overlap
        if (proposedTime < EndTime && proposedEndTime > StartTime)
            return true;

        // Check observance-specific conflicts
        return ObservanceLevel switch
        {
            ReligiousObservanceLevel.High => ConflictsWithHighObservance(proposedTime),
            ReligiousObservanceLevel.Highest => ConflictsWithHighestObservance(proposedTime),
            _ => false
        };
    }

    private bool ConflictsWithHighObservance(DateTime proposedTime)
    {
        // High observance events like Poyaday affect morning hours
        if (EventType == CulturalEventType.Poyaday)
        {
            var poyadayMorning = StartTime.Date.AddHours(6);
            var poyadayNoon = StartTime.Date.AddHours(12);
            return proposedTime >= poyadayMorning && proposedTime <= poyadayNoon;
        }

        // Navaratri evenings are reserved for devotional activities
        if (EventType == CulturalEventType.Navaratri)
        {
            var eveningStart = StartTime.Date.AddHours(18);
            var eveningEnd = StartTime.Date.AddHours(22);
            return proposedTime >= eveningStart && proposedTime <= eveningEnd;
        }

        return false;
    }

    private bool ConflictsWithHighestObservance(DateTime proposedTime)
    {
        // Highest significance events like Vesak Poya affect the entire day
        return proposedTime.Date == StartTime.Date;
    }

    private static bool IsBuddhistEvent(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.VesakPoya => true,
            CulturalEventType.VesakDayBuddhist => true,
            CulturalEventType.Poyaday => true,
            CulturalEventType.PosonPoya => true,
            CulturalEventType.EsalaPerahera => true,
            CulturalEventType.UnduvapPoya => true,
            CulturalEventType.BuddhaPurnima => true,
            _ => false
        };
    }

    private static bool IsHinduEvent(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Deepavali => true,
            CulturalEventType.DiwaliHindu => true,
            CulturalEventType.Thaipusam => true,
            CulturalEventType.ThaipusamTamil => true,
            CulturalEventType.Navaratri => true,
            CulturalEventType.Holi => true,
            CulturalEventType.TamilNewYear => true,
            CulturalEventType.MahaShivaratri => true,
            _ => false
        };
    }

    private static CulturalSignificance GetBuddhistSignificance(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.VesakPoya => CulturalSignificance.Highest,
            CulturalEventType.VesakDayBuddhist => CulturalSignificance.Highest,
            CulturalEventType.EsalaPerahera => CulturalSignificance.Highest,
            CulturalEventType.Poyaday => CulturalSignificance.High,
            CulturalEventType.PosonPoya => CulturalSignificance.High,
            CulturalEventType.UnduvapPoya => CulturalSignificance.High,
            CulturalEventType.BuddhaPurnima => CulturalSignificance.High,
            _ => CulturalSignificance.Medium
        };
    }

    private static CulturalSignificance GetHinduSignificance(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Deepavali => CulturalSignificance.Highest,
            CulturalEventType.DiwaliHindu => CulturalSignificance.Highest,
            CulturalEventType.Thaipusam => CulturalSignificance.High,
            CulturalEventType.ThaipusamTamil => CulturalSignificance.High,
            CulturalEventType.Navaratri => CulturalSignificance.High,
            CulturalEventType.Holi => CulturalSignificance.High,
            CulturalEventType.TamilNewYear => CulturalSignificance.High,
            CulturalEventType.MahaShivaratri => CulturalSignificance.High,
            _ => CulturalSignificance.Medium
        };
    }

    private static ReligiousObservanceLevel GetBuddhistObservanceLevel(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.VesakPoya => ReligiousObservanceLevel.Highest,
            CulturalEventType.VesakDayBuddhist => ReligiousObservanceLevel.Highest,
            CulturalEventType.Poyaday => ReligiousObservanceLevel.High,
            CulturalEventType.PosonPoya => ReligiousObservanceLevel.High,
            CulturalEventType.EsalaPerahera => ReligiousObservanceLevel.High,
            CulturalEventType.UnduvapPoya => ReligiousObservanceLevel.High,
            CulturalEventType.BuddhaPurnima => ReligiousObservanceLevel.High,
            _ => ReligiousObservanceLevel.Medium
        };
    }

    private static ReligiousObservanceLevel GetHinduObservanceLevel(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Deepavali => ReligiousObservanceLevel.Highest,
            CulturalEventType.DiwaliHindu => ReligiousObservanceLevel.Highest,
            CulturalEventType.Thaipusam => ReligiousObservanceLevel.High,
            CulturalEventType.ThaipusamTamil => ReligiousObservanceLevel.High,
            CulturalEventType.Navaratri => ReligiousObservanceLevel.High,
            CulturalEventType.MahaShivaratri => ReligiousObservanceLevel.High,
            CulturalEventType.Holi => ReligiousObservanceLevel.Medium,
            _ => ReligiousObservanceLevel.Medium
        };
    }

    private static CulturalEventFrequency GetEventFrequency(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Poyaday => CulturalEventFrequency.Monthly,
            CulturalEventType.VesakPoya => CulturalEventFrequency.Annual,
            CulturalEventType.VesakDayBuddhist => CulturalEventFrequency.Annual,
            CulturalEventType.PosonPoya => CulturalEventFrequency.Annual,
            CulturalEventType.Deepavali => CulturalEventFrequency.Annual,
            CulturalEventType.DiwaliHindu => CulturalEventFrequency.Annual,
            CulturalEventType.Thaipusam => CulturalEventFrequency.Annual,
            CulturalEventType.ThaipusamTamil => CulturalEventFrequency.Annual,
            CulturalEventType.Navaratri => CulturalEventFrequency.BiAnnual,
            CulturalEventType.SinhalaNewYear => CulturalEventFrequency.Annual,
            CulturalEventType.TamilNewYear => CulturalEventFrequency.Annual,
            CulturalEventType.EsalaPerahera => CulturalEventFrequency.Annual,
            CulturalEventType.UnduvapPoya => CulturalEventFrequency.Annual,
            CulturalEventType.BuddhaPurnima => CulturalEventFrequency.Annual,
            CulturalEventType.MahaShivaratri => CulturalEventFrequency.Annual,
            _ => CulturalEventFrequency.Annual
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return EventType;
        yield return StartTime;
        yield return EndTime;
        yield return Significance;
        yield return Description;
        yield return DiasporaRelevance;
        yield return ObservanceLevel;
        yield return Frequency;
    }
}

// CulturalEventType enum moved to LankaConnect.Domain.Common.Enums

/// <summary>
/// Cultural significance levels for event prioritization
/// </summary>
public enum CulturalSignificance
{
    Low,
    Medium,
    High,
    Highest
}

/// <summary>
/// Religious observance levels affecting scheduling conflicts
/// </summary>
public enum ReligiousObservanceLevel
{
    None,
    Low,
    Medium,
    High,
    Highest
}

/// <summary>
/// Frequency patterns for recurring cultural events
/// </summary>
public enum CulturalEventFrequency
{
    OneTime,
    Monthly,
    BiAnnual,
    Annual,
    LunarBased,
    AstronomicalBased
}