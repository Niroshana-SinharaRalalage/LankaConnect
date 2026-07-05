using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.SharedKernel.Cultural;
namespace LankaConnect.Modules.Communications.Domain.Services;

/// <summary>
/// Domain service for cultural optimization of email timing and content
/// </summary>
public class EmailCulturalOptimizer
{
    private readonly ICulturalCalendarService _culturalCalendar;
    private readonly IGeographicTimeZoneService _timezoneService;
    private readonly IReligiousObservanceService _religiousService;

    public EmailCulturalOptimizer(
        ICulturalCalendarService culturalCalendar,
        IGeographicTimeZoneService timezoneService,
        IReligiousObservanceService religiousService)
    {
        _culturalCalendar = culturalCalendar;
        _timezoneService = timezoneService;
        _religiousService = religiousService;
    }

    public Result<CulturalOptimizationResult> OptimizeSendingTime(EmailMessage email, CulturalEmailContext context, DateTime? requestedTime = null)
    {
        var targetTime = requestedTime ?? DateTime.UtcNow;
        var culturalContextResult = CulturalContext.Create(
            context.PreferredLanguage,
            CulturalBackground.SinhalaBuddhist, // Map from context.PrimaryCulture
            context.TargetRegion);

        if (culturalContextResult.IsFailure)
            return Result<CulturalOptimizationResult>.Failure(culturalContextResult.Error.Message);

        var optimizedTiming = _culturalCalendar.GetOptimalSendTime(targetTime, culturalContextResult.Value);

        var result = new CulturalOptimizationResult(
            optimizedTiming.OptimalSendTime,
            optimizedTiming.Reason,
            optimizedTiming.ReligiousContext,
            optimizedTiming.AlternativeTimeSlots,
            GetAuspiciousTimingReason(optimizedTiming),
            GetAvoidanceReason(optimizedTiming));

        return Result<CulturalOptimizationResult>.Success(result);
    }

    private string GetAuspiciousTimingReason(CulturalTimingPreference timing)
    {
        return timing.ReligiousContext switch
        {
            LankaConnect.SharedKernel.Cultural.ReligiousContext.BuddhistPoyaday => "Brahma Muhurta",
            LankaConnect.SharedKernel.Cultural.ReligiousContext.HinduFestival => "Brahma Muhurta",
            _ => "Optimal community engagement time"
        };
    }

    private string GetAvoidanceReason(CulturalTimingPreference timing)
    {
        return timing.ReligiousContext switch
        {
            LankaConnect.SharedKernel.Cultural.ReligiousContext.ChristianSabbath => "Church service time",
            LankaConnect.SharedKernel.Cultural.ReligiousContext.BuddhistPoyaday => "Poyaday observance",
            LankaConnect.SharedKernel.Cultural.ReligiousContext.Ramadan => "Fasting hours",
            _ => "Cultural consideration"
        };
    }
}


/// <summary>
/// Result of cultural optimization with detailed explanations
/// </summary>
public record CulturalOptimizationResult(
    DateTime OptimizedSendTime,
    string CulturalDelayReason,
    LankaConnect.SharedKernel.Cultural.ReligiousContext ReligiousContext,
    IReadOnlyList<DateTime> AlternativeTimeSlots,
    string AuspiciousTimingReason,
    string AvoidanceReason);