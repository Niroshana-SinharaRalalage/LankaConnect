using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Cultural timing preferences for optimal email delivery based on religious and cultural considerations
/// </summary>
public class CulturalTimingPreference : ValueObject
{
    public DateTime OptimalSendTime { get; }
    public string Reason { get; }
    public ReligiousContext ReligiousContext { get; }
    public GeographicRegion TargetRegion { get; }
    public IReadOnlyList<DateTime> AlternativeTimeSlots { get; }
    public bool IsCulturallyOptimized { get; }
    public TimeSpan PreferredTimeOfDay { get; }

    private CulturalTimingPreference(
        DateTime optimalSendTime,
        string reason,
        ReligiousContext religiousContext,
        GeographicRegion targetRegion,
        IReadOnlyList<DateTime> alternativeTimeSlots,
        bool isCulturallyOptimized,
        TimeSpan preferredTimeOfDay)
    {
        OptimalSendTime = optimalSendTime;
        Reason = reason;
        ReligiousContext = religiousContext;
        TargetRegion = targetRegion;
        AlternativeTimeSlots = alternativeTimeSlots;
        IsCulturallyOptimized = isCulturallyOptimized;
        PreferredTimeOfDay = preferredTimeOfDay;
    }

    public static Result<CulturalTimingPreference> Create(
        DateTime optimalSendTime,
        string reason,
        ReligiousContext religiousContext = ReligiousContext.None,
        GeographicRegion targetRegion = GeographicRegion.SriLanka,
        IReadOnlyList<DateTime>? alternativeTimeSlots = null,
        bool isCulturallyOptimized = false,
        TimeSpan? preferredTimeOfDay = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result<CulturalTimingPreference>.Failure("Timing reason is required");

        var alternatives = alternativeTimeSlots ?? Array.Empty<DateTime>();
        var preferredTime = preferredTimeOfDay ?? optimalSendTime.TimeOfDay;

        return Result<CulturalTimingPreference>.Success(new CulturalTimingPreference(
            optimalSendTime,
            reason,
            religiousContext,
            targetRegion,
            alternatives,
            isCulturallyOptimized,
            preferredTime));
    }

    public static CulturalTimingPreference CreateForPoyadayDelay(DateTime nextValidTime, GeographicRegion region)
    {
        var alternatives = new[]
        {
            nextValidTime.AddHours(6), // Morning option
            nextValidTime.AddHours(12), // Afternoon option
            nextValidTime.AddHours(18) // Evening option
        };

        return new CulturalTimingPreference(
            nextValidTime,
            "Delayed due to Poyaday observance in Buddhist community",
            ReligiousContext.BuddhistPoyaday,
            region,
            alternatives,
            true,
            TimeSpan.FromHours(8) // Prefer 8 AM
        );
    }

    public static CulturalTimingPreference CreateForRamadanOptimization(DateTime iftarTime, GeographicRegion region)
    {
        var alternatives = new[]
        {
            iftarTime.AddHours(1), // After Iftar
            iftarTime.AddHours(-8), // Before Fajr
            iftarTime.AddHours(12) // Next day morning
        };

        return new CulturalTimingPreference(
            iftarTime.AddMinutes(30), // 30 minutes after Iftar
            "Optimized for Ramadan Iftar timing",
            ReligiousContext.Ramadan,
            region,
            alternatives,
            true,
            TimeSpan.FromHours(19) // Prefer evening
        );
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return OptimalSendTime;
        yield return Reason;
        yield return ReligiousContext;
        yield return TargetRegion;
        yield return IsCulturallyOptimized;
        yield return PreferredTimeOfDay;
        
        foreach (var slot in AlternativeTimeSlots)
            yield return slot;
    }
}