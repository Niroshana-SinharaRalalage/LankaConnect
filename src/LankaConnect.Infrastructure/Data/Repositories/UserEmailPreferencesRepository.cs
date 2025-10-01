using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Application.Common.Interfaces;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for UserEmailPreferences entities
/// Follows TDD principles and integrates Result pattern for error handling
/// </summary>
public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>, IUserEmailPreferencesRepository
{
    public UserEmailPreferencesRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return null;

        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("UserId", userId))
        {
            _logger.Debug("Getting user email preferences for user {UserId}", userId);

            var result = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (result != null)
            {
                _logger.Debug("Found email preferences for user {UserId}", userId);
            }
            else
            {
                _logger.Debug("No email preferences found for user {UserId}", userId);
            }

            return result;
        }
    }

    public async Task<UserEmailPreferences?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        using (LogContext.PushProperty("Operation", "GetByEmail"))
        using (LogContext.PushProperty("Email", email))
        {
            _logger.Debug("Getting user email preferences by email {Email}", email);

            // This would require joining with User entity to get email
            // For now, we'll implement a basic approach assuming we have user context
            // In a real implementation, this might need to join with the Users table
            var result = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId != Guid.Empty, cancellationToken); // Placeholder logic

            _logger.Debug("Email preferences lookup by email completed for {Email}", email);
            return result;
        }
    }

    public async Task UpdateAsync(UserEmailPreferences preferences, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "UpdatePreferences"))
        using (LogContext.PushProperty("UserId", preferences.UserId))
        {
            _logger.Debug("Updating email preferences for user {UserId}", preferences.UserId);

            _dbSet.Update(preferences);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.Debug("Updated email preferences for user {UserId}", preferences.UserId);
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
        {
            _logger.Debug("Getting users with specific email preferences");

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

            _logger.Debug("Retrieved {Count} users with matching email preferences", result.Count);
            return result;
        }
    }

    public async Task<bool> HasOptedOutOfMarketingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "HasOptedOutOfMarketing"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var preferences = await GetByUserIdAsync(userId, cancellationToken);
            var hasOptedOut = preferences?.AllowMarketing == false;
            
            _logger.Debug("User {UserId} marketing opt-out status: {HasOptedOut}", userId, hasOptedOut);
            return hasOptedOut;
        }
    }

    public async Task<int> GetActiveUsersCountAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveUsersCount"))
        {
            var count = await _dbSet
                .AsNoTracking()
                .Where(p => p.AllowNotifications || p.AllowNewsletters || p.AllowMarketing)
                .CountAsync(cancellationToken);

            _logger.Debug("Active users count (with enabled preferences): {Count}", count);
            return count;
        }
    }

    public async Task<List<UserEmailPreferences>> GetUsersByPreferenceAsync(
        string preferenceName, 
        bool isEnabled, 
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUsersByPreference"))
        using (LogContext.PushProperty("PreferenceName", preferenceName))
        using (LogContext.PushProperty("IsEnabled", isEnabled))
        {
            _logger.Debug("Getting users by preference {PreferenceName} = {IsEnabled}", preferenceName, isEnabled);

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

            _logger.Debug("Retrieved {Count} users with preference {PreferenceName} = {IsEnabled}", 
                result.Count, preferenceName, isEnabled);
            return result;
        }
    }
}