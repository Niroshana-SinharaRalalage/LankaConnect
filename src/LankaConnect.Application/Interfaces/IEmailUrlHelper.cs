namespace LankaConnect.Application.Interfaces;

/// <summary>
/// Service for building URLs used in email templates.
/// Centralizes URL construction from configuration to eliminate hardcoded URLs.
/// </summary>
public interface IEmailUrlHelper
{
    /// <summary>
    /// Builds the email verification URL.
    /// </summary>
    /// <param name="token">The verification token.</param>
    /// <returns>The complete email verification URL.</returns>
    string BuildEmailVerificationUrl(string token);

    /// <summary>
    /// Builds the event details URL.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The complete event details URL.</returns>
    string BuildEventDetailsUrl(Guid eventId);

    /// <summary>
    /// Builds the event management URL.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The complete event management URL.</returns>
    string BuildEventManageUrl(Guid eventId);

    /// <summary>
    /// Builds the event signup URL.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The complete event signup URL.</returns>
    string BuildEventSignupUrl(Guid eventId);

    /// <summary>
    /// Builds the my events URL.
    /// </summary>
    /// <returns>The complete my events URL.</returns>
    string BuildMyEventsUrl();

    /// <summary>
    /// Builds the newsletter confirmation URL.
    /// </summary>
    /// <param name="token">The confirmation token.</param>
    /// <returns>The complete newsletter confirmation URL.</returns>
    string BuildNewsletterConfirmUrl(string token);

    /// <summary>
    /// Builds the newsletter unsubscribe URL.
    /// </summary>
    /// <param name="token">The unsubscribe token.</param>
    /// <returns>The complete newsletter unsubscribe URL.</returns>
    string BuildNewsletterUnsubscribeUrl(string token);

    /// <summary>
    /// Builds the general unsubscribe URL.
    /// </summary>
    /// <param name="token">The unsubscribe token.</param>
    /// <returns>The complete unsubscribe URL.</returns>
    string BuildUnsubscribeUrl(string token);

    /// <summary>
    /// Builds the ticket view URL.
    /// </summary>
    /// <param name="ticketId">The ticket ID.</param>
    /// <returns>The complete ticket view URL.</returns>
    string BuildTicketViewUrl(Guid ticketId);

    /// <summary>
    /// Builds the password reset URL.
    /// Phase 6A.101: Added for environment-aware password reset links.
    /// </summary>
    /// <param name="token">The password reset token.</param>
    /// <returns>The complete password reset URL.</returns>
    string BuildPasswordResetUrl(string token);

    /// <summary>
    /// Builds the form edit URL for editing a form response.
    /// Phase 6A.116: Added to fix email edit button 404 error (Issue #8).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="formId">The form ID.</param>
    /// <param name="accessToken">Optional access token for anonymous users.</param>
    /// <returns>The complete form edit URL with optional token parameter.</returns>
    string BuildFormEditUrl(Guid eventId, Guid formId, string? accessToken = null);

    /// <summary>
    /// Builds the signup lists URL for viewing event signup lists.
    /// Phase 6A.116: Added for signup list button in emails (Issue #9).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The complete signup lists URL with anchor to signup lists section.</returns>
    string BuildSignupListsUrl(Guid eventId);

    /// <summary>
    /// Gets the frontend base URL.
    /// Phase 6A.116: Exposed for direct URL construction in special cases.
    /// </summary>
    /// <returns>The frontend base URL without trailing slash.</returns>
    string GetFrontendBaseUrl();
}
