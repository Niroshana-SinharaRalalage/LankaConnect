using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service interface for religious observance management across different faiths
/// </summary>
public interface IReligiousObservanceService
{
    // Islamic observances
    bool IsRamadan(DateTime date);
    TimeSpan GetIftarTime(DateTime date, GeographicRegion region);
    TimeSpan GetSuhoorTime(DateTime date, GeographicRegion region);
    bool IsEidAlFitr(DateTime date);
    TimeSpan GetEidPrayerTime(DateTime eidDate, GeographicRegion region);
    IslamicCelebrationTimes GetIslamicCelebrationTimes(DateTime eidDate);
    
    // Christian observances
    bool IsChurchServiceTime(DateTime dateTime, GeographicRegion region);
    bool IsChristmas(DateTime date);
    ChristianServiceTimes GetChristmasServiceTimes(DateTime christmasDate, GeographicRegion region);
    
    // Buddhist/Hindu observances
    Task<bool> CheckBuddhistObservanceRestrictionsAsync(DateTime dateTime, GeographicRegion region);
    Task<bool> CheckHinduObservanceRestrictionsAsync(DateTime dateTime, GeographicRegion region);
}

/// <summary>
/// Islamic celebration times for Eid and other festivals
/// </summary>
public record IslamicCelebrationTimes(
    TimeSpan EidPrayer,
    TimeSpan CommunityGathering,
    TimeSpan FamilyTime);

/// <summary>
/// Christian service times for Christmas and other celebrations
/// </summary>
public record ChristianServiceTimes(
    TimeSpan MidnightMass,
    TimeSpan MorningService,
    TimeSpan EveningService);