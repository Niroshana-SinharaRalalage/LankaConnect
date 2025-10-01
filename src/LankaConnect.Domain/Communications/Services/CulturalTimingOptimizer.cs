using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Cultural Timing Optimizer - Buddhist/Hindu Calendar Integration Service
/// Provides sophisticated religious observance awareness, Poyaday calculation,
/// and culturally appropriate timing optimization for WhatsApp messaging
/// </summary>
public class CulturalTimingOptimizer : ICulturalTimingOptimizer
{
    private readonly ICulturalCalendarService _culturalCalendarService;
    private readonly IGeographicTimeZoneService _timeZoneService;
    private readonly IReligiousObservanceService _observanceService;

    // Buddhist observance time windows (in local time)
    private static readonly Dictionary<string, (int StartHour, int EndHour)> _buddhistQuietHours = new()
    {
        { "meditation_morning", (5, 7) },      // Early morning meditation
        { "meditation_evening", (18, 21) },    // Evening meditation and chanting
        { "poyaday_quiet", (19, 22) }          // Poyaday evening quiet period
    };

    // Hindu observance time windows
    private static readonly Dictionary<string, (int StartHour, int EndHour)> _hinduObservanceHours = new()
    {
        { "morning_prayers", (5, 8) },         // Sandhya Vandana morning
        { "evening_prayers", (17, 19) },       // Sandhya Vandana evening
        { "festival_puja", (18, 22) }          // Festival puja times
    };

    public CulturalTimingOptimizer(
        ICulturalCalendarService culturalCalendarService,
        IGeographicTimeZoneService timeZoneService,
        IReligiousObservanceService observanceService)
    {
        _culturalCalendarService = culturalCalendarService ?? throw new ArgumentNullException(nameof(culturalCalendarService));
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _observanceService = observanceService ?? throw new ArgumentNullException(nameof(observanceService));
    }

    public async Task<Result<bool>> IsBuddhistObservanceTimeAsync(DateTime dateTime, string timeZone)
    {
        try
        {
            var localTime = await _timeZoneService.ConvertToTimeZoneAsync(dateTime, timeZone);
            
            // Check if it's a Poyaday (Buddhist observance day)
            var isPoyaDay = await _culturalCalendarService.IsBuddhistObservanceDayAsync(localTime.Date);
            
            if (isPoyaDay)
            {
                // On Poyadays, evening hours are particularly sacred
                var isQuietHours = IsWithinTimeWindow(localTime.Hour, _buddhistQuietHours["poyaday_quiet"]);
                if (isQuietHours)
                    return Result<bool>.Success(true);
            }

            // Check regular meditation hours
            var isMorningMeditation = IsWithinTimeWindow(localTime.Hour, _buddhistQuietHours["meditation_morning"]);
            var isEveningMeditation = IsWithinTimeWindow(localTime.Hour, _buddhistQuietHours["meditation_evening"]);

            // Check for special Buddhist calendar events
            var isSpecialObservanceDay = await _culturalCalendarService.IsSpecialBuddhistDayAsync(localTime.Date);
            
            var isObservanceTime = isPoyaDay || isMorningMeditation || isEveningMeditation || isSpecialObservanceDay;
            
            return Result<bool>.Success(isObservanceTime);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check Buddhist observance time: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsHinduObservanceTimeAsync(DateTime dateTime, string timeZone)
    {
        try
        {
            var localTime = await _timeZoneService.ConvertToTimeZoneAsync(dateTime, timeZone);
            
            // Check for Hindu prayer times (Sandhya Vandana)
            var isMorningPrayer = IsWithinTimeWindow(localTime.Hour, _hinduObservanceHours["morning_prayers"]);
            var isEveningPrayer = IsWithinTimeWindow(localTime.Hour, _hinduObservanceHours["evening_prayers"]);

            // Check for Hindu fasting days
            var isFastingDay = await _culturalCalendarService.IsHinduFastingDayAsync(localTime.Date);
            
            // Check for major Hindu festivals requiring special timing
            var isFestivalDay = await _culturalCalendarService.IsHinduFestivalDayAsync(localTime.Date);
            
            if (isFestivalDay)
            {
                // During festivals like Deepavali, evening is actually preferred for celebrations
                var isFestivalPujaTime = IsWithinTimeWindow(localTime.Hour, _hinduObservanceHours["festival_puja"]);
                // Festival puja time is NOT restrictive for celebration messages
                if (isFestivalPujaTime)
                    return Result<bool>.Success(false); // Allow messages during festival celebrations
            }

            var isObservanceTime = isMorningPrayer || isEveningPrayer || (isFastingDay && !isFestivalDay);
            
            return Result<bool>.Success(isObservanceTime);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check Hindu observance time: {ex.Message}");
        }
    }

    public async Task<Result<DateTime>> FindNextOptimalDeliveryTimeAsync(
        DateTime requestedTime, 
        WhatsAppCulturalContext culturalContext, 
        string timeZone)
    {
        try
        {
            var localTime = await _timeZoneService.ConvertToTimeZoneAsync(requestedTime, timeZone);
            var optimalLocalTime = localTime;

            // Buddhist context optimization
            if (culturalContext.RequiresBuddhistCalendarAwareness)
            {
                optimalLocalTime = await FindBuddhistOptimalTime(localTime, timeZone);
            }
            // Hindu context optimization  
            else if (culturalContext.RequiresHinduCalendarAwareness)
            {
                optimalLocalTime = await FindHinduOptimalTime(localTime, timeZone, culturalContext);
            }
            // General cultural optimization
            else
            {
                optimalLocalTime = await FindGeneralOptimalTime(localTime);
            }

            // Ensure the time is not in the past
            var currentLocalTime = await _timeZoneService.ConvertToTimeZoneAsync(DateTime.UtcNow, timeZone);
            if (optimalLocalTime <= currentLocalTime)
            {
                optimalLocalTime = currentLocalTime.AddHours(1); // Add buffer
                optimalLocalTime = await EnsureOptimalHours(optimalLocalTime, culturalContext);
            }

            // Convert back to UTC
            var optimalUtcTime = await _timeZoneService.ConvertFromTimeZoneAsync(optimalLocalTime, timeZone);
            
            return Result<DateTime>.Success(optimalUtcTime);
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Failure($"Failed to find optimal delivery time: {ex.Message}");
        }
    }

    public async Task<Result<TimeWindow>> GetFestivalGreetingWindowAsync(
        string festivalName, 
        DateTime festivalDate, 
        string timeZone)
    {
        try
        {
            var localFestivalDate = await _timeZoneService.ConvertToTimeZoneAsync(festivalDate, timeZone);
            var timeWindow = festivalName.ToLower() switch
            {
                "vesak" => await GetVesakGreetingWindow(localFestivalDate, timeZone),
                "deepavali" => await GetDeepavaliGreetingWindow(localFestivalDate, timeZone),
                "poson" => await GetPosonGreetingWindow(localFestivalDate, timeZone),
                "thai pusam" => await GetThaiPusamGreetingWindow(localFestivalDate, timeZone),
                _ => await GetDefaultFestivalWindow(localFestivalDate, timeZone)
            };

            return Result<TimeWindow>.Success(timeWindow);
        }
        catch (Exception ex)
        {
            return Result<TimeWindow>.Failure($"Failed to get festival greeting window: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ValidateMessageTimingSensitivityAsync(
        DateTime messageTime, 
        WhatsAppCulturalContext culturalContext, 
        string timeZone)
    {
        try
        {
            var localTime = await _timeZoneService.ConvertToTimeZoneAsync(messageTime, timeZone);
            
            // Check Buddhist timing sensitivity
            if (culturalContext.RequiresBuddhistCalendarAwareness)
            {
                var buddhistSensitivity = await CheckBuddhistTimingSensitivity(localTime, culturalContext);
                if (!buddhistSensitivity)
                    return Result<bool>.Success(false);
            }

            // Check Hindu timing sensitivity
            if (culturalContext.RequiresHinduCalendarAwareness)
            {
                var hinduSensitivity = await CheckHinduTimingSensitivity(localTime, culturalContext);
                if (!hinduSensitivity)
                    return Result<bool>.Success(false);
            }

            // Check general cultural timing appropriateness
            var generalSensitivity = await CheckGeneralTimingSensitivity(localTime);
            
            return Result<bool>.Success(generalSensitivity);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to validate timing sensitivity: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private static bool IsWithinTimeWindow(int hour, (int StartHour, int EndHour) window)
    {
        return hour >= window.StartHour && hour <= window.EndHour;
    }

    private async Task<DateTime> FindBuddhistOptimalTime(DateTime localTime, string timeZone)
    {
        // Check if current time conflicts with Buddhist observances
        var conflictsWithObservance = await IsBuddhistObservanceTimeAsync(
            await _timeZoneService.ConvertFromTimeZoneAsync(localTime, timeZone), 
            timeZone);

        if (conflictsWithObservance.IsSuccess && conflictsWithObservance.Value)
        {
            // Suggest morning time (9 AM - 11 AM) for Buddhist messages
            var optimalTime = localTime.Date.AddHours(10); // 10 AM
            
            // If it's already past noon, suggest next day
            if (localTime.Hour >= 12)
                optimalTime = optimalTime.AddDays(1);
                
            return optimalTime;
        }

        return localTime;
    }

    private async Task<DateTime> FindHinduOptimalTime(DateTime localTime, string timeZone, WhatsAppCulturalContext culturalContext)
    {
        // Check if current time conflicts with Hindu observances
        var conflictsWithObservance = await IsHinduObservanceTimeAsync(
            await _timeZoneService.ConvertFromTimeZoneAsync(localTime, timeZone), 
            timeZone);

        if (conflictsWithObservance.IsSuccess && conflictsWithObservance.Value)
        {
            // For festival messages (like Deepavali), evening is often preferred
            if (culturalContext.IsFestivalRelated && culturalContext.FestivalName?.ToLower().Contains("deepavali") == true)
            {
                var festivalOptimalTime = localTime.Date.AddHours(18); // 6 PM for celebrations
                if (localTime.Hour >= 22)
                    festivalOptimalTime = festivalOptimalTime.AddDays(1);
                return festivalOptimalTime;
            }

            // For other Hindu messages, suggest mid-morning
            var optimalTime = localTime.Date.AddHours(11); // 11 AM
            if (localTime.Hour >= 16)
                optimalTime = optimalTime.AddDays(1);
                
            return optimalTime;
        }

        return localTime;
    }

    private async Task<DateTime> FindGeneralOptimalTime(DateTime localTime)
    {
        // Ensure message is sent during reasonable hours (8 AM - 8 PM)
        if (localTime.Hour < 8)
            return await Task.FromResult(localTime.Date.AddHours(9)); // 9 AM
        
        if (localTime.Hour >= 20)
            return await Task.FromResult(localTime.Date.AddDays(1).AddHours(9)); // Next day 9 AM
            
        return await Task.FromResult(localTime);
    }

    private async Task<DateTime> EnsureOptimalHours(DateTime localTime, WhatsAppCulturalContext culturalContext)
    {
        // Buddhist preferences: morning hours
        if (culturalContext.RequiresBuddhistCalendarAwareness)
        {
            if (localTime.Hour < 9 || localTime.Hour > 16)
                return await Task.FromResult(localTime.Date.AddHours(10)); // 10 AM
        }

        // Hindu festival preferences: evening for celebrations
        if (culturalContext.RequiresHinduCalendarAwareness && culturalContext.IsFestivalRelated)
        {
            if (localTime.Hour < 16 || localTime.Hour > 20)
                return await Task.FromResult(localTime.Date.AddHours(18)); // 6 PM
        }

        // General optimal hours
        if (localTime.Hour < 8 || localTime.Hour > 20)
            return await Task.FromResult(localTime.Date.AddHours(10)); // 10 AM

        return await Task.FromResult(localTime);
    }

    private async Task<TimeWindow> GetVesakGreetingWindow(DateTime vesaDate, string timeZone)
    {
        // Vesak greetings are best sent in the morning for reflection
        var startTime = vesaDate.Date.AddHours(8); // 8 AM
        var endTime = vesaDate.Date.AddHours(16);   // 4 PM

        return new TimeWindow
        {
            StartTime = await _timeZoneService.ConvertFromTimeZoneAsync(startTime, timeZone),
            EndTime = await _timeZoneService.ConvertFromTimeZoneAsync(endTime, timeZone),
            Description = "Vesak morning reflection period - optimal for Buddhist greetings",
            IsOptimal = true
        };
    }

    private async Task<TimeWindow> GetDeepavaliGreetingWindow(DateTime deepavaliDate, string timeZone)
    {
        // Deepavali greetings are best sent in the evening during celebration time
        var startTime = deepavaliDate.Date.AddHours(16); // 4 PM
        var endTime = deepavaliDate.Date.AddHours(22);   // 10 PM

        return new TimeWindow
        {
            StartTime = await _timeZoneService.ConvertFromTimeZoneAsync(startTime, timeZone),
            EndTime = await _timeZoneService.ConvertFromTimeZoneAsync(endTime, timeZone),
            Description = "Deepavali evening celebration period - optimal for festival greetings",
            IsOptimal = true
        };
    }

    private async Task<TimeWindow> GetPosonGreetingWindow(DateTime posonDate, string timeZone)
    {
        // Poson (Buddhism arrival in Sri Lanka) - morning optimal
        var startTime = posonDate.Date.AddHours(7);  // 7 AM
        var endTime = posonDate.Date.AddHours(15);   // 3 PM

        return new TimeWindow
        {
            StartTime = await _timeZoneService.ConvertFromTimeZoneAsync(startTime, timeZone),
            EndTime = await _timeZoneService.ConvertFromTimeZoneAsync(endTime, timeZone),
            Description = "Poson morning observance period - optimal for Buddhist cultural messages",
            IsOptimal = true
        };
    }

    private async Task<TimeWindow> GetThaiPusamGreetingWindow(DateTime thaiPusamDate, string timeZone)
    {
        // Thai Pusam - early morning and late evening are significant
        var startTime = thaiPusamDate.Date.AddHours(5);  // 5 AM (early morning devotions)
        var endTime = thaiPusamDate.Date.AddHours(11);   // 11 AM

        return new TimeWindow
        {
            StartTime = await _timeZoneService.ConvertFromTimeZoneAsync(startTime, timeZone),
            EndTime = await _timeZoneService.ConvertFromTimeZoneAsync(endTime, timeZone),
            Description = "Thai Pusam morning devotion period - optimal for Hindu festival messages",
            IsOptimal = true
        };
    }

    private async Task<TimeWindow> GetDefaultFestivalWindow(DateTime festivalDate, string timeZone)
    {
        // Default festival window - general celebration time
        var startTime = festivalDate.Date.AddHours(10); // 10 AM
        var endTime = festivalDate.Date.AddHours(19);   // 7 PM

        return new TimeWindow
        {
            StartTime = await _timeZoneService.ConvertFromTimeZoneAsync(startTime, timeZone),
            EndTime = await _timeZoneService.ConvertFromTimeZoneAsync(endTime, timeZone),
            Description = "General festival celebration period",
            IsOptimal = false
        };
    }

    private async Task<bool> CheckBuddhistTimingSensitivity(DateTime localTime, WhatsAppCulturalContext culturalContext)
    {
        // Very sensitive during Vesak and other major Buddhist festivals
        if (culturalContext.FestivalName?.ToLower().Contains("vesak") == true)
        {
            // Avoid late evening during Vesak (meditation time)
            if (localTime.Hour >= 19 && localTime.Hour <= 22)
                return false;
        }

        // Check for Poyaday sensitivity
        var isPoyaDay = await _culturalCalendarService.IsBuddhistObservanceDayAsync(localTime.Date);
        if (isPoyaDay && (localTime.Hour >= 18 && localTime.Hour <= 21))
            return false;

        return await Task.FromResult(true); // Generally acceptable
    }

    private async Task<bool> CheckHinduTimingSensitivity(DateTime localTime, WhatsAppCulturalContext culturalContext)
    {
        // During Hindu festivals, evening is often celebration time (not sensitive)
        if (culturalContext.IsFestivalRelated)
        {
            // Deepavali evening is celebration time - highly appropriate
            if (culturalContext.FestivalName?.ToLower().Contains("deepavali") == true &&
                localTime.Hour >= 17 && localTime.Hour <= 21)
                return true; // Very appropriate
        }

        // Check for general Hindu prayer times (sensitive periods)
        if (IsWithinTimeWindow(localTime.Hour, _hinduObservanceHours["morning_prayers"]) ||
            IsWithinTimeWindow(localTime.Hour, _hinduObservanceHours["evening_prayers"]))
        {
            // Less sensitive during festivals
            if (culturalContext.IsFestivalRelated)
                return true;
                
            return false; // Avoid prayer times for non-festival messages
        }

        return await Task.FromResult(true); // Generally acceptable
    }

    private async Task<bool> CheckGeneralTimingSensitivity(DateTime localTime)
    {
        // Avoid very early morning (before 7 AM) or very late night (after 10 PM)
        if (localTime.Hour < 7 || localTime.Hour >= 22)
            return false;

        // Weekend mornings are generally more relaxed
        if ((localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday) &&
            localTime.Hour >= 8 && localTime.Hour <= 19)
            return true;

        // Weekday optimal hours
        if (localTime.DayOfWeek >= DayOfWeek.Monday && localTime.DayOfWeek <= DayOfWeek.Friday &&
            localTime.Hour >= 9 && localTime.Hour <= 18)
            return true;

        return await Task.FromResult(true); // Generally acceptable within reasonable hours
    }

    #endregion
}