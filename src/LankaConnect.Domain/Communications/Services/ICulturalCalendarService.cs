using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service interface for cultural calendar integration supporting Buddhist, Hindu, Islamic, and Christian observances
/// </summary>
public interface ICulturalCalendarService
{
    bool IsPoyaday(DateTime date, GeographicRegion region = GeographicRegion.SriLanka);
    DateTime GetNextNonPoyaday(DateTime currentDate, GeographicRegion region = GeographicRegion.SriLanka);
    bool IsHinduFestivalPeriod(DateTime date);
    bool IsVesakDay(DateTime date);
    bool IsDeepavali(DateTime date);
    
    CulturalTimingPreference GetOptimalSendTime(DateTime requestedTime, CulturalContext context);
    
    // Festival-specific timing methods
    FestivalCelebrationTimes GetVesakCelebrationTimes(DateTime vesakDate, GeographicRegion region);
    HinduAuspiciousTimes GetHinduAuspiciousTimes(DateTime festivalDate, HinduFestival festival);
    AuspiciousTimeSlot GetAuspiciousTime(DateTime date, HinduFestival festival);
    
    // Async methods for cultural calendar analysis
    Task<bool> IsHinduObservanceTimeAsync(DateTime dateTime);
    Task<CulturalTimingPreference> GetOptimalSendTimeAsync(DateTime requestedTime, CulturalContext context);
    Task<bool> IsBuddhistObservanceDayAsync(DateTime dateTime);
    Task<bool> IsSpecialBuddhistDayAsync(DateTime dateTime);
    Task<bool> IsHinduFastingDayAsync(DateTime dateTime);
    Task<bool> IsHinduFestivalDayAsync(DateTime dateTime);
    Task<bool> IsTimeCulturallyAppropriateAsync(DateTime dateTime, CulturalContext context);
}

/// <summary>
/// Festival celebration times for Vesak Day
/// </summary>
public record FestivalCelebrationTimes(
    TimeSpan PreDawnCeremony,
    TimeSpan MainCelebration,
    TimeSpan EveningDhamma);

/// <summary>
/// Hindu auspicious times for festivals
/// </summary>
public record HinduAuspiciousTimes(
    TimeSpan BrahmaMuhurta,
    TimeSpan LakshmiPuja,
    TimeSpan DiyaLighting);

/// <summary>
/// Auspicious time slot with cultural significance
/// </summary>
public record AuspiciousTimeSlot(
    TimeSpan StartTime,
    TimeSpan EndTime,
    string Significance);