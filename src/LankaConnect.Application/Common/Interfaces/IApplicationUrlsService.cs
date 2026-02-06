namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service for generating application URLs used in emails and redirects.
/// Phase 6A.53: Member Email Verification System
/// </summary>
public interface IApplicationUrlsService
{
    /// <summary>
    /// Frontend base URL (e.g., https://lankaconnect.com)
    /// </summary>
    string FrontendBaseUrl { get; }

    /// <summary>
    /// Gets email verification URL with token.
    /// Example: https://lankaconnect.com/verify-email?token=abc123
    /// </summary>
    string GetEmailVerificationUrl(string token);

    /// <summary>
    /// Gets event details page URL.
    /// Example: https://lankaconnect.com/events/123e4567-e89b-12d3-a456-426614174000
    /// </summary>
    string GetEventDetailsUrl(Guid eventId);

    /// <summary>
    /// Gets unsubscribe URL with token.
    /// Example: https://lankaconnect.com/unsubscribe?token=xyz789
    /// </summary>
    string GetUnsubscribeUrl(string token);
}
