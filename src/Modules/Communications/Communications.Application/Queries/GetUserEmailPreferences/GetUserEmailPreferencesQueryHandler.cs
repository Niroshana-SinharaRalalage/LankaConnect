using LankaConnect.Modules.Identity.Contracts;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using IUserEmailPreferencesRepository = LankaConnect.BuildingBlocks.Application.Common.Interfaces.IUserEmailPreferencesRepository;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Queries.GetUserEmailPreferences;

/// <summary>
/// Handler for retrieving user email preferences
/// </summary>
public class GetUserEmailPreferencesQueryHandler : IRequestHandler<GetUserEmailPreferencesQuery, Result<GetUserEmailPreferencesResponse>>
{
    private readonly IIdentityQueries _identityQueries;
    private readonly IUserEmailPreferencesRepository _preferencesRepository;
    private readonly ILogger<GetUserEmailPreferencesQueryHandler> _logger;

    public GetUserEmailPreferencesQueryHandler(
        IIdentityQueries identityQueries,
        IUserEmailPreferencesRepository preferencesRepository,
        ILogger<GetUserEmailPreferencesQueryHandler> logger)
    {
        _identityQueries = identityQueries;
        _preferencesRepository = preferencesRepository;
        _logger = logger;
    }

    public async Task<Result<GetUserEmailPreferencesResponse>> Handle(GetUserEmailPreferencesQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetUserEmailPreferences"))
        using (LogContext.PushProperty("EntityType", "UserEmailPreferences"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetUserEmailPreferences START: UserId={UserId}",
                request.UserId);

            try
            {
                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserEmailPreferences FAILED: Invalid UserId - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<GetUserEmailPreferencesResponse>.Failure("User ID is required");
                }

                // Validate user exists
                var user = await _identityQueries.GetContactInfoAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserEmailPreferences FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<GetUserEmailPreferencesResponse>.Failure("User not found");
                }

                // Get user email preferences (create default if none exist)
                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);
                var createdDefaults = false;
                if (preferences == null)
                {
                    preferences = await CreateDefaultPreferencesAsync(user.Id, cancellationToken);
                    createdDefaults = true;
                }

                // Map email preferences to DTO
                var preferencesDto = new UserEmailPreferencesDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    ReceiveWelcomeEmails = preferences.AllowTransactional, // Welcome emails are transactional
                    ReceiveBusinessNotifications = preferences.AllowNotifications,
                    ReceiveMarketingEmails = preferences.AllowMarketing,
                    ReceiveSystemAlerts = preferences.AllowTransactional, // System alerts are transactional
                    ReceivePasswordAlerts = preferences.AllowTransactional, // Password alerts are transactional
                    NotificationFrequency = EmailFrequency.Immediate, // Default frequency since domain doesn't store this
                    LastUpdated = preferences.UpdatedAt ?? DateTime.UtcNow
                };

                // Create email verification status DTO
                var verificationDto = new EmailVerificationDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    IsEmailVerified = user.IsEmailVerified,
                    VerificationTokenExpiresAt = user.EmailVerificationTokenExpiresAt,
                    LastVerificationSentAt = GetLastVerificationSentDate(user.EmailVerificationTokenExpiresAt),
                    VerificationAttempts = GetVerificationAttempts(user.IsEmailVerified, user.EmailVerificationTokenExpiresAt)
                };

                // Get email subscriptions
                var subscriptions = GetEmailSubscriptions(preferences);

                var response = new GetUserEmailPreferencesResponse(
                    preferencesDto,
                    verificationDto,
                    subscriptions);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUserEmailPreferences COMPLETE: UserId={UserId}, IsEmailVerified={IsEmailVerified}, AllowMarketing={AllowMarketing}, CreatedDefaults={CreatedDefaults}, Duration={ElapsedMs}ms",
                    request.UserId, user.IsEmailVerified, preferences.AllowMarketing, createdDefaults, stopwatch.ElapsedMilliseconds);

                return Result<GetUserEmailPreferencesResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUserEmailPreferences FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<GetUserEmailPreferencesResponse>.Failure("An error occurred while retrieving email preferences");
            }
        }
    }

    private async Task<LankaConnect.Modules.Communications.Domain.Entities.UserEmailPreferences> CreateDefaultPreferencesAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var createResult = LankaConnect.Modules.Communications.Domain.Entities.UserEmailPreferences.Create(userId);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create default preferences: {createResult.Error}");
        }

        var defaultPreferences = createResult.Value;
        await _preferencesRepository.AddAsync(defaultPreferences, cancellationToken);

        _logger.LogInformation("Created default email preferences for user {UserId}", userId);

        return defaultPreferences;
    }

    private static DateTime? GetLastVerificationSentDate(DateTime? emailVerificationTokenExpiresAt)
    {
        // Calculate when verification token was created (tokens expire after 24 hours).
        // Wave 4.10.s1c (2026-06-26): refactored to accept primitive instead of User aggregate
        // so the Application layer doesn't need the User type.
        if (emailVerificationTokenExpiresAt.HasValue)
        {
            return emailVerificationTokenExpiresAt.Value.AddHours(-24);
        }
        return null;
    }

    private static int GetVerificationAttempts(bool isEmailVerified, DateTime? emailVerificationTokenExpiresAt)
    {
        // This would need to be tracked in the domain or retrieved from email logs.
        // For now, return a default value based on whether a verification flow has happened.
        // (Token presence is equivalent to ExpiresAt presence — both set together by the
        // User aggregate's GenerateEmailVerificationToken method.)
        return isEmailVerified ? 1 : (emailVerificationTokenExpiresAt.HasValue ? 1 : 0);
    }

    private static List<EmailSubscriptionDto> GetEmailSubscriptions(LankaConnect.Modules.Communications.Domain.Entities.UserEmailPreferences preferences)
    {
        return new List<EmailSubscriptionDto>
        {
            new EmailSubscriptionDto
            {
                SubscriptionType = "welcome",
                DisplayName = "Welcome Emails",
                Description = "Receive welcome messages and getting started guides",
                IsEnabled = preferences.AllowTransactional,
                Frequency = EmailFrequency.Immediate,
                IsRequired = false,
                LastUpdated = preferences.UpdatedAt ?? DateTime.UtcNow
            },
            new EmailSubscriptionDto
            {
                SubscriptionType = "business-notifications",
                DisplayName = "Business Notifications",
                Description = "Updates about your business listings, reviews, and performance",
                IsEnabled = preferences.AllowNotifications,
                Frequency = EmailFrequency.Daily,
                IsRequired = false,
                LastUpdated = preferences.UpdatedAt ?? DateTime.UtcNow
            },
            new EmailSubscriptionDto
            {
                SubscriptionType = "marketing",
                DisplayName = "Marketing & Promotions",
                Description = "Special offers, feature updates, and platform news",
                IsEnabled = preferences.AllowMarketing,
                Frequency = EmailFrequency.Weekly,
                IsRequired = false,
                LastUpdated = preferences.UpdatedAt ?? DateTime.UtcNow
            },
            new EmailSubscriptionDto
            {
                SubscriptionType = "system-alerts",
                DisplayName = "System Alerts",
                Description = "Critical system notifications and maintenance updates",
                IsEnabled = preferences.AllowTransactional,
                Frequency = EmailFrequency.Immediate,
                IsRequired = true, // Cannot be disabled
                LastUpdated = preferences.UpdatedAt ?? DateTime.UtcNow
            },
            new EmailSubscriptionDto
            {
                SubscriptionType = "password-alerts",
                DisplayName = "Security Alerts",
                Description = "Password changes, login attempts, and security notifications",
                IsEnabled = preferences.AllowTransactional,
                Frequency = EmailFrequency.Immediate,
                IsRequired = true, // Cannot be disabled for security
                LastUpdated = preferences.UpdatedAt ?? DateTime.UtcNow
            }
        };
    }
}