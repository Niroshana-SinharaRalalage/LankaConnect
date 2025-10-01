using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for optimizing emails for diaspora communities across multiple time zones
/// </summary>
public class DiasporaEmailOptimizer
{
    private readonly IGeographicTimeZoneService _timezoneService;

    public DiasporaEmailOptimizer(IGeographicTimeZoneService timezoneService)
    {
        _timezoneService = timezoneService;
    }

    public Result<DiasporaOptimizationResult> OptimizeForDiaspora(EmailMessage email, DiasporaCommunityContext diasporaContext)
    {
        var optimalTime = _timezoneService.FindOptimalSendTimeForRegions(diasporaContext.TargetRegions.ToArray());
        
        var regionOptimizations = diasporaContext.TargetRegions.Select(region => 
            new RegionOptimization(region, _timezoneService.ConvertToRegionTime(optimalTime.OptimalSendTime, region))
        ).ToList();

        var result = new DiasporaOptimizationResult(
            optimalTime.OptimalSendTime,
            regionOptimizations,
            optimalTime.RegionConsiderations);

        return Result<DiasporaOptimizationResult>.Success(result);
    }
}

/// <summary>
/// Context for diaspora community targeting across multiple regions
/// </summary>
public record DiasporaCommunityContext(IReadOnlyList<GeographicRegion> TargetRegions);

/// <summary>
/// Optimization result for diaspora email targeting
/// </summary>
public record DiasporaOptimizationResult(
    DateTime OptimalSendTime,
    IReadOnlyList<RegionOptimization> RegionOptimizations,
    string[] CompromiseReasons);

/// <summary>
/// Regional optimization details
/// </summary>
public record RegionOptimization(GeographicRegion Region, DateTime LocalTime);