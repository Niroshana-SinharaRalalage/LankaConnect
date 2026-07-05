using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Enums;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.SPLIT_PER_ENTITY.Repositories;

/// <summary>
/// Phase 7A: Repository implementation for UserWhatsAppPreferences entities.
/// Follows existing codebase patterns with structured logging and observability.
/// </summary>
public class UserWhatsAppPreferencesRepository : Repository<UserWhatsAppPreferences>, IUserWhatsAppPreferencesRepository
{
    private readonly ILogger<UserWhatsAppPreferencesRepository> _repoLogger;

    public UserWhatsAppPreferencesRepository(
        AppDbContext context,
        ILogger<UserWhatsAppPreferencesRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<UserWhatsAppPreferences?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _repoLogger.LogWarning("GetByUserIdAsync called with empty Guid");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetByUserIdAsync START: UserId={UserId}", userId);

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetByUserIdAsync COMPLETE: UserId={UserId}, Found={Found}, Duration={ElapsedMs}ms",
                    userId, result != null, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetByUserIdAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<UserWhatsAppPreferences?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            _repoLogger.LogWarning("GetByPhoneNumberAsync called with null/empty phoneNumber");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByPhoneNumber"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetByPhoneNumberAsync START");

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(p => p.WhatsAppPhoneNumber == phoneNumber, ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetByPhoneNumberAsync COMPLETE: Found={Found}, Duration={ElapsedMs}ms",
                    result != null, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetByPhoneNumberAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<UserWhatsAppPreferences>> GetVerifiedUsersByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var idList = userIds.ToList();

        using (LogContext.PushProperty("Operation", "GetVerifiedUsersByIds"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        using (LogContext.PushProperty("UserIdCount", idList.Count))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetVerifiedUsersByIdsAsync START: UserIdCount={UserIdCount}", idList.Count);

            try
            {
                var result = await _dbSet
                    .Where(p => idList.Contains(p.UserId))
                    .Where(p => p.WhatsAppEnabled && p.PhoneVerified)
                    .AsNoTracking()
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetVerifiedUsersByIdsAsync COMPLETE: UserIdCount={UserIdCount}, VerifiedCount={VerifiedCount}, Duration={ElapsedMs}ms",
                    idList.Count, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetVerifiedUsersByIdsAsync FAILED: UserIdCount={UserIdCount}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    idList.Count, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<int> GetUsersEnabledButUnverifiedCountAsync(CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetUsersEnabledButUnverifiedCount"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetUsersEnabledButUnverifiedCountAsync START");

            try
            {
                var count = await _dbSet
                    .AsNoTracking()
                    .CountAsync(p => p.WhatsAppEnabled && !p.PhoneVerified, ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetUsersEnabledButUnverifiedCountAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    count, stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetUsersEnabledButUnverifiedCountAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<UserWhatsAppPreferences>> GetStaleUnverifiedAsync(
        DateTime cutoff, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetStaleUnverified"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        using (LogContext.PushProperty("Cutoff", cutoff))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetStaleUnverifiedAsync START: Cutoff={Cutoff:o}", cutoff);

            try
            {
                // Tracked (not AsNoTracking) — the auto-disable job mutates each row
                // and SaveChangesAsync will flush the update + fire the domain event.
                var result = await _dbSet
                    .Where(p => p.WhatsAppEnabled
                             && !p.PhoneVerified
                             && p.WhatsAppEnabledAt != null
                             && p.WhatsAppEnabledAt < cutoff)
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetStaleUnverifiedAsync COMPLETE: Cutoff={Cutoff:o}, Count={Count}, Duration={ElapsedMs}ms",
                    cutoff, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetStaleUnverifiedAsync FAILED: Cutoff={Cutoff:o}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    cutoff, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<UserWhatsAppPreferences>> GetUsersOptedInForNotificationTypeAsync(
        WhatsAppNotificationType notificationType, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetUsersOptedInForNotificationType"))
        using (LogContext.PushProperty("EntityType", "UserWhatsAppPreferences"))
        using (LogContext.PushProperty("NotificationType", notificationType))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetUsersOptedInForNotificationTypeAsync START: NotificationType={NotificationType}", notificationType);

            try
            {
                var query = _dbSet
                    .Where(p => p.WhatsAppEnabled && p.PhoneVerified)
                    .AsNoTracking();

                // Filter by notification type preference
                query = notificationType switch
                {
                    WhatsAppNotificationType.EventRegistration => query.Where(p => p.NotifyEventRegistration),
                    WhatsAppNotificationType.EventReminder => query.Where(p => p.NotifyEventReminder),
                    WhatsAppNotificationType.EventCancellation => query.Where(p => p.NotifyEventCancellation),
                    WhatsAppNotificationType.EventUpdate => query.Where(p => p.NotifyEventUpdate),
                    WhatsAppNotificationType.SignupCommitment => query.Where(p => p.NotifySignupCommitment),
                    WhatsAppNotificationType.Refund => query.Where(p => p.NotifyRefund),
                    WhatsAppNotificationType.Newsletter => query.Where(p => p.NotifyNewsletter),
                    WhatsAppNotificationType.NewEvent => query.Where(p => p.NotifyNewEvent),
                    WhatsAppNotificationType.Payment => query.Where(p => p.NotifyPayment),
                    _ => query.Where(_ => false) // Unknown type — return nothing
                };

                var result = await query.ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetUsersOptedInForNotificationTypeAsync COMPLETE: NotificationType={NotificationType}, Count={Count}, Duration={ElapsedMs}ms",
                    notificationType, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetUsersOptedInForNotificationTypeAsync FAILED: NotificationType={NotificationType}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    notificationType, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }
}
