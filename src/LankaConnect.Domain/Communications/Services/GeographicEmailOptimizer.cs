using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for geographic optimization of email timing based on business hours and local preferences
/// </summary>
public class GeographicEmailOptimizer
{
    private readonly IGeographicTimeZoneService _timezoneService;

    public GeographicEmailOptimizer(IGeographicTimeZoneService timezoneService)
    {
        _timezoneService = timezoneService;
    }

    public Result<GeographicOptimizationResult> OptimizeForBusinessHours(EmailMessage email, GeographicRegion region)
    {
        var businessHours = _timezoneService.GetBusinessHours(region);
        var currentTime = DateTime.UtcNow;
        var regionTime = _timezoneService.ConvertToRegionTime(currentTime, region);
        
        var optimizedTime = GetOptimalBusinessTime(regionTime, businessHours);
        var utcOptimizedTime = _timezoneService.ConvertToRegionTime(optimizedTime, GeographicRegion.SriLanka); // Convert back to UTC-like

        var result = new GeographicOptimizationResult(utcOptimizedTime);
        
        return Result<GeographicOptimizationResult>.Success(result);
    }

    private DateTime GetOptimalBusinessTime(DateTime regionTime, BusinessHours businessHours)
    {
        var date = regionTime.Date;
        
        // If it's a weekend, move to Monday
        while (date.DayOfWeek < businessHours.WeekStart || date.DayOfWeek > businessHours.WeekEnd)
        {
            date = date.AddDays(1);
        }
        
        // Set to business start time
        return date.Add(businessHours.StartTime);
    }
}

/// <summary>
/// Result of geographic optimization
/// </summary>
public record GeographicOptimizationResult(DateTime OptimizedSendTime);