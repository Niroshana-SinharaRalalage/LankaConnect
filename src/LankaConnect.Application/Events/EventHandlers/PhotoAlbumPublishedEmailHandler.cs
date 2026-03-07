using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Sends email notifications to registered attendees when a photo album is published.
/// Uses fire-and-forget pattern to avoid blocking the HTTP response.
///
/// Design decisions:
/// - Only notifies attendees with Confirmed or Attended registration status
/// - Uses Task.Run with new DI scope (Phase 6A.127 pattern) because the HTTP request scope
///   is disposed by the time the email is sent
/// - Fail-silent: never throws to prevent rolling back the album publish transaction
/// - Sends emails individually per registration (not bulk) to support per-recipient personalization
///
/// TODO: When the database email template "template-photo-album-published" is created,
/// extract PhotoAlbumPublishedEmailParams to its own file in LankaConnect.Shared/Email/Contracts/
/// and add the template name to EmailTemplateContract.TemplateNames.
/// </summary>
public class PhotoAlbumPublishedEmailHandler : INotificationHandler<DomainEventNotification<PhotoAlbumPublishedDomainEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<PhotoAlbumPublishedEmailHandler> _logger;

    public PhotoAlbumPublishedEmailHandler(
        IServiceScopeFactory scopeFactory,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<PhotoAlbumPublishedEmailHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _registrationRepository = registrationRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<PhotoAlbumPublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PhotoAlbumPublished START: AlbumId={AlbumId}, EventId={EventId}, EventTitle={EventTitle}",
                domainEvent.AlbumId, domainEvent.EventId, domainEvent.EventTitle);

            // Get all registrations for the event (read-only, no tracking needed)
            var registrations = await _registrationRepository.GetByEventAsync(
                domainEvent.EventId, cancellationToken, trackChanges: false);

            // Filter to only Confirmed and Attended registrations
            var eligibleRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed
                         || r.Status == RegistrationStatus.Attended)
                .ToList();

            if (eligibleRegistrations.Count == 0)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "PhotoAlbumPublished SKIPPED: No eligible registrations for EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                return;
            }

            _logger.LogInformation(
                "PhotoAlbumPublished: Found {TotalRegistrations} registrations, {EligibleCount} eligible for notification - EventId={EventId}",
                registrations.Count, eligibleRegistrations.Count, domainEvent.EventId);

            // Build the album URL using the event details URL with photos anchor
            var albumUrl = $"{_emailUrlHelper.BuildEventDetailsUrl(domainEvent.EventId)}/photos";

            // Capture values for Task.Run closure (avoid capturing disposed objects)
            var capturedScopeFactory = _scopeFactory;
            var capturedAlbumId = domainEvent.AlbumId;
            var capturedEventId = domainEvent.EventId;
            var capturedEventTitle = domainEvent.EventTitle;
            var capturedAlbumUrl = albumUrl;

            // Build email params list before entering Task.Run to fail fast on data issues
            var emailParamsList = new List<PhotoAlbumPublishedEmailParams>();

            foreach (var registration in eligibleRegistrations)
            {
                var recipientEmail = GetRecipientEmail(registration);
                var recipientName = GetRecipientName(registration);

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    _logger.LogWarning(
                        "PhotoAlbumPublished: Skipping registration {RegistrationId} - no email address found",
                        registration.Id);
                    continue;
                }

                emailParamsList.Add(PhotoAlbumPublishedEmailParams.Create(
                    recipientEmail: recipientEmail,
                    recipientName: recipientName,
                    eventId: capturedEventId,
                    eventTitle: capturedEventTitle,
                    albumUrl: capturedAlbumUrl));
            }

            if (emailParamsList.Count == 0)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "PhotoAlbumPublished SKIPPED: No valid email addresses found for EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                return;
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "PhotoAlbumPublished DISPATCHING: Sending {EmailCount} emails async - AlbumId={AlbumId}, EventId={EventId}, Duration={ElapsedMs}ms",
                emailParamsList.Count, capturedAlbumId, capturedEventId, stopwatch.ElapsedMilliseconds);

            // Fire-and-forget: send emails in background thread with new DI scope
            // Phase 6A.127 pattern: HTTP request scope is disposed by the time Task.Run executes
            _ = Task.Run(async () =>
            {
                var sentCount = 0;
                var failedCount = 0;

                try
                {
                    using var scope = capturedScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();

                    foreach (var emailParams in emailParamsList)
                    {
                        try
                        {
                            var emailResult = await emailService.SendEmailAsync(emailParams, CancellationToken.None);
                            if (emailResult.Success)
                            {
                                sentCount++;
                            }
                            else
                            {
                                failedCount++;
                                _logger.LogError(
                                    "PhotoAlbumPublished EMAIL FAILED: Email={Email}, AlbumId={AlbumId}, Errors={Errors}",
                                    emailParams.RecipientEmail, capturedAlbumId,
                                    string.Join(", ", emailResult.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            _logger.LogError(ex,
                                "PhotoAlbumPublished EMAIL EXCEPTION: Email={Email}, AlbumId={AlbumId}",
                                emailParams.RecipientEmail, capturedAlbumId);
                        }
                    }

                    _logger.LogInformation(
                        "PhotoAlbumPublished EMAILS COMPLETE: Sent={SentCount}, Failed={FailedCount}, AlbumId={AlbumId}, EventId={EventId}",
                        sentCount, failedCount, capturedAlbumId, capturedEventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "PhotoAlbumPublished EMAIL BATCH EXCEPTION: AlbumId={AlbumId}, EventId={EventId}, Sent={SentCount}, Failed={FailedCount}",
                        capturedAlbumId, capturedEventId, sentCount, failedCount);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "PhotoAlbumPublished FAILED: Exception during handler - AlbumId={AlbumId}, EventId={EventId}",
                domainEvent.AlbumId, domainEvent.EventId);
        }
    }

    /// <summary>
    /// Extracts the recipient email from a registration.
    /// Supports both new multi-attendee format (RegistrationContact) and legacy format (AttendeeInfo).
    /// </summary>
    private static string GetRecipientEmail(Registration registration)
    {
        // New format: shared contact info
        if (registration.Contact != null)
            return registration.Contact.Email;

        // Legacy format: anonymous registration with AttendeeInfo
        if (registration.AttendeeInfo != null)
            return registration.AttendeeInfo.Email.Value;

        // Authenticated user without contact info - email not available at this level
        // The caller should skip this registration
        return string.Empty;
    }

    /// <summary>
    /// Extracts the recipient name from a registration.
    /// Uses the first attendee's name or falls back to contact email prefix.
    /// </summary>
    private static string GetRecipientName(Registration registration)
    {
        // New format: use first attendee name if available
        if (registration.HasDetailedAttendees() && registration.Attendees.Count > 0)
        {
            var firstAttendee = registration.Attendees[0];
            return firstAttendee.Name ?? "Attendee";
        }

        // Legacy format: anonymous registration with AttendeeInfo
        if (registration.AttendeeInfo != null)
            return registration.AttendeeInfo.Name;

        return "Attendee";
    }
}

/// <summary>
/// Typed email parameters for photo album published notification.
/// Implements IEmailParameters for use with ITypedEmailService.
///
/// TODO: When the database template "template-photo-album-published" is created:
/// 1. Move this class to LankaConnect.Shared/Email/Contracts/PhotoAlbumPublishedEmailParams.cs
/// 2. Add template name constant to EmailTemplateContract.TemplateNames
/// 3. Use EmailTemplateContract constants in ToDictionary()
/// 4. Add event image, organizer contact, and other standard fields
/// </summary>
internal class PhotoAlbumPublishedEmailParams : IEmailParameters
{
    /// <summary>
    /// Template name for photo album published notification.
    /// Must match the 'name' column in communications.email_templates table.
    /// </summary>
    public string TemplateName => "template-photo-album-published";

    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = "Attendee";

    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string AlbumUrl { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, RecipientName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { "AlbumUrl", AlbumUrl },
            { "PhotoExpiryDays", 7 },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year },
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (EventId == Guid.Empty)
            errors.Add("EventId is required");

        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");

        if (string.IsNullOrWhiteSpace(AlbumUrl))
            errors.Add("AlbumUrl is required");

        return errors.Count == 0;
    }

    /// <summary>
    /// Factory method to create email params for a photo album published notification.
    /// </summary>
    public static PhotoAlbumPublishedEmailParams Create(
        string recipientEmail,
        string recipientName,
        Guid eventId,
        string eventTitle,
        string albumUrl)
    {
        return new PhotoAlbumPublishedEmailParams
        {
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            EventId = eventId,
            EventTitle = eventTitle,
            AlbumUrl = albumUrl,
        };
    }
}
