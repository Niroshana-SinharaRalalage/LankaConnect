using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Queries.GetUserEmailPreferences;

/// <summary>
/// Query to get user email preferences and notification settings
/// </summary>
/// <param name="UserId">The ID of the user whose preferences to retrieve</param>
public record GetUserEmailPreferencesQuery(
    Guid UserId) : IQuery<GetUserEmailPreferencesResponse>;

/// <summary>
/// Response containing user email preferences
/// </summary>
public class GetUserEmailPreferencesResponse
{
    public UserEmailPreferencesDto EmailPreferences { get; init; }
    public EmailVerificationDto EmailVerificationStatus { get; init; }
    public List<EmailSubscriptionDto> Subscriptions { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public bool CanReceiveEmails => EmailPreferences.ReceiveSystemAlerts || HasAnySubscription;
    public bool HasAnySubscription => Subscriptions.Any(s => s.IsEnabled);

    public GetUserEmailPreferencesResponse(UserEmailPreferencesDto emailPreferences, 
        EmailVerificationDto emailVerificationStatus, List<EmailSubscriptionDto> subscriptions)
    {
        EmailPreferences = emailPreferences;
        EmailVerificationStatus = emailVerificationStatus;
        Subscriptions = subscriptions;
        LastUpdated = emailPreferences.LastUpdated;
    }
}

/// <summary>
/// Email subscription information
/// </summary>
public class EmailSubscriptionDto
{
    public string SubscriptionType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public EmailFrequency Frequency { get; set; }
    public bool IsRequired { get; set; } // Some subscriptions like security alerts cannot be disabled
    public DateTime LastUpdated { get; set; }
}