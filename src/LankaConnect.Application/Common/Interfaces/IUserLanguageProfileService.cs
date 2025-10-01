using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Routing;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Application.Common.Routing;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// User Language Profile Service Interface
/// Handles comprehensive multi-language user profile management for South Asian diaspora
/// Performance targets: <50ms with L1/L2 caching strategy
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Supports incremental learning and community-wide language pattern changes
/// </summary>
public interface IUserLanguageProfileService
{
    /// <summary>
    /// Store comprehensive multi-language user profile with optimization
    /// Supports complex language hierarchies and cultural preferences
    /// </summary>
    /// <param name="userProfile">Complete user language profile</param>
    /// <returns>Storage success confirmation</returns>
    Task<bool> StoreMultiLanguageProfileAsync(LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile userProfile);

    /// <summary>
    /// Retrieve multi-language user profile with cache optimization
    /// Performance target: <50ms with L1/L2 caching strategy
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Complete user language profile or null if not found</returns>
    Task<LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile?> GetMultiLanguageProfileAsync(Guid userId);

    /// <summary>
    /// Update user language preferences with incremental learning
    /// Adapts to changing language preferences over time
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="languageInteractions">Recent language interaction data</param>
    /// <returns>Updated profile with learning integration</returns>
    Task<LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile> UpdateLanguagePreferencesAsync(Guid userId, List<LankaConnect.Application.Common.Models.MultiLanguage.LanguageInteractionData> languageInteractions);

    /// <summary>
    /// Bulk update user profiles for community-wide language pattern changes
    /// Efficient for cultural event preparation and community migrations
    /// </summary>
    /// <param name="communityUpdates">Community language profile updates</param>
    /// <returns>Bulk update result summary</returns>
    Task<LankaConnect.Application.Common.Routing.BulkProfileUpdateResult> BulkUpdateCommunityLanguageProfilesAsync(List<LankaConnect.Application.Common.Models.MultiLanguage.CommunityLanguageProfileUpdate> communityUpdates);
}