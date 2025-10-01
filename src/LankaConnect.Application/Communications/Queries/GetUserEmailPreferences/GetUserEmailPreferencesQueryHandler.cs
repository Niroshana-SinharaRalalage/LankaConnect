using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using IUserRepository = LankaConnect.Domain.Users.IUserRepository;
using IUserEmailPreferencesRepository = LankaConnect.Application.Common.Interfaces.IUserEmailPreferencesRepository;

namespace LankaConnect.Application.Communications.Queries.GetUserEmailPreferences;

/// <summary>
/// Handler for retrieving user email preferences
/// </summary>
public class GetUserEmailPreferencesQueryHandler : IRequestHandler<GetUserEmailPreferencesQuery, Result<GetUserEmailPreferencesResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserEmailPreferencesRepository _preferencesRepository;
    private readonly ILogger<GetUserEmailPreferencesQueryHandler> _logger;

    public GetUserEmailPreferencesQueryHandler(
        IUserRepository userRepository,
        IUserEmailPreferencesRepository preferencesRepository,
        ILogger<GetUserEmailPreferencesQueryHandler> logger)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
        _logger = logger;
    }

    public async Task<Result<GetUserEmailPreferencesResponse>> Handle(GetUserEmailPreferencesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<GetUserEmailPreferencesResponse>.Failure("User not found");
            }

            // Get user email preferences (create default if none exist)
            var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            if (preferences == null)
            {
                preferences = await CreateDefaultPreferencesAsync(user, cancellationToken);
            }

            // Map email preferences to DTO
            var preferencesDto = new UserEmailPreferencesDto
            {
                UserId = user.Id,
                Email = user.Email.Value,
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
                Email = user.Email.Value,
                IsEmailVerified = user.IsEmailVerified,
                VerificationTokenExpiresAt = user.EmailVerificationTokenExpiresAt,
                LastVerificationSentAt = GetLastVerificationSentDate(user),
                VerificationAttempts = GetVerificationAttempts(user)
            };

            // Get email subscriptions
            var subscriptions = GetEmailSubscriptions(preferences);

            var response = new GetUserEmailPreferencesResponse(
                preferencesDto,
                verificationDto,
                subscriptions);

            _logger.LogInformation("Retrieved email preferences for user {UserId}", request.UserId);

            return Result<GetUserEmailPreferencesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email preferences for user {UserId}", request.UserId);
            return Result<GetUserEmailPreferencesResponse>.Failure("An error occurred while retrieving email preferences");
        }
    }

    private async Task<LankaConnect.Domain.Communications.Entities.UserEmailPreferences> CreateDefaultPreferencesAsync(
        LankaConnect.Domain.Users.User user, CancellationToken cancellationToken)
    {
        var createResult = LankaConnect.Domain.Communications.Entities.UserEmailPreferences.Create(user.Id);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create default preferences: {createResult.Error}");
        }
        
        var defaultPreferences = createResult.Value;
        await _preferencesRepository.AddAsync(defaultPreferences, cancellationToken);
        
        _logger.LogInformation("Created default email preferences for user {UserId}", user.Id);
        
        return defaultPreferences;
    }

    private static DateTime? GetLastVerificationSentDate(LankaConnect.Domain.Users.User user)
    {
        // Calculate when verification token was created (tokens expire after 24 hours)
        if (user.EmailVerificationTokenExpiresAt.HasValue)
        {
            return user.EmailVerificationTokenExpiresAt.Value.AddHours(-24);
        }
        return null;
    }

    private static int GetVerificationAttempts(LankaConnect.Domain.Users.User user)
    {
        // This would need to be tracked in the domain or retrieved from email logs
        // For now, return a default value
        return user.IsEmailVerified ? 1 : (user.EmailVerificationToken != null ? 1 : 0);
    }

    private static List<EmailSubscriptionDto> GetEmailSubscriptions(LankaConnect.Domain.Communications.Entities.UserEmailPreferences preferences)
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