namespace LankaConnect.Infrastructure.Email.Configuration;

/// <summary>
/// Configuration for application URLs used in emails and redirects.
/// Supports environment-specific base URLs (dev/staging/production).
/// Phase 0 - Email System Configuration Infrastructure
/// </summary>
public sealed class ApplicationUrlsOptions
{
    public const string SectionName = "ApplicationUrls";

    /// <summary>
    /// Frontend base URL (e.g., https://lankaconnect.com, http://localhost:3000 for dev)
    /// </summary>
    public string FrontendBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email verification path (default: /verify-email)
    /// </summary>
    public string EmailVerificationPath { get; set; } = "/verify-email";

    /// <summary>
    /// Unsubscribe path (default: /unsubscribe)
    /// </summary>
    public string UnsubscribePath { get; set; } = "/unsubscribe";

    /// <summary>
    /// Event details path template (default: /events/{eventId})
    /// </summary>
    public string EventDetailsPath { get; set; } = "/events/{eventId}";

    /// <summary>
    /// Gets email verification URL with token.
    /// Example: https://lankaconnect.com/verify-email?token=abc123
    /// </summary>
    /// <param name="token">Verification token</param>
    /// <returns>Full URL for email verification</returns>
    public string GetEmailVerificationUrl(string token)
    {
        ValidateFrontendBaseUrl();
        return $"{FrontendBaseUrl.TrimEnd('/')}{EmailVerificationPath}?token={token}";
    }

    /// <summary>
    /// Gets event details page URL.
    /// Example: https://lankaconnect.com/events/123e4567-e89b-12d3-a456-426614174000
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Full URL for event details page</returns>
    public string GetEventDetailsUrl(Guid eventId)
    {
        ValidateFrontendBaseUrl();
        return $"{FrontendBaseUrl.TrimEnd('/')}{EventDetailsPath.Replace("{eventId}", eventId.ToString())}";
    }

    /// <summary>
    /// Gets unsubscribe URL with token.
    /// Example: https://lankaconnect.com/unsubscribe?token=xyz789
    /// </summary>
    /// <param name="token">Unsubscribe token</param>
    /// <returns>Full URL for unsubscribe page</returns>
    public string GetUnsubscribeUrl(string token)
    {
        ValidateFrontendBaseUrl();
        return $"{FrontendBaseUrl.TrimEnd('/')}{UnsubscribePath}?token={token}";
    }

    private void ValidateFrontendBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(FrontendBaseUrl))
        {
            throw new InvalidOperationException(
                $"{SectionName}:FrontendBaseUrl is not configured in appsettings.json. " +
                "Please configure the frontend base URL for the current environment.");
        }
    }
}
