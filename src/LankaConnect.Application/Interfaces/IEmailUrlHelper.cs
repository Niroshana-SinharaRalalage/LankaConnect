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
}
