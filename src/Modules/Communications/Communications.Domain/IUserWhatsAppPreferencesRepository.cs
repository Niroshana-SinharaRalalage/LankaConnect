using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
namespace LankaConnect.Modules.Communications.Domain;

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

    /// <summary>
    /// Phase 7D Fix 4: preferences that are enabled + unverified where the grace clock
    /// (<c>WhatsAppEnabledAt</c>) elapsed before <paramref name="cutoff"/>. Returns a tracked
    /// entity list so the auto-disable job can call <see cref="Entities.UserWhatsAppPreferences.AutoDisableUnverified"/>
    /// and persist the change in one UoW.
    /// </summary>
    Task<IReadOnlyList<UserWhatsAppPreferences>> GetStaleUnverifiedAsync(
        DateTime cutoff, CancellationToken ct = default);
}
