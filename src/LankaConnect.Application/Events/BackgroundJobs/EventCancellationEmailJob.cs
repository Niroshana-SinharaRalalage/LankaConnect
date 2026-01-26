using System.Diagnostics;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.64: Background job that sends event cancellation emails to all recipients.
/// This job is queued by EventCancelledEventHandler and executes asynchronously outside the HTTP request context.
///
/// Recipients include:
/// 1. Confirmed registrations (user accounts only)
/// 2. Event email groups
/// 3. Location-matched newsletter subscribers (metro → state → all locations)
/// 4. Sign-up list committed users (Phase 6A.75)
///
/// Performance: Sends emails to unlimited recipients without blocking the API response.
/// Retry: Hangfire automatically retries failed jobs (default: 10 attempts with exponential backoff).
/// Monitoring: View job status and failures in Hangfire Dashboard (/hangfire).
/// </summary>
public class EventCancellationEmailJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventNotificationRecipientService _recipientService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly ILogger<EventCancellationEmailJob> _logger;

    public EventCancellationEmailJob(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEventNotificationRecipientService recipientService,
        IUserRepository userRepository,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        ILogger<EventCancellationEmailJob> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _recipientService = recipientService;
        _userRepository = userRepository;
        _emailService = emailService;
        _urlsService = urlsService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background job to send event cancellation emails.
    /// This method is called by Hangfire outside the HTTP request context.
    /// </summary>
    /// <param name="eventId">The ID of the cancelled event</param>
    /// <param name="cancellationReason">The reason for cancellation provided by the organizer</param>
    public async Task ExecuteAsync(Guid eventId, string cancellationReason)
    {
        using (LogContext.PushProperty("Operation", "EventCancellationEmail"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.64] EventCancellationEmailJob START: EventId={EventId}, Reason={Reason}",
                eventId, cancellationReason);

            try
            {
            // 1. Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(eventId, CancellationToken.None);
            if (@event == null)
            {
                _logger.LogWarning("[Phase 6A.64] Event {EventId} not found, skipping email job", eventId);
                return;
            }

            if (@event.Status != EventStatus.Cancelled)
            {
                _logger.LogWarning(
                    "[Phase 6A.64] Event {EventId} status is {Status}, expected Cancelled. Skipping email job.",
                    eventId, @event.Status);
                return;
            }

            _logger.LogInformation("[Phase 6A.64] Retrieved event {EventId} ({EventTitle}) in {ElapsedMs}ms",
                eventId, @event.Title?.Value ?? "Untitled Event", stopwatch.ElapsedMilliseconds);

            // 2. Get confirmed registration emails (bulk query to avoid N+1)
            var registrationStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var registrations = await _registrationRepository.GetByEventAsync(eventId, CancellationToken.None);
            var confirmedRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed && r.UserId.HasValue)
                .ToList();

            _logger.LogInformation("[Phase 6A.64] Retrieved {Count} confirmed registrations in {ElapsedMs}ms",
                confirmedRegistrations.Count, registrationStopwatch.ElapsedMilliseconds);

            var registrationEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (confirmedRegistrations.Any())
            {
                var userIds = confirmedRegistrations
                    .Select(r => r.UserId!.Value)
                    .Distinct()
                    .ToList();

                var bulkQueryStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var userEmails = await _userRepository.GetEmailsByUserIdsAsync(userIds, CancellationToken.None);

                _logger.LogInformation(
                    "[Phase 6A.64] Bulk fetched {Count} user emails in {ElapsedMs}ms",
                    userEmails.Count, bulkQueryStopwatch.ElapsedMilliseconds);

                foreach (var email in userEmails.Values)
                {
                    registrationEmails.Add(email);
                }
            }

            // 3. Phase 6A.75: Get sign-up committed users
            var signUpCommitmentEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var signUpStopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (@event.SignUpLists != null && @event.SignUpLists.Any())
            {
                // Extract all unique user IDs from sign-up commitments
                var signUpUserIds = @event.SignUpLists
                    .SelectMany(sl => sl.Items ?? Enumerable.Empty<Domain.Events.Entities.SignUpItem>())
                    .SelectMany(item => item.Commitments ?? Enumerable.Empty<Domain.Events.Entities.SignUpCommitment>())
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToList();

                _logger.LogInformation(
                    "[Phase 6A.75] Found {Count} unique sign-up committed users in {ElapsedMs}ms",
                    signUpUserIds.Count, signUpStopwatch.ElapsedMilliseconds);

                if (signUpUserIds.Any())
                {
                    var signUpEmailsStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var signUpUserEmails = await _userRepository.GetEmailsByUserIdsAsync(signUpUserIds, CancellationToken.None);

                    _logger.LogInformation(
                        "[Phase 6A.75] Bulk fetched {Count} sign-up user emails in {ElapsedMs}ms",
                        signUpUserEmails.Count, signUpEmailsStopwatch.ElapsedMilliseconds);

                    foreach (var email in signUpUserEmails.Values)
                    {
                        signUpCommitmentEmails.Add(email);
                    }
                }
            }
            else
            {
                _logger.LogInformation("[Phase 6A.75] No sign-up lists found for event {EventId}", eventId);
            }

            // 4. Get email groups + newsletter subscribers
            var recipientStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var notificationRecipients = await _recipientService.ResolveRecipientsAsync(
                eventId,
                CancellationToken.None);

            _logger.LogInformation(
                "[Phase 6A.64] Resolved {Count} notification recipients in {ElapsedMs}ms. " +
                "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
                notificationRecipients.EmailAddresses.Count, recipientStopwatch.ElapsedMilliseconds,
                notificationRecipients.Breakdown.EmailGroupCount,
                notificationRecipients.Breakdown.MetroAreaSubscribers,
                notificationRecipients.Breakdown.StateLevelSubscribers,
                notificationRecipients.Breakdown.AllLocationsSubscribers);

            // 5. Consolidate all recipients (deduplicated, case-insensitive)
            // Phase 6A.75: Now includes sign-up committed users
            var allRecipients = registrationEmails
                .Concat(signUpCommitmentEmails)
                .Concat(notificationRecipients.EmailAddresses)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!allRecipients.Any())
            {
                _logger.LogInformation("[Phase 6A.64] No recipients found for Event {EventId}, skipping email job",
                    eventId);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.75] Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}. " +
                "Breakdown: Registrations={RegCount}, SignUpCommitments={SignUpCount}, EmailGroups+Newsletter={NotificationCount}",
                allRecipients.Count, eventId,
                registrationEmails.Count,
                signUpCommitmentEmails.Count,
                notificationRecipients.EmailAddresses.Count);

            // TEMP DIAGNOSTIC: Log all recipient email addresses
            // TODO-REMOVE: Remove this verbose logging after Phase 6A.70 verification
            _logger.LogInformation(
                "[TEMP-DIAGNOSTIC] Event {EventId} cancellation email recipients: {Recipients}",
                eventId, string.Join(", ", allRecipients.OrderBy(e => e)));

            // 6. Prepare base template parameters
            var baseParameters = new Dictionary<string, object>
            {
                ["EventTitle"] = @event.Title?.Value ?? "Untitled Event",
                ["EventStartDate"] = @event.StartDate.ToString("MMMM dd, yyyy"),
                ["EventStartTime"] = @event.StartDate.ToString("h:mm tt"),
                ["EventLocation"] = GetEventLocationString(@event),
                ["CancellationReason"] = cancellationReason,
                ["RefundInfo"] = @event.IsFree() ? "No refund applicable for free events." : "Refunds will be processed within 5-7 business days.",
                // Phase 6A.83 Part 3: Use OrganizerContact* parameters (template expects these exact names)
                ["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "support@lankaconnect.com",
                ["OrganizerContactName"] = @event.OrganizerContactName ?? "LankaConnect Support",
                ["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? "",
                ["DashboardUrl"] = _urlsService.FrontendBaseUrl
            };

            // 7. Send templated email to each recipient
            // Phase 6A.64: Detailed timing and error tracking for observability
            var successCount = 0;
            var failCount = 0;
            var emailStopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("[Phase 6A.64] Starting email send to {Count} recipients...", allRecipients.Count);

            foreach (var email in allRecipients)
            {
                try
                {
                    var singleEmailStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    var recipientParameters = new Dictionary<string, object>(baseParameters);
                    var emailResult = Email.Create(email);
                    var user = emailResult.IsSuccess
                        ? await _userRepository.GetByEmailAsync(emailResult.Value, CancellationToken.None)
                        : null;
                    recipientParameters["UserName"] = user != null ? $"{user.FirstName} {user.LastName}" : "Valued Guest";

                    var result = await _emailService.SendTemplatedEmailAsync(
                        EmailTemplateNames.EventCancellation,
                        email,
                        recipientParameters,
                        CancellationToken.None);

                    singleEmailStopwatch.Stop();

                    if (result.IsSuccess)
                    {
                        successCount++;
                        // TEMP DIAGNOSTIC: Change from Debug to Information to see in logs
                        // TODO-REMOVE: Change back to LogDebug after Phase 6A.70 verification
                        _logger.LogInformation("[TEMP-DIAGNOSTIC] Successfully sent cancellation email to {Email} in {ElapsedMs}ms",
                            email, singleEmailStopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning(
                            "[Phase 6A.64] Failed to send cancellation email to {Email} for event {EventId} (took {ElapsedMs}ms): {Errors}",
                            email, eventId, singleEmailStopwatch.ElapsedMilliseconds, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception emailEx)
                {
                    failCount++;
                    _logger.LogError(emailEx,
                        "[Phase 6A.64] Exception sending cancellation email to {Email} for event {EventId}",
                        email, eventId);
                }
            }

            emailStopwatch.Stop();

            _logger.LogInformation(
                "[Phase 6A.64] Event cancellation emails completed for event {EventId} in {TotalElapsedMs}ms. " +
                "Success: {SuccessCount}, Failed: {FailCount}, Avg time per email: {AvgMs}ms",
                eventId, emailStopwatch.ElapsedMilliseconds, successCount, failCount,
                allRecipients.Count > 0 ? emailStopwatch.ElapsedMilliseconds / allRecipients.Count : 0);

                stopwatch.Stop();
                _logger.LogInformation(
                    "[Phase 6A.64] EventCancellationEmailJob COMPLETE: Duration={TotalMs}ms, EventId={EventId}",
                    stopwatch.ElapsedMilliseconds, eventId);
            }
            catch (OperationCanceledException) when (CancellationToken.None.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.64] EventCancellationEmailJob CANCELED: Duration={ElapsedMs}ms, EventId={EventId}",
                    stopwatch.ElapsedMilliseconds, eventId);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Let exception bubble up so Hangfire can retry the job
                _logger.LogError(ex,
                    "[Phase 6A.64] EventCancellationEmailJob FAILED: Duration={ElapsedMs}ms, EventId={EventId}",
                    stopwatch.ElapsedMilliseconds, eventId);
                throw; // Re-throw for Hangfire retry mechanism
            }
        }
    }

    /// <summary>
    /// Safely extracts event location string with defensive null handling.
    /// </summary>
    private static string GetEventLocationString(Event @event)
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

    /// <summary>
    /// Formats event date/time range for display.
    /// Examples:
    /// - Same day: "January 31, 2026 from 3:07 AM to 1:10 PM"
    /// - Different days: "January 31, 2026 at 3:07 AM to February 2, 2026 at 1:10 PM"
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
}
