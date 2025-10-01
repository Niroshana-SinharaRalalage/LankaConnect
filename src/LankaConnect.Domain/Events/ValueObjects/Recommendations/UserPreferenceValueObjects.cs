using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects.Recommendations;

/// <summary>
/// User attendance history for pattern analysis
/// </summary>
public class AttendanceHistory : ValueObject
{
    public Guid UserId { get; }
    public AttendedEvent[] AttendedEvents { get; }
    public int TotalEventsAttended { get; }
    public double AverageRating { get; }
    public DateTime LastUpdated { get; }

    public AttendanceHistory(Guid userId, AttendedEvent[] attendedEvents, double averageRating)
    {
        UserId = userId;
        AttendedEvents = attendedEvents ?? Array.Empty<AttendedEvent>();
        TotalEventsAttended = AttendedEvents.Length;
        AverageRating = Math.Max(0.0, Math.Min(5.0, averageRating));
        LastUpdated = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        yield return TotalEventsAttended;
        yield return AverageRating;
    }
}

/// <summary>
/// Individual attended event record
/// </summary>
public class AttendedEvent : ValueObject
{
    public string Category { get; }
    public double Rating { get; }
    public int AttendanceFrequency { get; }
    public DateTime AttendedDate { get; }
    public TimeSpan Duration { get; }

    public AttendedEvent(string category, double rating, int attendanceFrequency, DateTime attendedDate = default, TimeSpan duration = default)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Rating = Math.Max(0.0, Math.Min(5.0, rating));
        AttendanceFrequency = Math.Max(0, attendanceFrequency);
        AttendedDate = attendedDate == default ? DateTime.UtcNow : attendedDate;
        Duration = duration == default ? TimeSpan.FromHours(2) : duration;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Category;
        yield return Rating;
        yield return AttendanceFrequency;
        yield return AttendedDate.Date;
    }
}

/// <summary>
/// Analyzed preference patterns from attendance history
/// </summary>
public class PreferencePatterns : ValueObject
{
    public string[] StrongPreferences { get; }
    public string[] WeakPreferences { get; }
    public double OptimalFrequency { get; }
    public double EngagementScore { get; }
    public Dictionary<string, double> CategoryWeights { get; }

    public PreferencePatterns(
        string[] strongPreferences, 
        string[] weakPreferences, 
        double optimalFrequency, 
        double engagementScore,
        Dictionary<string, double>? categoryWeights = null)
    {
        StrongPreferences = strongPreferences ?? Array.Empty<string>();
        WeakPreferences = weakPreferences ?? Array.Empty<string>();
        OptimalFrequency = Math.Max(0.0, optimalFrequency);
        EngagementScore = Math.Max(0.0, Math.Min(1.0, engagementScore));
        CategoryWeights = categoryWeights ?? new Dictionary<string, double>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", StrongPreferences.OrderBy(x => x));
        yield return string.Join(",", WeakPreferences.OrderBy(x => x));
        yield return OptimalFrequency;
        yield return EngagementScore;
    }
}

/// <summary>
/// Cultural category preferences with machine learning weights
/// </summary>
public class CulturalCategoryPreferences : ValueObject
{
    public Dictionary<string, double> PreferenceWeights { get; }
    public double LearningConfidence { get; }
    public DateTime LastUpdated { get; }
    public int SampleSize { get; }
    
    // Derived properties for backward compatibility
    public string[] PrimaryCategories => PreferenceWeights
        .Where(x => x.Value >= 0.7)
        .Select(x => x.Key)
        .ToArray();
    
    public string[] SecondaryCategories => PreferenceWeights
        .Where(x => x.Value >= 0.4 && x.Value < 0.7)
        .Select(x => x.Key)
        .ToArray();

    public CulturalCategoryPreferences(Dictionary<string, double> preferenceWeights, double learningConfidence, DateTime lastUpdated, int sampleSize = 0)
    {
        PreferenceWeights = preferenceWeights ?? new Dictionary<string, double>();
        LearningConfidence = Math.Max(0.0, Math.Min(1.0, learningConfidence));
        LastUpdated = lastUpdated;
        SampleSize = Math.Max(0, sampleSize);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", PreferenceWeights.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value:F3}"));
        yield return LearningConfidence;
        yield return LastUpdated.Date;
    }
}

/// <summary>
/// User interaction for machine learning
/// </summary>
public class UserInteraction : ValueObject
{
    public InteractionType Type { get; }
    public double InteractionStrength { get; }
    public DateTime Timestamp { get; }
    public string Context { get; }

    public UserInteraction(InteractionType type, double interactionStrength, string context = "")
    {
        Type = type;
        InteractionStrength = Math.Max(0.0, Math.Min(1.0, interactionStrength));
        Timestamp = DateTime.UtcNow;
        Context = context ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return InteractionStrength;
        yield return Context;
    }
}

/// <summary>
/// Interaction types for preference learning
/// </summary>
public enum InteractionType
{
    View = 0,
    Click = 1,
    Register = 2,
    Attend = 3,
    Rate = 4,
    Share = 5,
    Bookmark = 6,
    Skip = 7
}

/// <summary>
/// Time slot preferences for optimal scheduling
/// </summary>
public class TimeSlotPreferences : ValueObject
{
    public DayOfWeek[] PreferredDays { get; }
    public DayOfWeek[] AvoidedDays { get; }
    public TimeSlot[] PreferredStartTimes { get; }
    public double WorkingHoursAvoidance { get; }

    public TimeSlotPreferences(
        DayOfWeek[] preferredDays, 
        DayOfWeek[] avoidedDays, 
        TimeSlot[] preferredStartTimes, 
        double workingHoursAvoidance = 0.7)
    {
        PreferredDays = preferredDays ?? Array.Empty<DayOfWeek>();
        AvoidedDays = avoidedDays ?? Array.Empty<DayOfWeek>();
        PreferredStartTimes = preferredStartTimes ?? Array.Empty<TimeSlot>();
        WorkingHoursAvoidance = Math.Max(0.0, Math.Min(1.0, workingHoursAvoidance));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", PreferredDays.OrderBy(x => x));
        yield return string.Join(",", AvoidedDays.OrderBy(x => x));
        yield return WorkingHoursAvoidance;
    }
}

/// <summary>
/// Time slot with preference weight
/// </summary>
public class TimeSlot : ValueObject
{
    public TimeSpan Start { get; }
    public TimeSpan End { get; }
    public double Preference { get; }

    public TimeSlot(TimeSpan start, TimeSpan end, double preference)
    {
        if (end <= start)
            throw new ArgumentException("End time must be after start time");

        Start = start;
        End = end;
        Preference = Math.Max(0.0, Math.Min(1.0, preference));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
        yield return Preference;
    }
}

/// <summary>
/// Time compatibility score
/// </summary>
public class TimeCompatibilityScore : ValueObject
{
    public double Value { get; }
    public double Score => Value; // Alias for backward compatibility
    public string TimeReason { get; }

    public TimeCompatibilityScore(double value, string timeReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        TimeReason = timeReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return TimeReason;
    }
}

/// <summary>
/// Family profile for family-appropriate event recommendations
/// </summary>
public class FamilyProfile : ValueObject
{
    public bool HasChildren { get; }
    public int[] ChildrenAges { get; }
    public double FamilyEventPreference { get; }
    public double AdultOnlyEventPreference { get; }
    public double ChildFriendlyImportance { get; }

    public FamilyProfile(
        bool hasChildren, 
        int[]? childrenAges = null, 
        double familyEventPreference = 0.8, 
        double adultOnlyEventPreference = 0.3, 
        double childFriendlyImportance = 0.9)
    {
        HasChildren = hasChildren;
        ChildrenAges = childrenAges ?? Array.Empty<int>();
        FamilyEventPreference = Math.Max(0.0, Math.Min(1.0, familyEventPreference));
        AdultOnlyEventPreference = Math.Max(0.0, Math.Min(1.0, adultOnlyEventPreference));
        ChildFriendlyImportance = Math.Max(0.0, Math.Min(1.0, childFriendlyImportance));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return HasChildren;
        yield return string.Join(",", ChildrenAges.OrderBy(x => x));
        yield return FamilyEventPreference;
        yield return AdultOnlyEventPreference;
    }
}

/// <summary>
/// Family compatibility score
/// </summary>
public class FamilyCompatibilityScore : ValueObject
{
    public double Value { get; }
    public double Score => Value; // Alias for backward compatibility
    public string FamilyReason { get; }

    public FamilyCompatibilityScore(double value, string familyReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        FamilyReason = familyReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return FamilyReason;
    }
}

/// <summary>
/// Age group preferences for demographic matching
/// </summary>
public class AgeGroupPreferences : ValueObject
{
    public string[] PreferredAgeRanges { get; }
    public string ActivityEnergyLevel { get; }
    public double SocialInteractionPreference { get; }

    public AgeGroupPreferences(string[] preferredAgeRanges, string activityEnergyLevel, double socialInteractionPreference)
    {
        PreferredAgeRanges = preferredAgeRanges ?? Array.Empty<string>();
        ActivityEnergyLevel = activityEnergyLevel ?? "Moderate";
        SocialInteractionPreference = Math.Max(0.0, Math.Min(1.0, socialInteractionPreference));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", PreferredAgeRanges.OrderBy(x => x));
        yield return ActivityEnergyLevel;
        yield return SocialInteractionPreference;
    }
}

/// <summary>
/// Age compatibility score
/// </summary>
public class AgeCompatibilityScore : ValueObject
{
    public double Value { get; }
    public double Score => Value; // Alias for backward compatibility
    public string AgeReason { get; }

    public AgeCompatibilityScore(double value, string ageReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        AgeReason = ageReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return AgeReason;
    }
}

/// <summary>
/// Language preferences supporting multilingual Sri Lankan community
/// </summary>
public class LanguagePreferences : ValueObject
{
    public string[] PrimaryLanguages { get; }
    public string[] SecondaryLanguages { get; }
    public double MultilingualPreference { get; }
    public bool RequiresTranslation { get; }

    public LanguagePreferences(
        string[] primaryLanguages, 
        string[] secondaryLanguages, 
        double multilingualPreference = 0.8, 
        bool requiresTranslation = false)
    {
        PrimaryLanguages = primaryLanguages ?? Array.Empty<string>();
        SecondaryLanguages = secondaryLanguages ?? Array.Empty<string>();
        MultilingualPreference = Math.Max(0.0, Math.Min(1.0, multilingualPreference));
        RequiresTranslation = requiresTranslation;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", PrimaryLanguages.OrderBy(x => x));
        yield return string.Join(",", SecondaryLanguages.OrderBy(x => x));
        yield return MultilingualPreference;
        yield return RequiresTranslation;
    }
}

/// <summary>
/// Language compatibility score
/// </summary>
public class LanguageCompatibilityScore : ValueObject
{
    public double Value { get; }
    public double Score => Value; // Alias for backward compatibility
    public string LanguageReason { get; }

    public LanguageCompatibilityScore(double value, string languageReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        LanguageReason = languageReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return LanguageReason;
    }
}

/// <summary>
/// Community involvement profile for engagement matching
/// </summary>
public class CommunityInvolvementProfile : ValueObject
{
    public InvolvementLevel InvolvementLevel { get; }
    public int VolunteerHours { get; }
    public int LeadershipRoles { get; }
    public int MembershipCount { get; }
    public CommitmentLevel CommitmentCapacity { get; }
    public string[] PreferredInvolvementTypes { get; }

    public CommunityInvolvementProfile(
        InvolvementLevel involvementLevel, 
        int volunteerHours, 
        int leadershipRoles, 
        int membershipCount, 
        CommitmentLevel commitmentCapacity,
        string[]? preferredInvolvementTypes = null)
    {
        InvolvementLevel = involvementLevel;
        VolunteerHours = Math.Max(0, volunteerHours);
        LeadershipRoles = Math.Max(0, leadershipRoles);
        MembershipCount = Math.Max(0, membershipCount);
        CommitmentCapacity = commitmentCapacity;
        PreferredInvolvementTypes = preferredInvolvementTypes ?? Array.Empty<string>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return InvolvementLevel;
        yield return VolunteerHours;
        yield return LeadershipRoles;
        yield return MembershipCount;
        yield return CommitmentCapacity;
    }
}

/// <summary>
/// Involvement levels
/// </summary>
public enum InvolvementLevel
{
    Observer = 0,
    Casual = 1,
    Regular = 2,
    Active = 3,
    Leader = 4
}

/// <summary>
/// Commitment levels
/// </summary>
public enum CommitmentLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    VeryHigh = 3
}

/// <summary>
/// Involvement compatibility score
/// </summary>
public class InvolvementCompatibilityScore : ValueObject
{
    public double Value { get; }
    public double Score => Value; // Alias for backward compatibility
    public string InvolvementReason { get; }

    public InvolvementCompatibilityScore(double value, string involvementReason = "")
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
        InvolvementReason = involvementReason ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return InvolvementReason;
    }
}