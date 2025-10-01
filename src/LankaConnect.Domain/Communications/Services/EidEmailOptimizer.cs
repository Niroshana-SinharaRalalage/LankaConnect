using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for optimizing emails during Eid celebrations
/// </summary>
public class EidEmailOptimizer
{
    private readonly IReligiousObservanceService _religiousService;

    public EidEmailOptimizer(IReligiousObservanceService religiousService)
    {
        _religiousService = religiousService;
    }

    public Result<EidOptimizationResult> OptimizeForEid(EmailMessage email, DateTime eidDate, GeographicRegion region)
    {
        var celebrationTimes = _religiousService.GetIslamicCelebrationTimes(eidDate);
        var eidPrayerTime = _religiousService.GetEidPrayerTime(eidDate, region);
        
        var optimizedTimes = new[]
        {
            eidDate.Add(celebrationTimes.EidPrayer),
            eidDate.Add(celebrationTimes.CommunityGathering),
            eidDate.Add(celebrationTimes.FamilyTime)
        };

        var avoidedTimes = new[] { eidDate.Add(eidPrayerTime) };

        var result = new EidOptimizationResult(
            optimizedTimes,
            optimizedTimes[1], // Recommend community gathering time
            new IslamicContextInfo("Eid al-Fitr celebration"),
            avoidedTimes);

        return Result<EidOptimizationResult>.Success(result);
    }
}

/// <summary>
/// Eid optimization result
/// </summary>
public record EidOptimizationResult(
    DateTime[] CelebrationTimes,
    DateTime RecommendedSendTime,
    IslamicContextInfo IslamicContext,
    DateTime[] AvoidedTimes);

/// <summary>
/// Islamic context information
/// </summary>
public record IslamicContextInfo(string Description);