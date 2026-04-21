using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Domain.Communications;

/// <summary>
/// Phase 7A: Repository interface for user WhatsApp preferences.
/// </summary>
public interface IUserWhatsAppPreferencesRepository : IRepository<UserWhatsAppPreferences>
{
    Task<UserWhatsAppPreferences?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserWhatsAppPreferences?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
    Task<IReadOnlyList<UserWhatsAppPreferences>> GetVerifiedUsersByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<IReadOnlyList<UserWhatsAppPreferences>> GetUsersOptedInForNotificationTypeAsync(
        Enums.WhatsAppNotificationType notificationType, CancellationToken ct = default);

    /// <summary>
    /// Count of users who turned WhatsApp on but never completed phone verification.
    /// These users silently do not receive any notifications — an admin signal that the
    /// verification flow is dropping conversions.
    /// </summary>
    Task<int> GetUsersEnabledButUnverifiedCountAsync(CancellationToken ct = default);
}
