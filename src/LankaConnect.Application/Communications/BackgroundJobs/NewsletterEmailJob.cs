using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.BackgroundJobs;

/// <summary>
/// Phase 6A.74: Background job that sends newsletter emails to all recipients.
/// This job is queued by SendNewsletterCommand and executes asynchronously outside the HTTP request context.
///
/// Recipients include:
/// 1. Email group members
/// 2. Newsletter subscribers (with location targeting: metro → state → all locations)
///
/// Performance: Sends emails to unlimited recipients without blocking the API response.
/// Retry: Hangfire automatically retries failed jobs (default: 10 attempts with exponential backoff).
/// Monitoring: View job status and failures in Hangfire Dashboard (/hangfire).
/// </summary>
public class NewsletterEmailJob
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IEventRepository _eventRepository;
    private readonly INewsletterRecipientService _recipientService;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NewsletterEmailJob> _logger;

    public NewsletterEmailJob(
        INewsletterRepository newsletterRepository,
        IEventRepository eventRepository,
        INewsletterRecipientService recipientService,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<NewsletterEmailJob> logger)
    {
        _newsletterRepository = newsletterRepository;
        _eventRepository = eventRepository;
        _recipientService = recipientService;
        _emailService = emailService;
        _urlsService = urlsService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background job to send newsletter emails.
    /// This method is called by Hangfire outside the HTTP request context.
    /// </summary>
    /// <param name="newsletterId">The ID of the newsletter to send</param>
    public async Task ExecuteAsync(Guid newsletterId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation(
            "[Phase 6A.74] NewsletterEmailJob STARTED - Newsletter {NewsletterId}",
            newsletterId);

        try
        {
            // 1. Retrieve newsletter data
            var newsletter = await _newsletterRepository.GetByIdAsync(newsletterId, CancellationToken.None);
            if (newsletter == null)
            {
                _logger.LogWarning("[Phase 6A.74] Newsletter {NewsletterId} not found, skipping email job", newsletterId);
                return;
            }

            if (newsletter.Status != NewsletterStatus.Active)
            {
                _logger.LogWarning(
                    "[Phase 6A.74] Newsletter {NewsletterId} status is {Status}, expected Active. Skipping email job.",
                    newsletterId, newsletter.Status);
                return;
            }

            // Phase 6A.74 Part 14: REMOVED SentAt check - newsletters can send unlimited emails
            // Old behavior blocked resending after first send, new behavior allows unlimited sends
            // SentAt is only used to track the first send timestamp for historical purposes
            if (newsletter.SentAt.HasValue)
            {
                _logger.LogInformation(
                    "[Phase 6A.74 Part 14] Newsletter {NewsletterId} was previously sent at {SentAt}. " +
                    "Allowing resend - unlimited email sends are now supported.",
                    newsletterId, newsletter.SentAt);
            }

            _logger.LogInformation("[Phase 6A.74] Retrieved newsletter {NewsletterId} ({Title}) in {ElapsedMs}ms",
                newsletterId, newsletter.Title?.Value ?? "Untitled Newsletter", stopwatch.ElapsedMilliseconds);

            // 2. Resolve recipients (email groups + newsletter subscribers with location targeting)
            var recipientStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var recipients = await _recipientService.ResolveRecipientsAsync(
                newsletterId,
                CancellationToken.None);

            _logger.LogInformation(
                "[Phase 6A.74] Resolved {Count} newsletter recipients in {ElapsedMs}ms. " +
                "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
                recipients.TotalRecipients, recipientStopwatch.ElapsedMilliseconds,
                recipients.Breakdown.EmailGroupCount,
                recipients.Breakdown.MetroAreaSubscribers,
                recipients.Breakdown.StateLevelSubscribers,
                recipients.Breakdown.AllLocationsSubscribers);

            if (recipients.TotalRecipients == 0)
            {
                _logger.LogInformation("[Phase 6A.74] No recipients found for Newsletter {NewsletterId}, skipping email job",
                    newsletterId);
                return;
            }

            // 3. Get event details if newsletter is linked to an event
            string? eventTitle = null;
            string? eventDate = null;
            string? eventLocation = null;
            bool hasSignUpLists = false;

            if (newsletter.EventId.HasValue)
            {
                var @event = await _eventRepository.GetByIdAsync(newsletter.EventId.Value, CancellationToken.None);
                if (@event != null)
                {
                    eventTitle = @event.Title?.Value ?? "Untitled Event";
                    eventDate = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);
                    eventLocation = GetEventLocationString(@event);
                    hasSignUpLists = @event.HasSignUpLists();

                    _logger.LogInformation(
                        "[Phase 6A.74] Event details for newsletter {NewsletterId}: Title={EventTitle}, HasSignUpLists={HasSignUpLists}",
                        newsletterId, eventTitle, hasSignUpLists);
                }
            }

            // 4. Prepare template parameters
            // Phase 6A.74 Part 10 Issue #3: Template uses 'NewsletterContent', not 'NewsletterDescription'
            // Phase 6A.74 Part 14 Fix: EventId must be truthy for Handlebars {{#if EventId}} - use newsletter.EventId.HasValue
            var parameters = new Dictionary<string, object>
            {
                ["NewsletterTitle"] = newsletter.Title?.Value ?? "Untitled Newsletter",
                ["NewsletterContent"] = newsletter.Description?.Value ?? "No content", // Template expects 'NewsletterContent'
                ["DashboardUrl"] = _urlsService.FrontendBaseUrl,
                ["IsEventNewsletter"] = newsletter.EventId.HasValue,
                ["EventId"] = newsletter.EventId.HasValue ? newsletter.EventId.Value : (object)false, // Handlebars {{#if}} needs truthy/falsy
                ["EventTitle"] = eventTitle ?? "",
                ["EventDate"] = eventDate ?? "",
                ["EventLocation"] = eventLocation ?? "",
                ["EventDetailsUrl"] = newsletter.EventId.HasValue ? $"{_urlsService.FrontendBaseUrl}/events/{newsletter.EventId}" : "",
                ["HasSignUpLists"] = hasSignUpLists, // Phase 6A.74 Part 14 Fix: Actually check if event has sign-up lists
                ["SignUpListsUrl"] = newsletter.EventId.HasValue ? $"{_urlsService.FrontendBaseUrl}/events/{newsletter.EventId}#sign-ups" : ""
            };

            // 5. Send templated email to each recipient
            var successCount = 0;
            var failCount = 0;
            var emailStopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("[Phase 6A.74] Starting email send to {Count} recipients...", recipients.TotalRecipients);

            foreach (var email in recipients.EmailAddresses)
            {
                try
                {
                    var singleEmailStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    var result = await _emailService.SendTemplatedEmailAsync(
                        "template-newsletter-notification",
                        email,
                        parameters,
                        CancellationToken.None);

                    singleEmailStopwatch.Stop();

                    if (result.IsSuccess)
                    {
                        successCount++;
                        _logger.LogInformation("[Phase 6A.74] Successfully sent newsletter email to {Email} in {ElapsedMs}ms",
                            email, singleEmailStopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning(
                            "[Phase 6A.74] Failed to send newsletter email to {Email} for newsletter {NewsletterId} (took {ElapsedMs}ms): {Errors}",
                            email, newsletterId, singleEmailStopwatch.ElapsedMilliseconds, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception emailEx)
                {
                    failCount++;
                    _logger.LogError(emailEx,
                        "[Phase 6A.74] Exception sending newsletter email to {Email} for newsletter {NewsletterId}",
                        email, newsletterId);
                }
            }

            emailStopwatch.Stop();

            _logger.LogInformation(
                "[Phase 6A.74] Newsletter emails completed for newsletter {NewsletterId} in {TotalElapsedMs}ms. " +
                "Success: {SuccessCount}, Failed: {FailCount}, Avg time per email: {AvgMs}ms",
                newsletterId, emailStopwatch.ElapsedMilliseconds, successCount, failCount,
                recipients.TotalRecipients > 0 ? emailStopwatch.ElapsedMilliseconds / recipients.TotalRecipients : 0);

            // 6. Record email send and create history
            // Phase 6A.74 Part 14: Changed from MarkAsSent() to RecordEmailSent()
            // - RecordEmailSent() only sets SentAt timestamp on first send, does NOT change status
            // - Newsletter remains Active and can send more emails
            // Phase 6A.74 Hotfix: Reload newsletter entity to get latest version and avoid concurrency exception
            _logger.LogInformation("[Phase 6A.74 Part 14] Reloading newsletter {NewsletterId} to get latest version before recording email send", newsletterId);

            var freshNewsletter = await _newsletterRepository.GetByIdAsync(newsletterId, CancellationToken.None);
            if (freshNewsletter == null)
            {
                _logger.LogError(
                    "[Phase 6A.74] Newsletter {NewsletterId} not found when reloading for RecordEmailSent",
                    newsletterId);
                return;
            }

            // Phase 6A.74 Part 14: Call RecordEmailSent() instead of MarkAsSent()
            // This only sets SentAt on first send but does NOT change status to Sent
            var recordResult = freshNewsletter.RecordEmailSent();
            if (recordResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Phase 6A.74 Part 14] Could not record email send for newsletter {NewsletterId}: {Error}. " +
                    "Continuing anyway - emails were sent successfully.",
                    newsletterId, recordResult.Error);
                // Don't throw - emails were sent, just couldn't update the record
            }

            // Always create history record, even for resends
            {
                // Phase 6A.74 Part 13+ Issue #1 BUGFIX: Create NewsletterEmailHistory AFTER marking newsletter as sent
                // This ensures both entities are tracked by the same DbContext and committed together
                _logger.LogInformation(
                    "[Phase 6A.74 Part 13+] Creating newsletter email history for newsletter {NewsletterId}. " +
                    "Total: {TotalRecipients}, NewsletterGroups: {NewsletterEmailGroupCount}, EventGroups: {EventEmailGroupCount}, " +
                    "Subscribers: {SubscriberCount}, EventRegistrations: {EventRegistrationCount}, Success: {SuccessCount}, Failed: {FailedCount}",
                    newsletterId,
                    recipients.TotalRecipients,
                    recipients.Breakdown.NewsletterEmailGroupCount,
                    recipients.Breakdown.EventEmailGroupCount,
                    recipients.Breakdown.SubscriberCount,
                    recipients.Breakdown.EventRegistrationCount,
                    successCount,
                    failCount);

                var newsletterHistory = NewsletterEmailHistory.Create(
                    newsletterId,
                    DateTime.UtcNow,
                    recipients.TotalRecipients,
                    recipients.Breakdown.NewsletterEmailGroupCount,
                    recipients.Breakdown.EventEmailGroupCount,
                    recipients.Breakdown.SubscriberCount,
                    recipients.Breakdown.EventRegistrationCount,
                    successCount,
                    failCount);

                var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext;
                if (dbContext != null)
                {
                    await dbContext.Set<NewsletterEmailHistory>().AddAsync(newsletterHistory);
                    _logger.LogInformation(
                        "[Phase 6A.74 Part 13 Issue #1] Added NewsletterEmailHistory record {HistoryId} to DbContext for newsletter {NewsletterId}",
                        newsletterHistory.Id,
                        newsletterId);
                }
                else
                {
                    _logger.LogWarning(
                        "[Phase 6A.74 Part 13 Issue #1] Unable to cast IApplicationDbContext to DbContext. NewsletterEmailHistory not persisted.");
                }

                // Phase 6A.74 Part 13 CRITICAL FIX (RCA Issue #1/#2): Detach EmailMessage entities
                // The email sending loop creates and tracks EmailMessage entities in the same DbContext
                // If we don't detach them, EF Core will try to save ALL tracked entities (including EmailMessages)
                // causing DbUpdateConcurrencyException when their timestamps have changed
                // Unlike Event notifications (which only update History), Newsletter needs to save BOTH
                // Newsletter entity (MarkAsSent) AND NewsletterEmailHistory, so we manually detach only EmailMessages
                _logger.LogInformation("[Phase 6A.74 Part 13 Issue #1/#2 RCA] Detaching EmailMessage entities before commit, keeping Newsletter and NewsletterEmailHistory tracked");

                if (dbContext != null)
                {
                    var emailMessageEntries = dbContext.ChangeTracker.Entries()
                        .Where(e => e.Entity is Domain.Communications.Entities.EmailMessage)
                        .ToList();

                    _logger.LogInformation("[Phase 6A.74 Part 13 Issue #1/#2 RCA] Found {Count} EmailMessage entities to detach",
                        emailMessageEntries.Count);

                    foreach (var entry in emailMessageEntries)
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }

                    _logger.LogInformation("[Phase 6A.74 Part 13 Issue #1/#2 RCA] Detached {Count} EmailMessage entities, Newsletter and NewsletterEmailHistory still tracked",
                        emailMessageEntries.Count);
                }

                try
                {
                    _logger.LogInformation(
                        "[Phase 6A.74] Attempting to commit newsletter {NewsletterId} as sent (with history record). Current version: {Version}",
                        newsletterId, freshNewsletter.GetType().GetProperty("Version", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(freshNewsletter));

                    await _unitOfWork.CommitAsync(CancellationToken.None);

                    _logger.LogInformation(
                        "[Phase 6A.74] Newsletter {NewsletterId} marked as sent at {SentAt} and history record persisted",
                        newsletterId, freshNewsletter.SentAt);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex,
                        "[Phase 6A.74] CONCURRENCY EXCEPTION when committing newsletter {NewsletterId}. " +
                        "This likely means another concurrent Hangfire retry already updated the record. " +
                        "Checking if another job execution succeeded...",
                        newsletterId);

                    // Phase 6A.74 CRITICAL FIX: Don't immediately retry on concurrency exception
                    // Instead, reload the entity to check if another concurrent execution already succeeded
                    var reloadedNewsletter = await _newsletterRepository.GetByIdAsync(newsletterId, CancellationToken.None);
                    if (reloadedNewsletter != null && reloadedNewsletter.SentAt.HasValue)
                    {
                        _logger.LogInformation(
                            "[Phase 6A.74] Verified that another concurrent job execution already marked newsletter as sent " +
                            "(SentAt: {SentAt}). This job can exit successfully - no retry needed.",
                            reloadedNewsletter.SentAt.Value);
                        return; // Exit successfully - another execution handled the commit
                    }

                    // If the entity still has no SentAt, this is a real concurrency conflict that needs retry
                    _logger.LogError(ex,
                        "[Phase 6A.74] CONCURRENCY EXCEPTION and no other job succeeded yet. Newsletter: {NewsletterId}. Rethrowing for Hangfire retry.",
                        newsletterId);
                    throw; // Re-throw for Hangfire retry
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[Phase 6A.74] UNEXPECTED EXCEPTION when committing newsletter {NewsletterId}. " +
                        "Exception Type: {ExceptionType}, Message: {Message}",
                        newsletterId, ex.GetType().FullName, ex.Message);
                    throw; // Re-throw for Hangfire retry
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[Phase 6A.74] NewsletterEmailJob COMPLETED for newsletter {NewsletterId}. Total time: {TotalMs}ms",
                newsletterId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Let exception bubble up so Hangfire can retry the job
            _logger.LogError(ex,
                "[Phase 6A.74] FATAL ERROR in NewsletterEmailJob for Newsletter {NewsletterId}. Hangfire will retry.",
                newsletterId);
            throw; // Re-throw for Hangfire retry mechanism
        }
    }

    /// <summary>
    /// Safely extracts event location string with defensive null handling.
    /// </summary>
    private static string GetEventLocationString(Domain.Events.Event @event)
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
