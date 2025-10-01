using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service interface for geographic time zone optimization and diaspora community targeting
/// </summary>
public interface IGeographicTimeZoneService
{
    OptimalTimeResult FindOptimalSendTimeForRegions(GeographicRegion[] regions);
    BusinessHours GetBusinessHours(GeographicRegion region);
    TimeZoneInfo GetTimeZoneForRegion(GeographicRegion region);
    DateTime ConvertToRegionTime(DateTime utcDateTime, GeographicRegion region);
    
    // Async methods for time zone conversion
    Task<DateTime> ConvertToTimeZoneAsync(DateTime dateTime, string timeZone);
    Task<DateTime> ConvertFromTimeZoneAsync(DateTime dateTime, string timeZone);
    Task<OptimalTimeResult> FindOptimalSendTimeForRegionsAsync(GeographicRegion[] regions);
}

/// <summary>
/// Optimal send time result for multi-region targeting
/// </summary>
public record OptimalTimeResult(
    DateTime OptimalSendTime,
    string[] RegionConsiderations);

/// <summary>
/// Business hours for a specific geographic region
/// </summary>
public record BusinessHours(
    TimeSpan StartTime,
    TimeSpan EndTime,
    DayOfWeek WeekStart,
    DayOfWeek WeekEnd);