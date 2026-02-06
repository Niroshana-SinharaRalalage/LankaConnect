using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Application.Common.Interfaces;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for UserEmailPreferences entities
/// Follows TDD principles and integrates Result pattern for error handling
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>, IUserEmailPreferencesRepository
{
    private readonly ILogger<UserEmailPreferencesRepository> _repoLogger;

    public UserEmailPreferencesRepository(
        AppDbContext context,
        ILogger<UserEmailPreferencesRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _repoLogger.LogWarning("GetByUserIdAsync called with empty Guid");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByUserIdAsync START: UserId={UserId}", userId);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAsync COMPLETE: UserId={UserId}, Found={Found}, Duration={ElapsedMs}ms",
                    userId,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUserIdAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<UserEmailPreferences?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _repoLogger.LogWarning("GetByEmailAsync called with null/empty email");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByEmail"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("Email", email))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEmailAsync START: Email={Email}", email);

            try
            {
                // This would require joining with User entity to get email
                // For now, we'll implement a basic approach assuming we have user context
                // In a real implementation, this might need to join with the Users table
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId != Guid.Empty, cancellationToken); // Placeholder logic

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEmailAsync COMPLETE: Email={Email}, Found={Found}, Duration={ElapsedMs}ms (Placeholder logic)",
                    email,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEmailAsync FAILED: Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task UpdateAsync(UserEmailPreferences preferences, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("UserId", preferences.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("UpdateAsync START: UserId={UserId}", preferences.UserId);

            try
            {
                _dbSet.Update(preferences);
                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "UpdateAsync COMPLETE: UserId={UserId}, AllowMarketing={AllowMarketing}, AllowNotifications={AllowNotifications}, AllowNewsletters={AllowNewsletters}, Duration={ElapsedMs}ms",
                    preferences.UserId,
                    preferences.AllowMarketing,
                    preferences.AllowNotifications,
                    preferences.AllowNewsletters,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "UpdateAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    preferences.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<List<UserEmailPreferences>> GetUsersWithPreferencesAsync(
        bool? allowMarketing = null,
        bool? allowNotifications = null,
        bool? allowNewsletters = null,
        string? preferredLanguage = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUsersWithPreferences"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("AllowMarketing", allowMarketing))
        using (LogContext.PushProperty("AllowNotifications", allowNotifications))
        using (LogContext.PushProperty("AllowNewsletters", allowNewsletters))
        using (LogContext.PushProperty("PreferredLanguage", preferredLanguage))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetUsersWithPreferencesAsync START: AllowMarketing={AllowMarketing}, AllowNotifications={AllowNotifications}, AllowNewsletters={AllowNewsletters}, PreferredLanguage={PreferredLanguage}",
                allowMarketing, allowNotifications, allowNewsletters, preferredLanguage);

            try
            {
                var query = _dbSet.AsNoTracking();

                if (allowMarketing.HasValue)
                    query = query.Where(p => p.AllowMarketing == allowMarketing.Value);

                if (allowNotifications.HasValue)
                    query = query.Where(p => p.AllowNotifications == allowNotifications.Value);

                if (allowNewsletters.HasValue)
                    query = query.Where(p => p.AllowNewsletters == allowNewsletters.Value);

                if (!string.IsNullOrWhiteSpace(preferredLanguage))
                    query = query.Where(p => p.PreferredLanguage == preferredLanguage);

                var result = await query
                    .OrderBy(p => p.UserId)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetUsersWithPreferencesAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetUsersWithPreferencesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<bool> HasOptedOutOfMarketingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "HasOptedOutOfMarketing"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("HasOptedOutOfMarketingAsync START: UserId={UserId}", userId);

            try
            {
                var preferences = await GetByUserIdAsync(userId, cancellationToken);
                var hasOptedOut = preferences?.AllowMarketing == false;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "HasOptedOutOfMarketingAsync COMPLETE: UserId={UserId}, HasOptedOut={HasOptedOut}, Duration={ElapsedMs}ms",
                    userId,
                    hasOptedOut,
                    stopwatch.ElapsedMilliseconds);

                return hasOptedOut;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "HasOptedOutOfMarketingAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetActiveUsersCountAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveUsersCount"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetActiveUsersCountAsync START");

            try
            {
                var count = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.AllowNotifications || p.AllowNewsletters || p.AllowMarketing)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetActiveUsersCountAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetActiveUsersCountAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<List<UserEmailPreferences>> GetUsersByPreferenceAsync(
        string preferenceName,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUsersByPreference"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("PreferenceName", preferenceName))
        using (LogContext.PushProperty("IsEnabled", isEnabled))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetUsersByPreferenceAsync START: PreferenceName={PreferenceName}, IsEnabled={IsEnabled}",
                preferenceName, isEnabled);

            try
            {
                var query = _dbSet.AsNoTracking();

                // Map preference name to specific property
                query = preferenceName.ToLowerInvariant() switch
                {
                    "marketing" or "allowmarketing" => query.Where(p => p.AllowMarketing == isEnabled),
                    "notifications" or "allownotifications" => query.Where(p => p.AllowNotifications == isEnabled),
                    "newsletters" or "allownewsletters" => query.Where(p => p.AllowNewsletters == isEnabled),
                    "transactional" or "allowtransactional" => query.Where(p => p.AllowTransactional == isEnabled),
                    _ => query // Return all if preference name not recognized
                };

                var result = await query
                    .OrderBy(p => p.UserId)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetUsersByPreferenceAsync COMPLETE: PreferenceName={PreferenceName}, IsEnabled={IsEnabled}, Count={Count}, Duration={ElapsedMs}ms",
                    preferenceName,
                    isEnabled,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetUsersByPreferenceAsync FAILED: PreferenceName={PreferenceName}, IsEnabled={IsEnabled}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    preferenceName,
                    isEnabled,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
