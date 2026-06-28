using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects.Recommendations;

/// <summary>
/// Core Event Recommendation value object containing event and computed recommendation score
/// </summary>
public class EventRecommendation : ValueObject
{
    public Event Event { get; }
    public RecommendationScore Score { get; }
    public DateTime GeneratedAt { get; }
    public string RecommendationReason { get; }
    public double Confidence { get; }

    public EventRecommendation(Event @event, RecommendationScore score, string reason, double confidence = 1.0)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        Score = score ?? throw new ArgumentNullException(nameof(score));
        RecommendationReason = reason ?? throw new ArgumentNullException(nameof(reason));
        Confidence = Math.Max(0.0, Math.Min(1.0, confidence));
        GeneratedAt = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Event;
        yield return Score;
        yield return RecommendationReason;
        yield return Confidence;
    }
}

/// <summary>
/// Base recommendation score containing all scoring dimensions
/// </summary>
public class RecommendationScore : ValueObject
{
    public double CompositeScore { get; }
    public double CulturalScore { get; }
    public double GeographicScore { get; }
    public double HistoryScore { get; }
    public double TimeScore { get; }
    public double LanguageScore { get; }
    public double FamilyScore { get; }
    public double InvolvementScore { get; }
    public double AccessibilityScore { get; }
    public double RegionalScore { get; }
    public double DistanceScore { get; }
    public double CategoryScore { get; }
    public double TimingScore { get; }
    public double LocationScore { get; }
    public double ProximityScore { get; }

    public RecommendationScore(
        double compositeScore,
        double culturalScore = 0.0,
        double geographicScore = 0.0,
        double historyScore = 0.0,
        double timeScore = 0.0,
        double languageScore = 0.0,
        double familyScore = 0.0,
        double involvementScore = 0.0,
        double accessibilityScore = 0.0,
        double regionalScore = 0.0,
        double distanceScore = 0.0,
        double categoryScore = 0.0,
        double timingScore = 0.0,
        double locationScore = 0.0,
        double proximityScore = 0.0)
    {
        CompositeScore = ValidateScore(compositeScore, nameof(compositeScore));
        CulturalScore = ValidateScore(culturalScore, nameof(culturalScore));
        GeographicScore = ValidateScore(geographicScore, nameof(geographicScore));
        HistoryScore = ValidateScore(historyScore, nameof(historyScore));
        TimeScore = ValidateScore(timeScore, nameof(timeScore));
        LanguageScore = ValidateScore(languageScore, nameof(languageScore));
        FamilyScore = ValidateScore(familyScore, nameof(familyScore));
        InvolvementScore = ValidateScore(involvementScore, nameof(involvementScore));
        AccessibilityScore = ValidateScore(accessibilityScore, nameof(accessibilityScore));
        RegionalScore = ValidateScore(regionalScore, nameof(regionalScore));
        DistanceScore = ValidateScore(distanceScore, nameof(distanceScore));
        CategoryScore = ValidateScore(categoryScore, nameof(categoryScore));
        TimingScore = ValidateScore(timingScore, nameof(timingScore));
        LocationScore = ValidateScore(locationScore, nameof(locationScore));
        ProximityScore = ValidateScore(proximityScore, nameof(proximityScore));
    }

    private static double ValidateScore(double score, string paramName)
    {
        if (double.IsNaN(score) || double.IsInfinity(score))
            throw new ArgumentException($"Score cannot be NaN or Infinity", paramName);
        
        return Math.Max(0.0, Math.Min(1.0, score));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CompositeScore;
        yield return CulturalScore;
        yield return GeographicScore;
        yield return HistoryScore;
        yield return TimeScore;
        yield return LanguageScore;
        yield return FamilyScore;
        yield return InvolvementScore;
    }
}

/// <summary>
/// Cultural appropriateness score for cultural intelligence
/// </summary>
public class CulturalScore : ValueObject
{
    public double Value { get; }
    public CulturalAppropriatenessLevel Level { get; }

    public CulturalScore(double value)
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        Level = value switch
        {
            >= 0.8 => CulturalAppropriatenessLevel.HighlyAppropriate,
            >= 0.6 => CulturalAppropriatenessLevel.Appropriate,
            >= 0.4 => CulturalAppropriatenessLevel.Neutral,
            >= 0.2 => CulturalAppropriatenessLevel.Questionable,
            _ => CulturalAppropriatenessLevel.Inappropriate
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Level;
    }
}

/// <summary>
/// Cultural appropriateness level enumeration
/// </summary>
public enum CulturalAppropriatenessLevel
{
    Inappropriate = 0,
    Questionable = 1,
    Neutral = 2,
    Appropriate = 3,
    HighlyAppropriate = 4
}

/// <summary>
/// Cultural sensitivity levels
/// </summary>
public enum CulturalSensitivityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    VeryHigh = 3
}

/// <summary>
/// Diaspora adaptation preferences
/// </summary>
public enum DiasporaAdaptationLevel
{
    Traditional = 0,
    Conservative = 1,
    Moderate = 2,
    Adaptive = 3,
    FullyIntegrated = 4
}

/// <summary>
/// Diaspora friendliness levels
/// </summary>
public enum DiasporaFriendliness
{
    Traditional = 0,
    Moderate = 1,
    High = 2,
    VeryHigh = 3
}

/// <summary>
/// Event nature classification
/// </summary>
public enum EventNature
{
    Religious = 0,
    Cultural = 1,
    Secular = 2,
    Mixed = 3
}

/// <summary>
/// User cultural preferences
/// </summary>
public class CulturalPreferences : ValueObject
{
    public string[] PreferredEventTypes { get; }
    public string[] DislikedEventTypes { get; }
    public double CulturalSignificanceWeight { get; }

    public CulturalPreferences(string[] preferredEventTypes, string[] dislikedEventTypes, double culturalSignificanceWeight = 0.8)
    {
        PreferredEventTypes = preferredEventTypes ?? Array.Empty<string>();
        DislikedEventTypes = dislikedEventTypes ?? Array.Empty<string>();
        CulturalSignificanceWeight = Math.Max(0.0, Math.Min(1.0, culturalSignificanceWeight));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", PreferredEventTypes.OrderBy(x => x));
        yield return string.Join(",", DislikedEventTypes.OrderBy(x => x));
        yield return CulturalSignificanceWeight;
    }
}

/// <summary>
/// Festival period for timing optimization
/// </summary>
public class FestivalPeriod : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string Name { get; }
    public TimeSpan Duration => EndDate - StartDate;

    public FestivalPeriod(DateTime startDate, DateTime endDate, string name = "")
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");

        StartDate = startDate;
        EndDate = endDate;
        Name = name ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
        yield return Name;
    }
}

/// <summary>
/// Significant cultural dates
/// </summary>
public class SignificantDate : ValueObject
{
    public string Name { get; }
    public DateTime Date { get; }
    public SignificanceLevel Level { get; }

    public SignificantDate(string name, DateTime date, SignificanceLevel level)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Date = date;
        Level = level;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Date;
        yield return Level;
    }
}

/// <summary>
/// Cultural significance levels
/// </summary>
public enum SignificanceLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Calendar validation result
/// </summary>
public class CalendarValidationResult : ValueObject
{
    public bool IsValid { get; }
    public string Reason { get; }
    public string[] Suggestions { get; }

    public CalendarValidationResult(bool isValid, string reason, string[]? suggestions = null)
    {
        IsValid = isValid;
        Reason = reason ?? string.Empty;
        Suggestions = suggestions ?? Array.Empty<string>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsValid;
        yield return Reason;
        yield return string.Join(",", Suggestions.OrderBy(x => x));
    }
}

/// <summary>
/// Event nature preferences with scoring weights
/// </summary>
public class EventNaturePreferences : ValueObject
{
    public double Religious { get; }
    public double Cultural { get; }
    public double Secular { get; }

    public EventNaturePreferences(double religious = 0.8, double cultural = 0.7, double secular = 0.5)
    {
        Religious = Math.Max(0.0, Math.Min(1.0, religious));
        Cultural = Math.Max(0.0, Math.Min(1.0, cultural));
        Secular = Math.Max(0.0, Math.Min(1.0, secular));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Religious;
        yield return Cultural;
        yield return Secular;
    }
}