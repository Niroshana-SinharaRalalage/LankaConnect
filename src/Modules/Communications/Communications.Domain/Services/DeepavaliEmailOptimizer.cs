using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.Modules.Communications.Domain.Services;

/// <summary>
/// Domain service for optimizing emails during Deepavali celebrations
/// </summary>
public class DeepavaliEmailOptimizer
{
    private readonly ICulturalCalendarService _culturalCalendar;

    public DeepavaliEmailOptimizer(ICulturalCalendarService culturalCalendar)
    {
        _culturalCalendar = culturalCalendar;
    }

    public Result<DeepavaliOptimizationResult> OptimizeForDeepavali(EmailMessage email, DateTime deepavaliDate, GeographicRegion region)
    {
        var auspiciousTimes = _culturalCalendar.GetHinduAuspiciousTimes(deepavaliDate, HinduFestival.Deepavali);
        
        var optimizedTimes = new[]
        {
            deepavaliDate.Add(auspiciousTimes.BrahmaMuhurta),
            deepavaliDate.Add(auspiciousTimes.LakshmiPuja),
            deepavaliDate.Add(auspiciousTimes.DiyaLighting)
        };

        var result = new DeepavaliOptimizationResult(
            optimizedTimes,
            optimizedTimes[0], // Recommend Brahma Muhurta
            new HinduContextInfo("Deepavali celebration with Lakshmi Puja"));

        return Result<DeepavaliOptimizationResult>.Success(result);
    }
}

/// <summary>
/// Deepavali optimization result
/// </summary>
public record DeepavaliOptimizationResult(
    DateTime[] AuspiciousTimes,
    DateTime RecommendedSendTime,
    HinduContextInfo HinduContext);

/// <summary>
/// Hindu context information
/// </summary>
public record HinduContextInfo(string Description);