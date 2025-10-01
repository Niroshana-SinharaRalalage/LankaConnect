using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing a cultural scheduling conflict and resolution
/// Provides intelligent conflict detection with alternative suggestions
/// </summary>
public sealed class CulturalConflict : ValueObject
{
    public bool HasConflict { get; }
    public string ConflictReason { get; }
    public CulturalEventType ConflictingEventType { get; }
    public ReligiousObservanceLevel ConflictSeverity { get; }
    public DateTime SuggestedAlternativeTime { get; }
    public IEnumerable<DateTime> AlternativeTimeSlots { get; }
    public CulturalResolutionStrategy RecommendedStrategy { get; }
    public string CulturalGuidance { get; }

    private CulturalConflict(
        bool hasConflict,
        string conflictReason,
        CulturalEventType conflictingEventType,
        ReligiousObservanceLevel conflictSeverity,
        DateTime suggestedAlternativeTime,
        IEnumerable<DateTime> alternativeTimeSlots,
        CulturalResolutionStrategy recommendedStrategy,
        string culturalGuidance)
    {
        HasConflict = hasConflict;
        ConflictReason = conflictReason;
        ConflictingEventType = conflictingEventType;
        ConflictSeverity = conflictSeverity;
        SuggestedAlternativeTime = suggestedAlternativeTime;
        AlternativeTimeSlots = alternativeTimeSlots;
        RecommendedStrategy = recommendedStrategy;
        CulturalGuidance = culturalGuidance;
    }

    /// <summary>
    /// Creates a no-conflict result
    /// </summary>
    public static CulturalConflict NoConflict()
    {
        return new CulturalConflict(
            false, string.Empty, CulturalEventType.Community,
            ReligiousObservanceLevel.None, DateTime.MinValue,
            Enumerable.Empty<DateTime>(), CulturalResolutionStrategy.NoAction, string.Empty);
    }

    /// <summary>
    /// Creates a Buddhist observance conflict
    /// </summary>
    public static CulturalConflict CreateBuddhistConflict(
        DateTime proposedTime,
        CulturalEventType conflictingEvent,
        ReligiousObservanceLevel severity)
    {
        var conflictReason = GetBuddhistConflictReason(conflictingEvent);
        var alternatives = GenerateBuddhistAlternatives(proposedTime, conflictingEvent);
        var suggestedTime = alternatives.First();
        var strategy = GetBuddhistResolutionStrategy(conflictingEvent, severity);
        var guidance = GetBuddhistCulturalGuidance(conflictingEvent);

        return new CulturalConflict(
            true, conflictReason, conflictingEvent, severity,
            suggestedTime, alternatives, strategy, guidance);
    }

    /// <summary>
    /// Creates a Hindu observance conflict
    /// </summary>
    public static CulturalConflict CreateHinduConflict(
        DateTime proposedTime,
        CulturalEventType conflictingEvent,
        ReligiousObservanceLevel severity)
    {
        var conflictReason = GetHinduConflictReason(conflictingEvent);
        var alternatives = GenerateHinduAlternatives(proposedTime, conflictingEvent);
        var suggestedTime = alternatives.First();
        var strategy = GetHinduResolutionStrategy(conflictingEvent, severity);
        var guidance = GetHinduCulturalGuidance(conflictingEvent);

        return new CulturalConflict(
            true, conflictReason, conflictingEvent, severity,
            suggestedTime, alternatives, strategy, guidance);
    }

    /// <summary>
    /// Creates a general cultural conflict
    /// </summary>
    public static CulturalConflict CreateCulturalConflict(
        DateTime proposedTime,
        CulturalEventType conflictingEvent,
        string customReason,
        ReligiousObservanceLevel severity = ReligiousObservanceLevel.Medium)
    {
        var alternatives = GenerateGeneralAlternatives(proposedTime);
        var suggestedTime = alternatives.First();
        var strategy = CulturalResolutionStrategy.RescheduleRecommended;
        var guidance = $"Consider rescheduling to respect {conflictingEvent} observance";

        return new CulturalConflict(
            true, customReason, conflictingEvent, severity,
            suggestedTime, alternatives, strategy, guidance);
    }

    /// <summary>
    /// Determines if conflict can be automatically resolved
    /// </summary>
    public bool CanAutoResolve()
    {
        return ConflictSeverity <= ReligiousObservanceLevel.Medium &&
               RecommendedStrategy != CulturalResolutionStrategy.ManualResolutionRequired;
    }

    /// <summary>
    /// Gets cultural sensitivity score (0-100)
    /// </summary>
    public int GetSensitivityScore()
    {
        return ConflictSeverity switch
        {
            ReligiousObservanceLevel.None => 0,
            ReligiousObservanceLevel.Low => 25,
            ReligiousObservanceLevel.Medium => 50,
            ReligiousObservanceLevel.High => 75,
            ReligiousObservanceLevel.Highest => 100,
            _ => 0
        };
    }

    private static string GetBuddhistConflictReason(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Poyaday => "Poyaday observance period - meditation and contemplation time",
            CulturalEventType.VesakPoya => "Vesak Poya - most sacred Buddhist day requiring reverence",
            CulturalEventType.UnduvapPoya => "Unduvap Poya - commemoration of arrival of Buddhism to Sri Lanka",
            CulturalEventType.EsalaPerahera => "Esala Perahera - sacred procession period",
            CulturalEventType.BuddhaPurnima => "Buddha Purnima - celebration of Buddha's birth",
            _ => "Buddhist observance period"
        };
    }

    private static string GetHinduConflictReason(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Deepavali => "Deepavali celebration - festival of lights family time",
            CulturalEventType.Thaipusam => "Thaipusam observance - devotional pilgrimage period",
            CulturalEventType.Navaratri => "Navaratri devotional period - evening worship time",
            CulturalEventType.Holi => "Holi celebration - festival of colors community gathering",
            CulturalEventType.TamilNewYear => "Tamil New Year - family celebration and traditions",
            _ => "Hindu observance period"
        };
    }

    private static IEnumerable<DateTime> GenerateBuddhistAlternatives(
        DateTime proposedTime, CulturalEventType eventType)
    {
        var alternatives = new List<DateTime>();

        if (eventType == CulturalEventType.Poyaday)
        {
            // Poyaday affects morning hours (6 AM - 12 PM)
            if (proposedTime.Hour >= 6 && proposedTime.Hour <= 12)
            {
                // Suggest afternoon or next day morning
                alternatives.Add(proposedTime.Date.AddHours(14)); // 2 PM same day
                alternatives.Add(proposedTime.Date.AddDays(1).AddHours(9)); // 9 AM next day
                alternatives.Add(proposedTime.Date.AddHours(16)); // 4 PM same day
            }
        }
        else if (eventType == CulturalEventType.VesakPoya)
        {
            // Vesak affects entire day - suggest next day
            alternatives.Add(proposedTime.AddDays(1));
            alternatives.Add(proposedTime.AddDays(2));
            alternatives.Add(proposedTime.AddDays(-1)); // Previous day if possible
        }

        return alternatives.Any() ? alternatives : GenerateGeneralAlternatives(proposedTime);
    }

    private static IEnumerable<DateTime> GenerateHinduAlternatives(
        DateTime proposedTime, CulturalEventType eventType)
    {
        var alternatives = new List<DateTime>();

        if (eventType == CulturalEventType.Navaratri)
        {
            // Navaratri affects evening hours (6 PM - 10 PM)
            if (proposedTime.Hour >= 18 && proposedTime.Hour <= 22)
            {
                // Suggest morning or afternoon
                alternatives.Add(proposedTime.Date.AddHours(10)); // 10 AM same day
                alternatives.Add(proposedTime.Date.AddHours(14)); // 2 PM same day
                alternatives.Add(proposedTime.Date.AddDays(1).AddHours(proposedTime.Hour)); // Same time next day
            }
        }
        else if (eventType == CulturalEventType.Deepavali)
        {
            // Deepavali affects evening and night - suggest next day
            alternatives.Add(proposedTime.AddDays(1));
            alternatives.Add(proposedTime.AddDays(2));
            alternatives.Add(proposedTime.Date.AddHours(10)); // Morning same day if early conflict
        }

        return alternatives.Any() ? alternatives : GenerateGeneralAlternatives(proposedTime);
    }

    private static IEnumerable<DateTime> GenerateGeneralAlternatives(DateTime proposedTime)
    {
        return new[]
        {
            proposedTime.AddHours(2),
            proposedTime.AddDays(1),
            proposedTime.AddHours(-2),
            proposedTime.AddDays(1).AddHours(2)
        };
    }

    private static CulturalResolutionStrategy GetBuddhistResolutionStrategy(
        CulturalEventType eventType, ReligiousObservanceLevel severity)
    {
        return severity switch
        {
            ReligiousObservanceLevel.Highest => CulturalResolutionStrategy.MustReschedule,
            ReligiousObservanceLevel.High => CulturalResolutionStrategy.RescheduleRecommended,
            _ => CulturalResolutionStrategy.FlexibleRescheduling
        };
    }

    private static CulturalResolutionStrategy GetHinduResolutionStrategy(
        CulturalEventType eventType, ReligiousObservanceLevel severity)
    {
        return severity switch
        {
            ReligiousObservanceLevel.Highest => CulturalResolutionStrategy.MustReschedule,
            ReligiousObservanceLevel.High => CulturalResolutionStrategy.RescheduleRecommended,
            _ => CulturalResolutionStrategy.FlexibleRescheduling
        };
    }

    private static string GetBuddhistCulturalGuidance(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Poyaday => "Poyaday is a time for meditation and spiritual reflection. Consider scheduling important meetings for afternoon hours.",
            CulturalEventType.VesakPoya => "Vesak Poya is the most sacred day in Buddhism. Please reschedule to show respect for this holy day.",
            CulturalEventType.EsalaPerahera => "Esala Perahera is a significant cultural and religious celebration. Consider the community's participation needs.",
            _ => "Please consider the spiritual significance of this Buddhist observance when scheduling."
        };
    }

    private static string GetHinduCulturalGuidance(CulturalEventType eventType)
    {
        return eventType switch
        {
            CulturalEventType.Deepavali => "Deepavali is the festival of lights, celebrated with family gatherings. Evening hours are particularly important for celebrations.",
            CulturalEventType.Thaipusam => "Thaipusam is a day of devotion and pilgrimage. Temple visits and prayers are prioritized during this time.",
            CulturalEventType.Navaratri => "Navaratri evenings are reserved for devotional activities and prayers. Morning or afternoon hours are preferable.",
            _ => "Please consider the cultural and religious significance of this Hindu festival when scheduling."
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return HasConflict;
        yield return ConflictReason;
        yield return ConflictingEventType;
        yield return ConflictSeverity;
        yield return SuggestedAlternativeTime;
        yield return RecommendedStrategy;
    }
}

/// <summary>
/// Cultural conflict resolution strategies
/// </summary>
public enum CulturalResolutionStrategy
{
    NoAction,
    FlexibleRescheduling,
    RescheduleRecommended,
    MustReschedule,
    ManualResolutionRequired
}