using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for optimizing emails during Christmas celebrations
/// </summary>
public class ChristmasEmailOptimizer
{
    private readonly IReligiousObservanceService _religiousService;

    public ChristmasEmailOptimizer(IReligiousObservanceService religiousService)
    {
        _religiousService = religiousService;
    }

    public Result<ChristmasOptimizationResult> OptimizeForChristmas(EmailMessage email, DateTime christmasDate, GeographicRegion region)
    {
        var serviceTimes = _religiousService.GetChristmasServiceTimes(christmasDate, region);
        
        var optimizedTimes = new[]
        {
            christmasDate.Add(serviceTimes.MidnightMass),
            christmasDate.Add(serviceTimes.MorningService),
            christmasDate.Add(serviceTimes.EveningService)
        };

        // Recommend time that avoids service times
        var recommendedTime = christmasDate.AddHours(14); // 2 PM, between services

        var result = new ChristmasOptimizationResult(
            optimizedTimes,
            recommendedTime,
            new ChristianContextInfo("Christmas Day celebration"));

        return Result<ChristmasOptimizationResult>.Success(result);
    }
}

/// <summary>
/// Christmas optimization result
/// </summary>
public record ChristmasOptimizationResult(
    DateTime[] ServiceTimes,
    DateTime RecommendedSendTime,
    ChristianContextInfo ChristianContext);

/// <summary>
/// Christian context information
/// </summary>
public record ChristianContextInfo(string Description);