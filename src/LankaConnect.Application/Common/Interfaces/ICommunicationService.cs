using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using SouthAsianLanguage = LankaConnect.Domain.Common.Enums.SouthAsianLanguage;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Communication service interface for sending notifications and alerts
/// </summary>
public interface ICommunicationService
{
    /// <summary>
    /// Sends an emergency notification
    /// </summary>
    Task<Result<string>> SendEmergencyNotificationAsync(
        string message,
        List<string> recipients,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a cultural event notification
    /// </summary>
    Task<Result<string>> SendCulturalEventNotificationAsync(
        string eventId,
        string message,
        SouthAsianLanguage language,
        List<string> recipients,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a system alert
    /// </summary>
    Task<Result<string>> SendSystemAlertAsync(
        string alertType,
        string message,
        string priority,
        List<string> recipients,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification preferences for a user
    /// </summary>
    Task<Result<Dictionary<string, object>>> GetNotificationPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates notification preferences for a user
    /// </summary>
    Task<Result<bool>> UpdateNotificationPreferencesAsync(
        string userId,
        Dictionary<string, object> preferences,
        CancellationToken cancellationToken = default);
}