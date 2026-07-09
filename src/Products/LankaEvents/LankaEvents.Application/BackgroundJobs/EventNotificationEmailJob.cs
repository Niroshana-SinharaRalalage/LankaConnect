using LankaConnect.Modules.Identity.Contracts;
using EmailDateTimeHelper = LankaConnect.Modules.Communications.Contracts.Email.Helpers.EmailDateTimeHelper;
using System.Diagnostics;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Constants;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // 4C.h prereq: cycle-break
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using OrganizerContactInfo = LankaConnect.Modules.Communications.Contracts.Email.Helpers.OrganizerContactInfo;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.BackgroundJobs;

/// <summary>
/// Phase 6A.100: Background job to send manual event notification emails.
/// Migrated from IEmailService to ITypedEmailService with EventDetailsEmailParams.
/// Consolidates recipients from email groups, registrations, and newsletter subscribers.
/// </summary>
public class EventNotificationEmailJob
{
    private readonly IEventNotificationHistoryRepository _historyRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IFormQueries _formQueries;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IIdentityQueries _identityQueries;
    private readonly INewsletterSubscriberRepository _newsletterSubscriberRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EventNotificationEmailJob> _logger;

    public EventNotificationEmailJob(
        IEventNotificationHistoryRepository historyRepository,
        IEventRepository eventRepository,
        IFormQueries formQueries,
        IRegistrationRepository registrationRepository,
        IEventNotificationRecipientService recipientService,
        IIdentityQueries identityQueries,
        INewsletterSubscriberRepository newsletterSubscriberRepository,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        IUnitOfWork unitOfWork,
        ILogger<EventNotificationEmailJob> logger)
    {
        _historyRepository = historyRepository;
        _eventRepository = eventRepository;
        _formQueries = formQueries;
        _registrationRepository = registrationRepository;
        _recipientService = recipientService;
        _identityQueries = identityQueries;
        _newsletterSubscriberRepository = newsletterSubscriberRepository;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executes the email notification job
    /// </summary>
    /// <param name="historyId">History record ID to track and update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(Guid historyId, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "EventNotificationEmail"))
        using (LogContext.PushProperty("EntityType", "EventNotificationHistory"))
        using (LogContext.PushProperty("HistoryId", historyId))
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString()[..8];

            _logger.LogInformation(
                "[Phase 6A.61][{CorrelationId}] EventNotificationEmailJob START: HistoryId={HistoryId}",
                correlationId, historyId);

            try
            {
            // 1. Load history record and event
            var history = await _historyRepository.GetByIdAsync(historyId, cancellationToken);
            if (history == null)
            {
                _logger.LogError("[Phase 6A.61][{CorrelationId}] History record {HistoryId} not found", correlationId, historyId);
                return;
            }

            // Phase 6A.61+ FIX #1: IDEMPOTENCY CHECK BEFORE EMAIL LOOP
            // This prevents duplicate emails on Hangfire retry - check BEFORE sending any emails
            if (history.SuccessfulSends > 0 || history.FailedSends > 0)
            {
                _logger.LogInformation(
                    "[Phase 6A.61][{CorrelationId}] IDEMPOTENCY CHECK - History {HistoryId} already processed " +
                    "(Success: {Success}, Failed: {Failed}). Skipping to prevent duplicate emails.",
                    correlationId, historyId, history.SuccessfulSends, history.FailedSends);
                return; // Exit early - another execution already sent emails
            }

            // Phase 6A.61+ FIX: Use trackChanges: false to properly load email groups from junction table
            // Background jobs don't need change tracking - this ensures .Include("_emailGroupLinks") works correctly (Wave 5.4.c.0)
            var @event = await _eventRepository.GetByIdAsync(history.EventId, trackChanges: false, cancellationToken);
            if (@event == null)
            {
                _logger.LogError("[Phase 6A.61][{CorrelationId}] Event {EventId} not found for history {HistoryId}",
                    correlationId, history.EventId, historyId);
                return;
            }

            // 2. Resolve recipients using EventNotificationRecipientService
            var recipientResult = await _recipientService.ResolveRecipientsAsync(history.EventId, cancellationToken);
            var recipients = new HashSet<string>(recipientResult.EmailAddresses, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Resolved {Count} recipients from email groups/newsletter",
                correlationId, recipients.Count);

            // 3. Add confirmed registrations (filter out anonymous registrations with null UserId)
            // Phase 6A.61+ Fix: Match EventCancellationEmailJob pattern - filter r.UserId.HasValue to prevent NullReferenceException
            var registrations = await _registrationRepository.GetByEventAsync(history.EventId, cancellationToken);
            var confirmedRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed && r.UserId.HasValue)
                .ToList();

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Found {Count} confirmed registrations with user accounts",
                correlationId, confirmedRegistrations.Count);

            if (confirmedRegistrations.Any())
            {
                // Use bulk query like EventCancellationEmailJob for better performance
                var userIds = confirmedRegistrations
                    .Select(r => r.UserId!.Value)
                    .Distinct()
                    .ToList();

                var userEmails = await _identityQueries.GetEmailsByUserIdsAsync(userIds, cancellationToken);

                _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Bulk fetched {Count} user emails",
                    correlationId, userEmails.Count);

                foreach (var email in userEmails.Values)
                {
                    recipients.Add(email);
                }
            }

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Total recipients after adding registrations: {Count}",
                correlationId, recipients.Count);

            // 4. Build template parameters (removed intermediate UpdateSendStatistics to prevent DbUpdateConcurrencyException)
            // Phase 6A.83 Part 3: Build base template data (UserName will be added per-recipient in loop)
            var baseTemplateData = BuildTemplateData(@event);

            // Phase 6A.103: Get event's primary image URL (once, outside the loop)
            var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
            var eventImageUrl = primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "";

            // Phase 6A.129: Check for active signup forms (once, outside the loop)
            var eventForms = await _formQueries.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, @event.Id, cancellationToken);
            var hasActiveSignupForms = eventForms.Any(f => f.Status == FormStatusDto.Active);
            var signupFormsUrl = hasActiveSignupForms
                ? $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms"
                : "";

            // Phase 6A.61+ RCA: Diagnostic logging using LogError to bypass log filtering
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] STARTING EMAIL SEND - Template: event-details, RecipientCount: {RecipientCount}, EventTitle: {EventTitle}",
                correlationId, recipients.Count, @event.Title.Value);

            // Log template data for debugging
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] Template Data Keys: {Keys}",
                correlationId, string.Join(", ", baseTemplateData.Keys));

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Sending to {RecipientCount} recipients",
                correlationId, recipients.Count);

            // 6. Phase 6A.100: Send emails using ITypedEmailService with EventDetailsEmailParams
            int successCount = 0, failedCount = 0;
            int emailIndex = 0;
            foreach (var email in recipients)
            {
                emailIndex++;
                try
                {
                    // Phase 6A.61+ RCA: Log before each email send attempt
                    _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] Sending email {Index}/{Total} to: {Email}",
                        correlationId, emailIndex, recipients.Count, email);

                    // Phase 6A.83 Part 3: Get personalized UserName for recipient
                    var emailResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.Email.Create(email);
                    var user = emailResult.IsSuccess
                        ? await _identityQueries.GetByEmailAsync(emailResult.Value.Value, cancellationToken)
                        : null;
                    var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Valued Guest";

                    // Phase 6A.100: Use typed email params
                    var emailParams = EventDetailsEmailParams.Create(
                        recipientEmail: email,
                        userName: userName,
                        eventTitle: (string)baseTemplateData["EventTitle"],
                        eventDate: (string)baseTemplateData["EventDate"],
                        eventStartDate: (string)baseTemplateData["EventStartDate"],
                        eventStartTime: (string)baseTemplateData["EventStartTime"],
                        eventDateTime: (string)baseTemplateData["EventDateTime"],
                        eventLocation: (string)baseTemplateData["EventLocation"],
                        eventCity: (string)baseTemplateData["EventCity"],
                        eventState: (string)baseTemplateData["EventState"],
                        eventDescription: (string)baseTemplateData["EventDescription"],
                        eventDetailsUrl: (string)baseTemplateData["EventDetailsUrl"],
                        isFree: (bool)baseTemplateData["IsFreeEvent"],
                        pricingDetails: (string)baseTemplateData["PricingDetails"],
                        ticketPrice: (string)baseTemplateData["TicketPrice"],
                        hasSignUpLists: (bool)baseTemplateData["HasSignUpLists"],
                        signUpListsUrl: baseTemplateData.TryGetValue("SignUpListsUrl", out var signUpUrl) ? (string)signUpUrl : "",
                        hasOrganizerContact: (bool)baseTemplateData["HasOrganizerContact"],
                        organizerContactName: baseTemplateData.TryGetValue("OrganizerContactName", out var orgName) ? (string)orgName : "",
                        organizerContactEmail: baseTemplateData.TryGetValue("OrganizerContactEmail", out var orgEmail) ? (string?)orgEmail : null,
                        organizerContactPhone: baseTemplateData.TryGetValue("OrganizerContactPhone", out var orgPhone) ? (string?)orgPhone : null,
                        subjectPrefix: (string)baseTemplateData["SubjectPrefix"]);

                    // Set location flag for conditional subject rendering
                    emailParams.HasLocation = (bool)baseTemplateData["HasLocation"];

                    // Phase 6A.103: Add event image if available
                    emailParams.WithEventImage(eventImageUrl);

                    // Phase 6A.129: Add signup forms URL if event has active forms
                    if (hasActiveSignupForms)
                    {
                        emailParams.WithSignupForms(signupFormsUrl);
                    }

                    // Phase 6A.133 Email: Set all organizer contacts with pre-formatted HTML
                    if (@event.HasOrganizerContact())
                    {
                        emailParams.WithOrganizerContacts(
                            @event.OrganizerContacts
                                .OrderBy(c => c.SortOrder)
                                .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                                .ToList());
                    }

                    // Set per-recipient unsubscribe URL for List-Unsubscribe header (RFC 2369/8058)
                    var subscriber = await _newsletterSubscriberRepository.GetByEmailAsync(email, cancellationToken);
                    if (subscriber?.UnsubscribeToken != null)
                    {
                        emailParams.UnsubscribeUrl = _emailUrlHelper.BuildNewsletterUnsubscribeUrl(subscriber.UnsubscribeToken);
                    }

                    var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                    if (result.Success)
                    {
                        successCount++;
                        _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] Email {Index}/{Total} SUCCESS to: {Email}",
                            correlationId, emailIndex, recipients.Count, email);
                    }
                    else
                    {
                        failedCount++;
                        // Phase 6A.61+ RCA: Log the EXACT error message at ERROR level
                        _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] Email {Index}/{Total} FAILED to: {Email}, Error: {Error}",
                            correlationId, emailIndex, recipients.Count, email, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    // Phase 6A.61+ RCA: Log full exception details
                    _logger.LogError(ex, "[DIAG-NOTIF-JOB][{CorrelationId}] Email {Index}/{Total} EXCEPTION to: {Email}, ExceptionType: {ExceptionType}, Message: {Message}",
                        correlationId, emailIndex, recipients.Count, email, ex.GetType().Name, ex.Message);
                    failedCount++;
                }
            }

            // Phase 6A.61+ RCA: Summary log
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] COMPLETED - Success: {Success}, Failed: {Failed}, Total: {Total}",
                correlationId, successCount, failedCount, recipients.Count);

            // 7. Update history record with final statistics
            // Phase 6A.61+ FIX #2: SINGLE ENTITY LOAD - Update the SAME entity loaded at start (line 66)
            // DO NOT reload to avoid multiple tracked entities causing DbUpdateConcurrencyException
            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Updating history statistics - Recipients: {Recipients}, Success: {Success}, Failed: {Failed}",
                correlationId, recipients.Count, successCount, failedCount);

            history.UpdateSendStatistics(recipients.Count, successCount, failedCount);
            _historyRepository.Update(history);

            // Phase 6A.61+ CRITICAL FIX: Clear ChangeTracker to detach EmailMessage entities
            // The email sending loop creates and tracks EmailMessage entities in the same DbContext
            // If we don't detach them, EF Core will try to save ALL tracked entities (including EmailMessages)
            // causing DbUpdateConcurrencyException when their timestamps have changed
            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Clearing ChangeTracker to detach EmailMessage entities before commit",
                correlationId);

            await _unitOfWork.ClearChangeTrackerExceptAsync<LankaConnect.Products.LankaEvents.Domain.Entities.EventNotificationHistory>(cancellationToken);

            try
            {
                _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Attempting to commit history {HistoryId}",
                    correlationId, historyId);

                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Successfully committed history update. Success: {Success}, Failed: {Failed}",
                    correlationId, successCount, failedCount);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                // Phase 6A.61+ FIX #3: GRACEFUL CONCURRENCY HANDLING
                // DO NOT re-throw exception - emails were sent successfully, that's the primary goal
                // Database statistics update is secondary - Hangfire retry would cause duplicate emails
                _logger.LogWarning(ex,
                    "[Phase 6A.61][{CorrelationId}] CONCURRENCY EXCEPTION when committing history {HistoryId}. " +
                    "Emails sent successfully ({Success} success, {Failed} failed), accepting as partial success. " +
                    "Exiting gracefully to prevent Hangfire retry and duplicate emails.",
                    correlationId, historyId, successCount, failedCount);

                // Check if another concurrent execution saved the statistics
                var reloadedHistory = await _historyRepository.GetByIdAsync(historyId, cancellationToken);
                if (reloadedHistory != null && (reloadedHistory.SuccessfulSends > 0 || reloadedHistory.FailedSends > 0))
                {
                    _logger.LogInformation(
                        "[Phase 6A.61][{CorrelationId}] Verified that another concurrent job execution already saved statistics " +
                        "(Success: {Success}, Failed: {Failed}). Exiting successfully.",
                        correlationId, reloadedHistory.SuccessfulSends, reloadedHistory.FailedSends);
                }
                else
                {
                    _logger.LogWarning(
                        "[Phase 6A.61][{CorrelationId}] Concurrency exception prevented database update, but emails were sent. " +
                        "Accepting as partial success - primary goal (email delivery) achieved.",
                        correlationId);
                }

                    // CRITICAL: Return successfully WITHOUT throwing - prevents Hangfire retry loop
                    return;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex,
                        "[Phase 6A.61][{CorrelationId}] UNEXPECTED EXCEPTION when committing history {HistoryId}. " +
                        "Exception Type: {ExceptionType}, Message: {Message}, Duration={ElapsedMs}ms",
                        correlationId, historyId, ex.GetType().FullName, ex.Message, stopwatch.ElapsedMilliseconds);
                    throw; // Re-throw for Hangfire retry
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[Phase 6A.61][{CorrelationId}] EventNotificationEmailJob COMPLETE: Duration={ElapsedMs}ms, HistoryId={HistoryId}, Success={Success}, Failed={Failed}",
                    correlationId, stopwatch.ElapsedMilliseconds, historyId, successCount, failedCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.61][{CorrelationId}] EventNotificationEmailJob CANCELED: Duration={ElapsedMs}ms, HistoryId={HistoryId}",
                    correlationId, stopwatch.ElapsedMilliseconds, historyId);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[Phase 6A.61][{CorrelationId}] EventNotificationEmailJob FAILED: Duration={ElapsedMs}ms, HistoryId={HistoryId}",
                    correlationId, stopwatch.ElapsedMilliseconds, historyId);
                throw;
            }
        }
    }

    private Dictionary<string, object> BuildTemplateData(LankaConnect.Products.LankaEvents.Domain.Event @event)
    {
        var isFree = @event.IsFree();

        // Phase 6A.97: Use timezone-aware formatting
        var formattedDate = EmailDateTimeHelper.FormatEventDate(@event.StartDate, @event.TimeZoneId);
        var formattedTime = EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId);

        // Phase 6A.98: Determine if event is "New" (within 7 days of publish) or "Upcoming"
        // New events get "New Event:" prefix, older events get "Upcoming Event:" prefix
        var isNewEvent = @event.PublishedAt == null ||
                         (DateTime.UtcNow - @event.PublishedAt.Value).TotalDays <= 7;
        var subjectPrefix = isNewEvent ? "New Event:" : "Upcoming Event:";

        _logger.LogInformation(
            "[Phase 6A.98] Event {EventId} subject prefix: {SubjectPrefix} (PublishedAt: {PublishedAt}, IsNew: {IsNew})",
            @event.Id, subjectPrefix, @event.PublishedAt, isNewEvent);

        // Phase 6A.61+: Include ALL fields from event-published template for consistency
        // This ensures event-details template can reuse the same rich template as event-published
        var data = new Dictionary<string, object>
        {
            // Core fields (original)
            { "EventTitle", @event.Title?.Value ?? "Untitled Event" },
            // Phase 8YA-2 TODO: render "Date TBD" when StartDate is null on TBD events.
            { "EventDate", @event.StartDate.HasValue ? @event.StartDate.Value.ToString("f") : "Date TBD" },
            { "EventLocation", GetEventLocationString(@event) },
            { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
            { "IsFreeEvent", isFree },
            { "PricingDetails", isFree ? "Free" : $"${@event.TicketPrice?.Amount ?? 0:F2}" },

            // Phase 6A.61+: Add event-published fields for rich template compatibility
            { "EventDescription", @event.Description?.Value ?? "" },
            { "EventStartDate", formattedDate },
            { "EventStartTime", formattedTime },
            { "EventDateTime", $"{formattedDate} at {formattedTime}" },  // Phase 6A.87+ Fix: Template expects combined EventDateTime
            // Wave9.h.10.5 F22 fix: double-`?.` on Address is REQUIRED because Location
            // can carry a null Address (e.g. events without a physical location or
            // partially-filled fixtures). Single `?.` on Location only guards Location
            // being null; when Location is non-null but Address is null, `.City` throws
            // NRE. Confirmed via staging Hangfire dashboard: 4x EventNotificationEmailJob
            // failures with NullReferenceException in BuildTemplateData. Contrast the
            // sibling GetEventLocationString below which does `Location?.Address == null`.
            { "EventCity", @event.Location?.Address?.City ?? string.Empty },
            { "EventState", @event.Location?.Address?.State ?? string.Empty },
            { "HasLocation", !string.IsNullOrWhiteSpace(@event.Location?.Address?.City) && !string.IsNullOrWhiteSpace(@event.Location?.Address?.State) },
            { "EventUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) }, // Alias for EventDetailsUrl
            { "IsFree", isFree }, // event-published uses this name
            { "IsPaid", !isFree }, // event-published conditional
            // Phase 6A.100 Fix: Fall back to Pricing.AdultPrice for events using dual/group pricing
            { "TicketPrice", isFree ? "Free" : @event.TicketPrice?.Amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"))
                ?? @event.Pricing?.AdultPrice.Amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"))
                ?? "See Event Details" },
            { "Year", DateTime.UtcNow.Year },  // Phase 6A.87+ Fix: Footer param
            { "SubjectPrefix", subjectPrefix }  // Phase 6A.98: Dynamic subject prefix
        };

        // Add sign-up lists URL if available
        if (@event.SignUpLists?.Any() == true)
        {
            data["HasSignUpLists"] = true;
            data["SignUpListsUrl"] = _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#sign-ups";  // Phase 6A.87+ Fix: Correct anchor
        }
        else
        {
            data["HasSignUpLists"] = false;
        }

        // Phase 6A.83 Part 3: REVERT - Use OrganizerContact* parameters (templates expect these exact names)
        if (@event.HasOrganizerContact())
        {
            data["HasOrganizerContact"] = true;
            data["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";

            if (!string.IsNullOrWhiteSpace(@event.OrganizerContactEmail))
                data["OrganizerContactEmail"] = @event.OrganizerContactEmail;

            if (!string.IsNullOrWhiteSpace(@event.OrganizerContactPhone))
                data["OrganizerContactPhone"] = @event.OrganizerContactPhone;
        }
        else
        {
            data["HasOrganizerContact"] = false;
        }

        return data;
    }

    /// <summary>
    /// Phase 6A.61+: Safely extracts event location string with defensive null handling.
    /// Matches EventPublishedEventHandler pattern for consistency.
    /// </summary>
    private string GetEventLocationString(LankaConnect.Products.LankaEvents.Domain.Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        var state = @event.Location.Address.State;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

        return string.Join(", ", parts);
    }
}
