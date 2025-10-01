using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for Google Calendar cultural synchronization with Buddhist/Hindu holidays
/// Provides comprehensive cultural calendar integration for diaspora communities
/// </summary>
public interface IGoogleCalendarCulturalService
{
    /// <summary>
    /// Synchronizes user's Google Calendar with personalized cultural events
    /// Includes Buddhist lunar calendar, Hindu festivals, and Sri Lankan cultural celebrations
    /// </summary>
    /// <param name="userId">User identifier for personalization</param>
    /// <param name="credentials">Google Calendar API credentials</param>
    /// <returns>Result indicating synchronization success</returns>
    Task<Result> SyncCulturalCalendar(UserId userId, GoogleCalendarCredentials credentials);

    /// <summary>
    /// Creates a new cultural event in user's Google Calendar
    /// Supports multi-language descriptions and cultural significance levels
    /// </summary>
    /// <param name="culturalEvent">Cultural event with diaspora customization</param>
    /// <returns>Result indicating event creation success</returns>
    Task<Result> CreateCulturalEvent(GoogleCalendarCulturalEvent culturalEvent);

    /// <summary>
    /// Validates potential scheduling conflicts with cultural observances
    /// Provides alternative timing suggestions respecting religious practices
    /// </summary>
    /// <param name="proposedTime">Proposed meeting or event time</param>
    /// <param name="context">User's cultural context and preferences</param>
    /// <returns>Cultural conflict analysis with resolution suggestions</returns>
    Task<Result<CulturalConflict>> ValidateSchedulingConflict(DateTime proposedTime, LankaConnect.Domain.Communications.ValueObjects.CulturalContext context);

    /// <summary>
    /// Retrieves personalized cultural events based on user's cultural profile
    /// Customized for diaspora location, language preference, and religious practices
    /// </summary>
    /// <param name="profile">User's cultural profile with location and preferences</param>
    /// <returns>Collection of relevant cultural events</returns>
    Task<Result<IEnumerable<GoogleCalendarCulturalEvent>>> GetPersonalizedCulturalEvents(CulturalProfile profile);

    /// <summary>
    /// Synchronizes temple schedules and community events with personal calendar
    /// Enables coordinated participation in diaspora community activities
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="templeLocation">Geographic location for relevant temple schedules</param>
    /// <returns>Result indicating temple schedule synchronization</returns>
    Task<Result> SyncTempleSchedules(UserId userId, DiasporaLocation templeLocation);

    /// <summary>
    /// Creates family-coordinated cultural calendar for multi-member households
    /// Enables synchronized celebration planning across family members
    /// </summary>
    /// <param name="familyId">Family unit identifier</param>
    /// <param name="memberProfiles">Cultural profiles of family members</param>
    /// <returns>Result indicating family calendar coordination success</returns>
    Task<Result> CreateFamilyculturalCalendar(FamilyId familyId, IEnumerable<CulturalProfile> memberProfiles);
}