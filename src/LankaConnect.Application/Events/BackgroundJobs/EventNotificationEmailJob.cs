using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
            // Phase 6A.83 Part 3: Build base template data (UserName will be added per-recipient in loop)
            var baseTemplateData = BuildTemplateData(@event);

            // Phase 6A.61+ RCA: Diagnostic logging using LogError to bypass log filtering
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] STARTING EMAIL SEND - Template: event-details, RecipientCount: {RecipientCount}, EventTitle: {EventTitle}",
                correlationId, recipients.Count, @event.Title.Value);

            // Log template data for debugging
            _logger.LogError("[DIAG-NOTIF-JOB][{CorrelationId}] Template Data Keys: {Keys}",
                correlationId, string.Join(", ", baseTemplateData.Keys));

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

                    // Phase 6A.83 Part 3: Create per-recipient template data with personalized UserName
                    var recipientTemplateData = new Dictionary<string, object>(baseTemplateData);
                    var emailResult = Domain.Shared.ValueObjects.Email.Create(email);
                    var user = emailResult.IsSuccess
                        ? await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken)
                        : null;
                    recipientTemplateData["UserName"] = user != null ? $"{user.FirstName} {user.LastName}" : "Valued Guest";

                    var result = await _emailService.SendTemplatedEmailAsync(
                        EmailTemplateNames.EventDetails,
                        email,
                        recipientTemplateData,
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

            await _unitOfWork.ClearChangeTrackerExceptAsync<Domain.Events.Entities.EventNotificationHistory>(cancellationToken);

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
