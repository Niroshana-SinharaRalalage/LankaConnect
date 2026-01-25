using LankaConnect.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LankaConnect.Infrastructure.Services;

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

    private string GetFrontendBaseUrl()
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
