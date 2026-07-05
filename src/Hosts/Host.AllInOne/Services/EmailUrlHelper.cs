using LankaConnect.Application.Interfaces;
using Microsoft.Extensions.Configuration;
namespace LankaConnect.Host.AllInOne.Services;

/// <summary>
/// Service for building URLs used in email templates.
/// Centralizes URL construction from configuration to eliminate hardcoded URLs.
/// </summary>
public class EmailUrlHelper : IEmailUrlHelper
{
    private readonly IConfiguration _configuration;

    public EmailUrlHelper(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string BuildEmailVerificationUrl(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var verificationPath = _configuration["ApplicationUrls:EmailVerificationPath"] ?? "/verify-email";

        return $"{frontendBaseUrl}{verificationPath}?token={Uri.EscapeDataString(token)}";
    }

    public string BuildEventDetailsUrl(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var eventDetailsPath = _configuration["ApplicationUrls:EventDetailsPath"] ?? "/events/{eventId}";

        // Replace the {eventId} placeholder with the actual ID
        var path = eventDetailsPath.Replace("{eventId}", eventId.ToString());

        return $"{frontendBaseUrl}{path}";
    }

    public string BuildEventManageUrl(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var eventManagePath = _configuration["ApplicationUrls:EventManagePath"] ?? "/events/{eventId}/manage";

        // Replace the {eventId} placeholder with the actual ID
        var path = eventManagePath.Replace("{eventId}", eventId.ToString());

        return $"{frontendBaseUrl}{path}";
    }

    public string BuildEventSignupUrl(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var eventSignupPath = _configuration["ApplicationUrls:EventSignupPath"] ?? "/events/{eventId}/signup";

        // Replace the {eventId} placeholder with the actual ID
        var path = eventSignupPath.Replace("{eventId}", eventId.ToString());

        return $"{frontendBaseUrl}{path}";
    }

    public string BuildMyEventsUrl()
    {
        var frontendBaseUrl = GetFrontendBaseUrl();
        var myEventsPath = _configuration["ApplicationUrls:MyEventsPath"] ?? "/my-events";

        return $"{frontendBaseUrl}{myEventsPath}";
    }

    public string BuildNewsletterConfirmUrl(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        var apiBaseUrl = GetApiBaseUrl();
        var confirmPath = _configuration["ApplicationUrls:NewsletterConfirmPath"] ?? "/api/newsletter/confirm";

        return $"{apiBaseUrl}{confirmPath}?token={Uri.EscapeDataString(token)}";
    }

    public string BuildNewsletterUnsubscribeUrl(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        var apiBaseUrl = GetApiBaseUrl();
        var unsubscribePath = _configuration["ApplicationUrls:NewsletterUnsubscribePath"] ?? "/api/newsletter/unsubscribe";

        return $"{apiBaseUrl}{unsubscribePath}?token={Uri.EscapeDataString(token)}";
    }

    public string BuildUnsubscribeUrl(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var unsubscribePath = _configuration["ApplicationUrls:UnsubscribePath"] ?? "/unsubscribe";

        return $"{frontendBaseUrl}{unsubscribePath}?token={Uri.EscapeDataString(token)}";
    }

    public string BuildTicketViewUrl(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var ticketViewPath = _configuration["ApplicationUrls:TicketViewPath"] ?? "/tickets/{ticketId}";

        // Replace the {ticketId} placeholder with the actual ID
        var path = ticketViewPath.Replace("{ticketId}", ticketId.ToString());

        return $"{frontendBaseUrl}{path}";
    }

    /// <summary>
    /// Builds the password reset URL.
    /// Phase 6A.101: Added for environment-aware password reset links.
    /// </summary>
    public string BuildPasswordResetUrl(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var passwordResetPath = _configuration["ApplicationUrls:PasswordResetPath"] ?? "/reset-password";

        return $"{frontendBaseUrl}{passwordResetPath}?token={Uri.EscapeDataString(token)}";
    }

    /// <summary>
    /// Builds the form edit URL for editing a form response.
    /// Phase 6A.116: Added to fix email edit button 404 error (Issue #8).
    /// Fixes duplicate /events/{id}/events/{id} path bug.
    /// </summary>
    public string BuildFormEditUrl(Guid eventId, Guid formId, string? accessToken = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        if (formId == Guid.Empty)
        {
            throw new ArgumentException("Form ID cannot be empty.", nameof(formId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var formEditPath = _configuration["ApplicationUrls:FormEditPath"] ?? "/events/{eventId}/forms/{formId}";

        // Replace placeholders
        var path = formEditPath
            .Replace("{eventId}", eventId.ToString())
            .Replace("{formId}", formId.ToString());

        var url = $"{frontendBaseUrl}{path}";

        // Add access token for anonymous users
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            url = $"{url}?token={Uri.EscapeDataString(accessToken)}";
        }

        return url;
    }

    /// <summary>
    /// Builds the signup lists URL for viewing event signup lists.
    /// Phase 6A.116: Added for signup list button in emails (Issue #9).
    /// </summary>
    public string BuildSignupListsUrl(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var eventDetailsPath = _configuration["ApplicationUrls:EventDetailsPath"] ?? "/events/{eventId}";

        // Replace the {eventId} placeholder
        var path = eventDetailsPath.Replace("{eventId}", eventId.ToString());

        // Add anchor to signup lists section
        return $"{frontendBaseUrl}{path}#signup-lists";
    }

    /// <summary>
    /// Builds the signup forms URL for viewing event signup forms.
    /// Phase 6A.116: Added for signup forms button in emails (Issue #4).
    /// </summary>
    public string BuildSignupFormsUrl(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        var frontendBaseUrl = GetFrontendBaseUrl();
        var eventDetailsPath = _configuration["ApplicationUrls:EventDetailsPath"] ?? "/events/{eventId}";

        // Replace the {eventId} placeholder
        var path = eventDetailsPath.Replace("{eventId}", eventId.ToString());

        // Add anchor to signup forms section (matches frontend tab ID)
        return $"{frontendBaseUrl}{path}#signup-forms";
    }

    /// <summary>
    /// Gets the frontend base URL.
    /// Phase 6A.116: Made public to support direct URL construction.
    /// </summary>
    public string GetFrontendBaseUrl()
    {
        var url = _configuration["ApplicationUrls:FrontendBaseUrl"];

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("ApplicationUrls:FrontendBaseUrl is not configured.");
        }

        return url.TrimEnd('/');
    }

    private string GetApiBaseUrl()
    {
        var url = _configuration["ApplicationUrls:ApiBaseUrl"];

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("ApplicationUrls:ApiBaseUrl is not configured.");
        }

        return url.TrimEnd('/');
    }
}
