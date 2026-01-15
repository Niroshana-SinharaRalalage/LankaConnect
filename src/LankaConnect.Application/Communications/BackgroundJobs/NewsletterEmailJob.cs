using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NewsletterEmailJob> _logger;

    public NewsletterEmailJob(
        INewsletterRepository newsletterRepository,
        IEventRepository eventRepository,
        INewsletterRecipientService recipientService,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<NewsletterEmailJob> logger)
    {
        _newsletterRepository = newsletterRepository;
        _eventRepository = eventRepository;
        _recipientService = recipientService;
        _emailService = emailService;
        _urlsService = urlsService;
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

            if (newsletter.SentAt.HasValue)
            {
                _logger.LogWarning(
                    "[Phase 6A.74] Newsletter {NewsletterId} has already been sent at {SentAt}. Skipping email job.",
                    newsletterId, newsletter.SentAt);
                return;
            }

            _logger.LogInformation("[Phase 6A.74] Retrieved newsletter {NewsletterId} ({Title}) in {ElapsedMs}ms",
                newsletterId, newsletter.Title.Value, stopwatch.ElapsedMilliseconds);

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

            if (newsletter.EventId.HasValue)
            {
                var @event = await _eventRepository.GetByIdAsync(newsletter.EventId.Value, CancellationToken.None);
                if (@event != null)
                {
                    eventTitle = @event.Title.Value;
                    eventDate = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);
                    eventLocation = GetEventLocationString(@event);
                }
            }

            // 4. Prepare template parameters
            // Phase 6A.74 Part 10 Issue #3: Template uses 'NewsletterContent', not 'NewsletterDescription'
            var parameters = new Dictionary<string, object>
            {
                ["NewsletterTitle"] = newsletter.Title.Value,
                ["NewsletterContent"] = newsletter.Description.Value, // Template expects 'NewsletterContent'
                ["DashboardUrl"] = _urlsService.FrontendBaseUrl,
                ["IsEventNewsletter"] = newsletter.EventId.HasValue,
                ["EventId"] = newsletter.EventId?.ToString() ?? "",
                ["EventTitle"] = eventTitle ?? "",
                ["EventDate"] = eventDate ?? "",
                ["EventLocation"] = eventLocation ?? "",
                ["EventDetailsUrl"] = newsletter.EventId.HasValue ? $"{_urlsService.FrontendBaseUrl}/events/{newsletter.EventId}" : "",
                ["HasSignUpLists"] = false, // TODO: Check if event has sign-up lists
                ["SignUpListsUrl"] = newsletter.EventId.HasValue ? $"{_urlsService.FrontendBaseUrl}/events/{newsletter.EventId}#signup-lists" : ""
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
                        "newsletter",
                        email,
                        parameters,
                        CancellationToken.None);

                    singleEmailStopwatch.Stop();

                    if (result.IsSuccess)
                    {
                        successCount++;
                        _logger.LogDebug("[Phase 6A.74] Successfully sent newsletter email to {Email} in {ElapsedMs}ms",
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

            // 6. Mark newsletter as sent
            var markResult = newsletter.MarkAsSent();
            if (markResult.IsFailure)
            {
                _logger.LogError(
                    "[Phase 6A.74] Failed to mark newsletter {NewsletterId} as sent: {Error}",
                    newsletterId, markResult.Error);
            }
            else
            {
                await _unitOfWork.CommitAsync(CancellationToken.None);
                _logger.LogInformation(
                    "[Phase 6A.74] Newsletter {NewsletterId} marked as sent at {SentAt}",
                    newsletterId, newsletter.SentAt);
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
