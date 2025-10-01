using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for optimizing emails during Vesak Day celebrations
/// </summary>
public class VesakDayEmailOptimizer
{
    private readonly ICulturalCalendarService _culturalCalendar;

    public VesakDayEmailOptimizer(ICulturalCalendarService culturalCalendar)
    {
        _culturalCalendar = culturalCalendar;
    }

    public Result<VesakOptimizationResult> OptimizeForVesak(EmailMessage email, DateTime vesakDate, GeographicRegion region)
    {
        var celebrationTimes = _culturalCalendar.GetVesakCelebrationTimes(vesakDate, region);
        
        var optimizedTimes = new[]
        {
            vesakDate.Add(celebrationTimes.PreDawnCeremony),
            vesakDate.Add(celebrationTimes.MainCelebration),
            vesakDate.Add(celebrationTimes.EveningDhamma)
        };

        var result = new VesakOptimizationResult(
            optimizedTimes,
            optimizedTimes[0], // Recommend pre-dawn ceremony time
            BuddhistFestival.Vesak);

        return Result<VesakOptimizationResult>.Success(result);
    }
}

/// <summary>
/// Vesak Day optimization result
/// </summary>
public record VesakOptimizationResult(
    DateTime[] OptimizedTimes,
    DateTime RecommendedSendTime,
    BuddhistFestival FestivalContext);