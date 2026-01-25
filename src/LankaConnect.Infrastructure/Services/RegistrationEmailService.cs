using System.Globalization;
using System.Text;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.X: Shared service for sending registration confirmation emails.
/// Consolidates email logic from multiple handlers to eliminate duplication.
/// Supports both free and paid events, member and anonymous registrations.
/// </summary>
public class RegistrationEmailService : IRegistrationEmailService
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<RegistrationEmailService> _logger;

    public RegistrationEmailService(
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<RegistrationEmailService> logger)
    {
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> SendFreeEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        User? user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending free event confirmation email - RegistrationId={RegistrationId}, EventId={EventId}, HasUser={HasUser}",
                registration.Id, @event.Id, user != null);

            // Get recipient email and name
            var (recipientEmail, recipientName) = GetRecipientInfo(registration, user);

            if (string.IsNullOrEmpty(recipientEmail))
            {
                _logger.LogWarning(
                    "No recipient email found - RegistrationId={RegistrationId}", registration.Id);
                return Result.Failure("No email address found for this registration");
            }

            // Prepare attendee details HTML
            var attendeeDetailsHtml = BuildAttendeeDetailsHtml(registration);
            var hasAttendeeDetails = registration.HasDetailedAttendees() && registration.Attendees.Any();

            // Get event image URL
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";
            var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

            // Format event date/time range
            var eventDateTimeRange = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);

            // Prepare email parameters
            var parameters = new Dictionary<string, object>
            {
                { "UserName", recipientName },
                { "EventTitle", @event.Title.Value },
                { "EventDateTime", eventDateTimeRange },
                { "EventLocation", GetEventLocationString(@event) },
                { "RegistrationDate", registration.CreatedAt.ToString("MMMM dd, yyyy h:mm tt") },
                { "Attendees", attendeeDetailsHtml },
                { "HasAttendeeDetails", hasAttendeeDetails },
                { "EventImageUrl", eventImageUrl },
                { "HasEventImage", hasEventImage }
            };

            // Add contact information
            if (registration.Contact != null)
            {
                parameters["ContactEmail"] = registration.Contact.Email;
                parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
                parameters["HasContactInfo"] = true;
            }
            else
            {
                parameters["ContactEmail"] = "";
                parameters["ContactPhone"] = "";
                parameters["HasContactInfo"] = false;
            }

            // Add organizer contact details
            parameters["HasOrganizerContact"] = @event.HasOrganizerContact();
            parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "";
            parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "";
            parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? "";

            // Render email template
            var renderResult = await _emailTemplateService.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                parameters,
                cancellationToken);

            if (renderResult.IsFailure)
            {
                _logger.LogError(
                    "Template rendering failed - Template={Template}, Error={Error}",
                    EmailTemplateNames.FreeEventRegistration, renderResult.Error);
                return Result.Failure($"Template rendering failed: {renderResult.Error}");
            }

            // Build and send email
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody
            };

            var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
            if (emailResult.IsFailure)
            {
                _logger.LogError(
                    "Email sending failed - Email={Email}, Errors={Errors}",
                    recipientEmail, string.Join(", ", emailResult.Errors));
                return Result.Failure($"Email sending failed: {string.Join(", ", emailResult.Errors)}");
            }

            _logger.LogInformation(
                "Free event confirmation email sent successfully - Email={Email}, RegistrationId={RegistrationId}",
                recipientEmail, registration.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while sending free event confirmation - RegistrationId={RegistrationId}",
                registration.Id);
            return Result.Failure($"Failed to send confirmation email: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SendPaidEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        Ticket ticket,
        byte[] ticketPdf,
        User? user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending paid event confirmation email - RegistrationId={RegistrationId}, TicketCode={TicketCode}, HasUser={HasUser}",
                registration.Id, ticket.TicketCode, user != null);

            // Get recipient email and name
            var (recipientEmail, recipientName) = GetRecipientInfo(registration, user);

            if (string.IsNullOrEmpty(recipientEmail))
            {
                _logger.LogWarning(
                    "No recipient email found - RegistrationId={RegistrationId}", registration.Id);
                return Result.Failure("No email address found for this registration");
            }

            // Prepare attendee details HTML
            var attendeeDetailsHtml = BuildAttendeeDetailsHtml(registration);
            var hasAttendeeDetails = registration.HasDetailedAttendees() && registration.Attendees.Any();

            // Get event image URL
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";
            var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

            // Format event date/time range
            var eventDateTimeRange = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);

            // Prepare email parameters
            var parameters = new Dictionary<string, object>
            {
                { "UserName", recipientName },
                { "EventTitle", @event.Title.Value },
                { "EventDateTime", eventDateTimeRange },
                { "EventLocation", GetEventLocationString(@event) },
                { "RegistrationDate", registration.CreatedAt.ToString("MMMM dd, yyyy h:mm tt") },
                { "Attendees", attendeeDetailsHtml },
                { "HasAttendeeDetails", hasAttendeeDetails },
                { "EventImageUrl", eventImageUrl },
                { "HasEventImage", hasEventImage },
                // Payment details
                { "AmountPaid", registration.TotalPrice?.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US")) ?? "$0.00" },
                { "PaymentIntentId", registration.StripePaymentIntentId ?? "" },
                { "PaymentDate", registration.UpdatedAt?.ToString("MMMM dd, yyyy h:mm tt") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
                // Ticket details
                { "HasTicket", true },
                { "TicketCode", ticket.TicketCode },
                { "TicketExpiryDate", @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy") }
            };

            // Add contact information
            if (registration.Contact != null)
            {
                parameters["ContactEmail"] = registration.Contact.Email;
                parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
                parameters["HasContactInfo"] = true;
            }
            else
            {
                parameters["ContactEmail"] = "";
                parameters["ContactPhone"] = "";
                parameters["HasContactInfo"] = false;
            }

            // Add organizer contact details
            parameters["HasOrganizerContact"] = @event.HasOrganizerContact();
            parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "";
            parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "";
            parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? "";

            // Render email template
            var renderResult = await _emailTemplateService.RenderTemplateAsync(
                EmailTemplateNames.PaidEventRegistration,
                parameters,
                cancellationToken);

            if (renderResult.IsFailure)
            {
                _logger.LogError(
                    "Template rendering failed - Template={Template}, Error={Error}",
                    EmailTemplateNames.PaidEventRegistration, renderResult.Error);
                return Result.Failure($"Template rendering failed: {renderResult.Error}");
            }

            // Build email message with PDF attachment
            var emailMessage = new EmailMessageDto
            {
                ToEmail = recipientEmail,
                ToName = recipientName,
                Subject = renderResult.Value.Subject,
                HtmlBody = renderResult.Value.HtmlBody,
                PlainTextBody = renderResult.Value.PlainTextBody,
                Attachments = new List<EmailAttachment>
                {
                    new EmailAttachment
                    {
                        FileName = $"ticket-{ticket.TicketCode}.pdf",
                        Content = ticketPdf,
                        ContentType = "application/pdf"
                    }
                }
            };

            var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
            if (emailResult.IsFailure)
            {
                _logger.LogError(
                    "Email sending failed - Email={Email}, Errors={Errors}",
                    recipientEmail, string.Join(", ", emailResult.Errors));
                return Result.Failure($"Email sending failed: {string.Join(", ", emailResult.Errors)}");
            }

            _logger.LogInformation(
                "Paid event confirmation email sent successfully - Email={Email}, TicketCode={TicketCode}",
                recipientEmail, ticket.TicketCode);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while sending paid event confirmation - RegistrationId={RegistrationId}, TicketCode={TicketCode}",
                registration.Id, ticket.TicketCode);
            return Result.Failure($"Failed to send confirmation email: {ex.Message}");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Gets recipient email and name from registration and user.
    /// For member registrations, uses user email and name.
    /// For anonymous registrations, uses contact info and first attendee name.
    /// </summary>
    private (string Email, string Name) GetRecipientInfo(Registration registration, User? user)
    {
        if (user != null)
        {
            // Member registration
            return (user.Email.Value, $"{user.FirstName} {user.LastName}");
        }

        // Anonymous registration
        var email = registration.Contact?.Email ?? "";
        var name = registration.HasDetailedAttendees() && registration.Attendees.Any()
            ? registration.Attendees.First().Name
            : "Guest";

        return (email, name);
    }

    /// <summary>
    /// Builds HTML for attendee details display in email template.
    /// Shows attendee names only (no ages) in paragraph format.
    /// </summary>
    private string BuildAttendeeDetailsHtml(Registration registration)
    {
        if (!registration.HasDetailedAttendees() || !registration.Attendees.Any())
            return string.Empty;

        var html = new StringBuilder();
        foreach (var attendee in registration.Attendees)
        {
            html.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
        }

        return html.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats event date/time range for display.
    /// Same day: "December 24, 2025 from 5:00 PM to 10:00 PM"
    /// Different days: "December 24, 2025 at 5:00 PM to December 25, 2025 at 10:00 PM"
    /// </summary>
    private static string FormatEventDateTimeRange(DateTime startDate, DateTime endDate)
    {
        if (startDate.Date == endDate.Date)
        {
            // Same day event
            return $"{startDate:MMMM dd, yyyy} from {startDate:h:mm tt} to {endDate:h:mm tt}";
        }
        else
        {
            // Multi-day event
            return $"{startDate:MMMM dd, yyyy} at {startDate:h:mm tt} to {endDate:MMMM dd, yyyy} at {endDate:h:mm tt}";
        }
    }

    /// <summary>
    /// Gets event location string with defensive null handling.
    /// Returns "Online Event" if location is null or empty.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }

    #endregion
}
