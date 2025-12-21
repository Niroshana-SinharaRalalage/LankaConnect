using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationConfirmedEvent to send confirmation email to attendee.
/// Phase 6A.24: Enhanced to include attendee details and skip paid events.
/// Phase 6A.37: Added CID-embedded images (header/footer banners + event image)
/// For paid events, email is sent by PaymentCompletedEventHandler after payment.
/// </summary>
public class RegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailBrandingService _emailBrandingService;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEmailBrandingService emailBrandingService,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _emailBrandingService = emailBrandingService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
            domainEvent.EventId, domainEvent.AttendeeId);

        try
        {
            // Retrieve user and event data
            var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for RegistrationConfirmedEvent", domainEvent.AttendeeId);
                return;
            }

            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning("Event {EventId} not found for RegistrationConfirmedEvent", domainEvent.EventId);
                return;
            }

            // Phase 6A.24: Fetch registration to get attendee details and check if paid event
            var registration = await _registrationRepository.GetByEventAndUserAsync(
                domainEvent.EventId, domainEvent.AttendeeId, cancellationToken);

            if (registration == null)
            {
                _logger.LogWarning("Registration not found for Event {EventId}, User {UserId}",
                    domainEvent.EventId, domainEvent.AttendeeId);
                return;
            }

            // Phase 6A.24: Skip email for paid events - PaymentCompletedEventHandler will send it after payment
            if (registration.PaymentStatus == PaymentStatus.Pending)
            {
                _logger.LogInformation(
                    "Skipping email for paid event {EventId}, User {UserId} - waiting for payment completion",
                    domainEvent.EventId, domainEvent.AttendeeId);
                return;
            }

            // Phase 6A.24: Prepare attendee details for email
            var attendeeDetailsHtml = new System.Text.StringBuilder();
            var attendeeDetailsText = new System.Text.StringBuilder();

            if (registration.HasDetailedAttendees())
            {
                foreach (var attendee in registration.Attendees)
                {
                    // HTML format
                    attendeeDetailsHtml.AppendLine($"<p><strong>{attendee.Name}</strong> (Age: {attendee.Age})</p>");

                    // Plain text format
                    attendeeDetailsText.AppendLine($"- {attendee.Name} (Age: {attendee.Age})");
                }
            }
            else
            {
                // Fallback if no detailed attendees
                attendeeDetailsHtml.AppendLine($"<p>{domainEvent.Quantity} attendee(s)</p>");
                attendeeDetailsText.AppendLine($"{domainEvent.Quantity} attendee(s)");
            }

            // Phase 6A.37: Get event's primary image URL for CID embedding
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? "";
            var hasEventImage = !string.IsNullOrEmpty(eventImageUrl);

            // Prepare email parameters with enhanced attendee details
            var parameters = new Dictionary<string, object>
            {
                { "UserName", $"{user.FirstName} {user.LastName}" },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },
                { "EventLocation", GetEventLocationString(@event) },
                { "Quantity", domainEvent.Quantity },
                { "RegistrationDate", domainEvent.RegistrationDate.ToString("MMMM dd, yyyy h:mm tt") },
                // Phase 6A.34: Format attendees as HTML/text string for template rendering
                { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
                { "HasAttendeeDetails", registration.HasDetailedAttendees() },
                // Phase 6A.37: Event image flag for conditional rendering
                { "HasEventImage", hasEventImage }
            };

            // Phase 6A.24: Add contact information if available
            if (registration.Contact != null)
            {
                parameters["ContactEmail"] = registration.Contact.Email;
                parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
                parameters["HasContactInfo"] = true;
            }
            else
            {
                parameters["HasContactInfo"] = false;
            }

            // Phase 6A.37: Prepare CID-embedded image attachments
            var attachments = await PrepareEmailAttachmentsAsync(eventImageUrl, hasEventImage, cancellationToken);

            // Send templated email with inline image attachments
            var result = await _emailService.SendTemplatedEmailAsync(
                "registration-confirmation",
                user.Email.Value,
                parameters,
                attachments,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send RSVP confirmation email to {Email}: {Errors}",
                    user.Email.Value, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("RSVP confirmation email sent successfully to {Email} with {AttendeeCount} attendees and {AttachmentCount} images",
                    user.Email.Value, domainEvent.Quantity, attachments?.Count ?? 0);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "Error handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
                domainEvent.EventId, domainEvent.AttendeeId);
        }
    }

    /// <summary>
    /// Phase 6A.37: Prepares email attachments including header banner, footer banner, and optional event image.
    /// All images are embedded using CID for immediate display in email clients.
    /// </summary>
    private async Task<List<EmailAttachment>?> PrepareEmailAttachmentsAsync(
        string eventImageUrl, bool hasEventImage, CancellationToken cancellationToken)
    {
        var attachments = new List<EmailAttachment>();

        try
        {
            // Get header banner
            var headerResult = await _emailBrandingService.GetHeaderBannerAsync(cancellationToken);
            if (headerResult.IsSuccess)
            {
                attachments.Add(new EmailAttachment
                {
                    FileName = headerResult.Value.FileName,
                    Content = headerResult.Value.Content,
                    ContentType = headerResult.Value.ContentType,
                    ContentId = headerResult.Value.ContentId
                });
                _logger.LogDebug("Added header banner attachment, size: {Size} bytes", headerResult.Value.Content.Length);
            }
            else
            {
                _logger.LogWarning("Failed to get header banner: {Errors}", string.Join(", ", headerResult.Errors));
            }

            // Get footer banner
            var footerResult = await _emailBrandingService.GetFooterBannerAsync(cancellationToken);
            if (footerResult.IsSuccess)
            {
                attachments.Add(new EmailAttachment
                {
                    FileName = footerResult.Value.FileName,
                    Content = footerResult.Value.Content,
                    ContentType = footerResult.Value.ContentType,
                    ContentId = footerResult.Value.ContentId
                });
                _logger.LogDebug("Added footer banner attachment, size: {Size} bytes", footerResult.Value.Content.Length);
            }
            else
            {
                _logger.LogWarning("Failed to get footer banner: {Errors}", string.Join(", ", footerResult.Errors));
            }

            // Get event image if available
            if (hasEventImage && !string.IsNullOrEmpty(eventImageUrl))
            {
                var eventImageResult = await _emailBrandingService.DownloadImageAsync(
                    eventImageUrl, "event-image", cancellationToken);

                if (eventImageResult.IsSuccess)
                {
                    attachments.Add(new EmailAttachment
                    {
                        FileName = eventImageResult.Value.FileName,
                        Content = eventImageResult.Value.Content,
                        ContentType = eventImageResult.Value.ContentType,
                        ContentId = eventImageResult.Value.ContentId
                    });
                    _logger.LogDebug("Added event image attachment, size: {Size} bytes", eventImageResult.Value.Content.Length);
                }
                else
                {
                    _logger.LogWarning("Failed to download event image from {Url}: {Errors}",
                        eventImageUrl, string.Join(", ", eventImageResult.Errors));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error preparing email attachments - email will be sent without images");
        }

        return attachments.Count > 0 ? attachments : null;
    }

    /// <summary>
    /// Phase 6A.35: Safely extracts event location string with defensive null handling.
    /// Handles data inconsistency where has_location=true but address fields are null.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        // Check if Location or Address is null (defensive against data inconsistency)
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        // Handle case where address fields exist but are empty
        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }
}
