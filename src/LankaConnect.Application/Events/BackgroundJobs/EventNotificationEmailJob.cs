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

            var @event = await _eventRepository.GetByIdAsync(history.EventId, cancellationToken);
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

            // 3. Add confirmed registrations
            var registrations = await _registrationRepository.GetByEventAsync(history.EventId, cancellationToken);
            foreach (var reg in registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
            {
                var user = await _userRepository.GetByIdAsync(reg.UserId!.Value, cancellationToken);
                if (user != null)
                {
                    recipients.Add(user.Email.Value);
                }
            }

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Total recipients after adding registrations: {Count}",
                correlationId, recipients.Count);

            // 4. Update history record with recipient count
            history.UpdateSendStatistics(recipients.Count, 0, 0); // Initialize counts
            _historyRepository.Update(history);
            await _unitOfWork.CommitAsync(cancellationToken);

            // 5. Build template parameters
            var templateData = BuildTemplateData(@event);

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Sending to {RecipientCount} recipients",
                correlationId, recipients.Count);

            // 6. Send emails
            int successCount = 0, failedCount = 0;
            foreach (var email in recipients)
            {
                try
                {
                    var result = await _emailService.SendTemplatedEmailAsync(
                        "event-details",
                        email,
                        templateData,
                        cancellationToken);

                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedCount++;
                        _logger.LogWarning("[Phase 6A.61][{CorrelationId}] Failed to send to {Email}: {Error}",
                            correlationId, email, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Phase 6A.61][{CorrelationId}] Exception sending to {Email}", correlationId, email);
                    failedCount++;
                }
            }

            // 7. Update history record with final statistics
            history.UpdateSendStatistics(recipients.Count, successCount, failedCount);
            _historyRepository.Update(history);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Phase 6A.61][{CorrelationId}] Completed. Success: {Success}, Failed: {Failed}",
                correlationId, successCount, failedCount);
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
        var data = new Dictionary<string, object>
        {
            { "EventTitle", @event.Title.Value },
            { "EventDate", @event.StartDate.ToString("f") }, // Full date/time pattern
            { "EventLocation", @event.Location?.Address.ToString() ?? "Location TBD" },
            { "EventDetailsUrl", _emailUrlHelper.BuildEventDetailsUrl(@event.Id) },
            { "IsFreeEvent", isFree },
            { "PricingDetails", isFree ? "Free" : $"${@event.TicketPrice?.Amount ?? 0:F2}" }
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
}
