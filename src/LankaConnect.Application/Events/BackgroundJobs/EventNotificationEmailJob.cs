using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.61: Background job to send manual event notification emails
/// Consolidates recipients from email groups, registrations, and newsletter subscribers
/// </summary>
public class EventNotificationEmailJob
{
    private readonly IEventNotificationHistoryRepository _historyRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EventNotificationEmailJob> _logger;

    public EventNotificationEmailJob(
        IEventNotificationHistoryRepository historyRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEventNotificationRecipientService recipientService,
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailUrlHelper emailUrlHelper,
        IUnitOfWork unitOfWork,
        ILogger<EventNotificationEmailJob> logger)
    {
        _historyRepository = historyRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _recipientService = recipientService;
        _userRepository = userRepository;
        _emailService = emailService;
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
        var correlationId = Guid.NewGuid().ToString()[..8];
        _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Starting event notification job for history {HistoryId}",
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

            // Phase 6A.61+ FIX: Use trackChanges: false to properly load email groups from junction table
            // Background jobs don't need change tracking - this ensures .Include("_emailGroupEntities") works correctly
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

                var userEmails = await _userRepository.GetEmailsByUserIdsAsync(userIds, cancellationToken);

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
            var templateData = BuildTemplateData(@event);

            // Phase 6A.61+ RCA: Diagnostic logging using LogError to bypass log filtering
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] STARTING EMAIL SEND - Template: event-details, RecipientCount: {RecipientCount}, EventTitle: {EventTitle}",
                correlationId, recipients.Count, @event.Title.Value);

            // Log template data for debugging
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] Template Data Keys: {Keys}",
                correlationId, string.Join(", ", templateData.Keys));

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Sending to {RecipientCount} recipients",
                correlationId, recipients.Count);

            // 6. Send emails
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

                    var result = await _emailService.SendTemplatedEmailAsync(
                        "event-details",
                        email,
                        templateData,
                        cancellationToken);

                    if (result.IsSuccess)
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
                            correlationId, emailIndex, recipients.Count, email, result.Error ?? "Unknown error");
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
            // Phase 6A.61+ FIX: Reload entity to get fresh version and avoid DbUpdateConcurrencyException
            // The original entity loaded at the start may have stale UpdatedAt after minutes of email sending
            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Reloading history {HistoryId} to get fresh version before final update",
                correlationId, historyId);

            var freshHistory = await _historyRepository.GetByIdAsync(historyId, cancellationToken);
            if (freshHistory == null)
            {
                _logger.LogError("[Phase 6A.61][{CorrelationId}] History record {HistoryId} not found for final update",
                    correlationId, historyId);
                return;
            }

            // Phase 6A.61+ Idempotency Check: If statistics already updated (SuccessfulSends > 0), skip to avoid duplicate sends
            // This prevents concurrent Hangfire retries from sending the same emails multiple times
            if (freshHistory.SuccessfulSends > 0 || freshHistory.FailedSends > 0)
            {
                _logger.LogInformation(
                    "[Phase 6A.61][{CorrelationId}] History {HistoryId} already has statistics (Success: {Success}, Failed: {Failed}). " +
                    "Another job execution already completed. Skipping commit to avoid concurrency exception.",
                    correlationId, historyId, freshHistory.SuccessfulSends, freshHistory.FailedSends);
                return; // Exit successfully - another concurrent execution handled the email send
            }

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Updating history statistics - Recipients: {Recipients}, Success: {Success}, Failed: {Failed}",
                correlationId, recipients.Count, successCount, failedCount);

            freshHistory.UpdateSendStatistics(recipients.Count, successCount, failedCount);
            _historyRepository.Update(freshHistory);

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
                _logger.LogWarning(ex,
                    "[Phase 6A.61][{CorrelationId}] CONCURRENCY EXCEPTION when committing history {HistoryId}. " +
                    "This likely means another concurrent Hangfire retry already updated the record. " +
                    "Checking if another job execution succeeded...",
                    correlationId, historyId);

                // Phase 6A.61+ CRITICAL FIX: Don't immediately retry on concurrency exception
                // Instead, reload the entity to check if another concurrent execution already succeeded
                var reloadedHistory = await _historyRepository.GetByIdAsync(historyId, cancellationToken);
                if (reloadedHistory != null && (reloadedHistory.SuccessfulSends > 0 || reloadedHistory.FailedSends > 0))
                {
                    _logger.LogInformation(
                        "[Phase 6A.61][{CorrelationId}] Verified that another concurrent job execution already committed statistics " +
                        "(Success: {Success}, Failed: {Failed}). This job can exit successfully - no retry needed.",
                        correlationId, reloadedHistory.SuccessfulSends, reloadedHistory.FailedSends);
                    return; // Exit successfully - another execution handled the commit
                }

                // If the entity still has no statistics, this is a real concurrency conflict that needs retry
                _logger.LogError(ex,
                    "[Phase 6A.61][{CorrelationId}] CONCURRENCY EXCEPTION and no other job succeeded yet. " +
                    "History: {HistoryId}, Recipients: {Recipients}, Success: {Success}, Failed: {Failed}. Rethrowing for Hangfire retry.",
                    correlationId, historyId, recipients.Count, successCount, failedCount);
                throw; // Re-throw for Hangfire retry
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 6A.61][{CorrelationId}] UNEXPECTED EXCEPTION when committing history {HistoryId}. " +
                    "Exception Type: {ExceptionType}, Message: {Message}",
                    correlationId, historyId, ex.GetType().FullName, ex.Message);
                throw; // Re-throw for Hangfire retry
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.61][{CorrelationId}] Fatal error in notification job", correlationId);
            throw;
        }
    }

    private Dictionary<string, object> BuildTemplateData(Domain.Events.Event @event)
    {
        var isFree = @event.IsFree();

        // Phase 6A.61+: Include ALL fields from event-published template for consistency
        // This ensures event-details template can reuse the same rich template as event-published
        var data = new Dictionary<string, object>
        {
            // Core fields (original)
            { "EventTitle", @event.Title?.Value ?? "Untitled Event" },
            { "EventDate", @event.StartDate.ToString("f") }, // Full date/time pattern (e.g., "Monday, December 25, 2025 7:00 PM")
            { "EventLocation", GetEventLocationString(@event) },
            { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
            { "IsFreeEvent", isFree },
            { "PricingDetails", isFree ? "Free" : $"${@event.TicketPrice?.Amount ?? 0:F2}" },

            // Phase 6A.61+: Add event-published fields for rich template compatibility
            { "EventDescription", @event.Description?.Value ?? "" },
            { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") }, // e.g., "December 25, 2025"
            { "EventStartTime", @event.StartDate.ToString("h:mm tt") }, // e.g., "7:00 PM"
            { "EventCity", @event.Location?.Address.City ?? "TBA" },
            { "EventState", @event.Location?.Address.State ?? "TBA" },
            { "EventUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) }, // Alias for EventDetailsUrl
            { "IsFree", isFree }, // event-published uses this name
            { "IsPaid", !isFree }, // event-published conditional
            { "TicketPrice", isFree ? "Free" : @event.TicketPrice?.Amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) ?? "TBA" }
        };

        // Add sign-up lists URL if available
        if (@event.SignUpLists?.Any() == true)
        {
            data["HasSignUpLists"] = true;
            data["SignUpListsUrl"] = _emailUrlHelper.BuildEventDetailsUrl(@event.Id) + "#signup-lists";
        }
        else
        {
            data["HasSignUpLists"] = false;
        }

        // Add organizer contact if opted in
        if (@event.HasOrganizerContact())
        {
            data["HasOrganizerContact"] = true;
            data["OrganizerName"] = @event.OrganizerContactName ?? "Event Organizer";

            if (!string.IsNullOrWhiteSpace(@event.OrganizerContactEmail))
                data["OrganizerEmail"] = @event.OrganizerContactEmail;

            if (!string.IsNullOrWhiteSpace(@event.OrganizerContactPhone))
                data["OrganizerPhone"] = @event.OrganizerContactPhone;
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
    private string GetEventLocationString(Domain.Events.Event @event)
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
