using LankaConnect.Modules.Identity.Contracts;
using System.Text;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Host.AllInOne.Services;

/// <summary>
/// Phase 6A.100: Shared service for sending registration confirmation emails.
/// Consolidates email logic from multiple handlers to eliminate duplication.
/// Supports both free and paid events, member and anonymous registrations.
///
/// Migrated to use ITypedEmailService with strongly-typed email parameters
/// (FreeEventRegistrationEmailParams and TicketConfirmationEmailParams).
/// </summary>
public class RegistrationEmailService : IRegistrationEmailService
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IFormRepository _eventFormRepository;
    private readonly ILogger<RegistrationEmailService> _logger;

    public RegistrationEmailService(
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        IFormRepository eventFormRepository,
        ILogger<RegistrationEmailService> logger)
    {
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _eventFormRepository = eventFormRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> SendFreeEventConfirmationEmailAsync(
        Registration registration,
        Event @event,
        UserContactDto? user,
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

            // Build event details URL for the email
            var eventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);

            // Create typed email parameters
            var emailParams = FreeEventRegistrationEmailParams.Create(
                eventId: @event.Id,
                registrationId: registration.Id,
                userName: recipientName,
                userEmail: recipientEmail,
                eventTitle: @event.Title.Value,
                // Phase 8YA-2 TODO: registration is blocked on TBD events (Q2=A); param class should accept DateTime?.
                eventStartDate: @event.StartDate.GetValueOrDefault(),
                eventStartTime: @event.StartDate.HasValue ? @event.StartDate.Value.ToString("h:mm tt") : "TBD",
                eventLocation: GetEventLocationString(@event),
                eventDetailsUrl: eventDetailsUrl,
                registrationDate: registration.CreatedAt
            );

            // Phase 7C.2b Chunk 2c: emit decomposed location keys so the
            // Venue Name + Address + optional Secondary Location block renders
            // correctly in the free-event confirmation template. Without this,
            // the fallback in ToDictionary() would project the scalar into
            // LocationAddress (keeping the email non-empty) — but the primary
            // goal is multi-venue support for events with a named venue.
            emailParams.WithLocationDetails(@event.ProjectEmailLocation());

            // Set timezone for consistent date/time display
            emailParams.TimeZoneId = @event.TimeZoneId;

            // Add optional parameters
            if (hasAttendeeDetails)
            {
                emailParams.WithAttendees(attendeeDetailsHtml);
            }

            if (hasEventImage)
            {
                emailParams.WithEventImage(eventImageUrl);
            }

            // Add contact information
            if (registration.Contact != null)
            {
                emailParams.WithContactInfo(
                    registration.Contact.Email,
                    registration.Contact.PhoneNumber);
            }

            // Add organizer contact details
            emailParams.WithOrganizerContacts(
                @event.OrganizerContacts
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                    .ToList());

            // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
            if (@event.HasSignUpLists())
            {
                emailParams.WithSignUpLists(
                    _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
            }

            // Phase 6A.128: Add signup forms URL if event has active forms
            var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);
            var hasActiveForms = forms.Any(f => f.Status == FormStatus.Active);
            if (hasActiveForms)
            {
                emailParams.WithSignupForms(
                    _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#signup-forms");
            }

            // Send email via typed email service
            var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Email sending failed - Email={Email}, Errors={Errors}",
                    recipientEmail, string.Join(", ", result.Errors));
                return Result.Failure($"Email sending failed: {string.Join(", ", result.Errors)}");
            }

            _logger.LogInformation(
                "Free event confirmation email sent successfully - Email={Email}, RegistrationId={RegistrationId}, CorrelationId={CorrelationId}",
                recipientEmail, registration.Id, result.CorrelationId);

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
        UserContactDto? user,
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

            // Build event details URL for the email
            var eventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);

            // Get payment amount
            var amountPaid = registration.TotalPrice?.Amount ?? 0;

            // Create typed email parameters
            var emailParams = TicketConfirmationEmailParams.Create(
                eventId: @event.Id,
                registrationId: registration.Id,
                userName: recipientName,
                contactEmail: recipientEmail,
                eventTitle: @event.Title.Value,
                // Phase 8YA-2 TODO: registration is blocked on TBD events (Q2=A); param class should accept DateTime?.
                eventStartDate: @event.StartDate.GetValueOrDefault(),
                eventStartTime: @event.StartDate.HasValue ? @event.StartDate.Value.ToString("h:mm tt") : "TBD",
                eventLocation: GetEventLocationString(@event),
                eventDetailsUrl: eventDetailsUrl,
                amountPaid: amountPaid,
                paymentIntentId: registration.StripePaymentIntentId ?? "",
                paymentDate: registration.UpdatedAt ?? DateTime.UtcNow,
                quantity: registration.Attendees?.Count ?? 1
            );

            // Phase 7C.2b Chunk 2c: emit decomposed location keys — root cause
            // of the 2026-04-23 "empty LOCATION header on paid-ticket resend"
            // regression was that this handler path (used by
            // ResendAttendeeConfirmationCommandHandler) never called
            // WithLocationDetails, so the ToDictionary() fallback projected an
            // empty LocationAddress into the decomposed template. Mirrors the
            // call in PaymentCompletedEventHandler.cs:246.
            emailParams.WithLocationDetails(@event.ProjectEmailLocation());

            // Set timezone for consistent date/time display
            emailParams.TimeZoneId = @event.TimeZoneId;
            emailParams.RegistrationDate = registration.CreatedAt;

            // Add ticket information
            emailParams.WithTicket(
                ticketCode: ticket.TicketCode,
                // Phase 8YA-2 TODO: render expiry as "TBD" when EndDate is null on TBD events.
                expiryDate: @event.EndDate.HasValue ? @event.EndDate.Value.AddDays(1).ToString("MMMM dd, yyyy") : "TBD",
                ticketUrl: "" // Ticket URL if applicable
            );

            // Add optional parameters
            if (hasAttendeeDetails)
            {
                emailParams.WithAttendees(attendeeDetailsHtml);
            }

            // Phase 7F-E.6.B (architect-approved 2026-05-04): cross-surface registration
            // breakdown HTML — same wiring pattern as PaymentCompletedEventHandler so the
            // confirm-attendee resend path emits the breakdown card. Defensive: renderer
            // faults fall through to empty string — email still sends without the card.
            try
            {
                var breakdown = LankaConnect.Products.LankaEvents.Application.Common
                    .TicketPdfRegistrationBreakdownAssembler.Build(registration);
                var breakdownHtml = breakdown is null
                    ? string.Empty
                    : LankaConnect.Products.LankaEvents.Application.Common
                        .RegistrationBreakdownEmailRenderer.Render(breakdown, registration.LeadAttendeeName);
                emailParams.WithRegistrationBreakdownHtml(breakdownHtml);
            }
            catch (Exception breakdownEx)
            {
                _logger.LogWarning(breakdownEx,
                    "[Phase 7F-E.6.B] Failed to render registration breakdown HTML for confirm-attendee email. " +
                    "RegistrationId={RegistrationId}. Falling back to empty string.",
                    registration.Id);
                emailParams.WithRegistrationBreakdownHtml(string.Empty);
            }

            if (hasEventImage)
            {
                emailParams.WithEventImage(eventImageUrl);
            }

            // Add contact information
            if (registration.Contact != null)
            {
                emailParams.WithContactInfo(
                    registration.Contact.Email,
                    registration.Contact.PhoneNumber);
            }

            // Add organizer contact details
            emailParams.WithOrganizerContacts(
                @event.OrganizerContacts
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                    .ToList());

            // Phase 6A.100 Fix: Add signup lists URL if event has signup lists
            if (@event.HasSignUpLists())
            {
                emailParams.WithSignUpLists(
                    _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups");
            }

            // Phase 6A.128: Add signup forms URL if event has active forms
            var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);
            var hasActiveForms = forms.Any(f => f.Status == FormStatus.Active);
            if (hasActiveForms)
            {
                emailParams.WithSignupForms(
                    _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#signup-forms");
            }

            // Create PDF attachment
            var attachments = new List<EmailAttachmentDto>
            {
                new EmailAttachmentDto
                {
                    FileName = $"ticket-{ticket.TicketCode}.pdf",
                    Content = ticketPdf,
                    ContentType = "application/pdf"
                }
            };

            // Send email with attachments via typed email service
            var result = await _typedEmailService.SendEmailWithAttachmentsAsync(
                emailParams,
                attachments,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Email sending failed - Email={Email}, Errors={Errors}",
                    recipientEmail, string.Join(", ", result.Errors));
                return Result.Failure($"Email sending failed: {string.Join(", ", result.Errors)}");
            }

            _logger.LogInformation(
                "Paid event confirmation email sent successfully - Email={Email}, TicketCode={TicketCode}, CorrelationId={CorrelationId}",
                recipientEmail, ticket.TicketCode, result.CorrelationId);

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
    private (string Email, string Name) GetRecipientInfo(Registration registration, UserContactDto? user)
    {
        if (user != null)
        {
            // Member registration
            return (user.Email, $"{user.FirstName} {user.LastName}");
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
    /// Slice 7 S7.7: Appends seat label when the attendee has one (assigned-seating events).
    /// </summary>
    private string BuildAttendeeDetailsHtml(Registration registration)
    {
        if (!registration.HasDetailedAttendees() || !registration.Attendees.Any())
            return string.Empty;

        var html = new StringBuilder();
        foreach (var attendee in registration.Attendees)
        {
            var seatSuffix = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                ? $" <span style=\"color: #2563EB; font-weight: 600;\">(Seat {attendee.SeatLabel})</span>"
                : "";
            html.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}{seatSuffix}</p>");
        }

        return html.ToString().TrimEnd();
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
