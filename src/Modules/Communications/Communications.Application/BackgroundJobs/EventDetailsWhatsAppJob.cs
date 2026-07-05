using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Communications.Application.BackgroundJobs;

/// <summary>
/// Phase 7A.3: Background job that sends WhatsApp event details update notifications.
/// Parallel to EventDetailsUpdateEmailJob (email). Email job is UNTOUCHED.
/// Triggered manually by admins when event details change.
/// </summary>
public class EventDetailsWhatsAppJob
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventDetailsWhatsAppJob> _logger;

    public EventDetailsWhatsAppJob(
        IWhatsAppService whatsAppService,
        IEventRepository eventRepository,
        ILogger<EventDetailsWhatsAppJob> logger)
    {
        _whatsAppService = whatsAppService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid eventId)
    {
        _logger.LogInformation("[Phase 7A] WhatsApp EventDetailsUpdate START: EventId={EventId}", eventId);

        try
        {
            var @event = await _eventRepository.GetByIdAsync(eventId, CancellationToken.None);
            if (@event == null)
            {
                _logger.LogWarning("[Phase 7A] WhatsApp EventDetailsUpdate: Event not found - EventId={EventId}", eventId);
                return;
            }

            var location = @event.Location?.Address != null
                ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}".Trim(',', ' ')
                : "Online Event";

            var parameters = new Dictionary<string, string>
            {
                { WhatsAppTemplateContract.Common.UserName, "Attendee" },
                { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                { WhatsAppTemplateContract.Common.EventDate, EmailDateTimeHelper.FormatEventDate(@event.StartDate, @event.TimeZoneId) },
                { WhatsAppTemplateContract.Common.EventTime, EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId) },
                { WhatsAppTemplateContract.Common.EventLocation, location },
                { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
            };

            var result = await _whatsAppService.BroadcastToEventAttendeesAsync(
                eventId,
                WhatsAppTemplateContract.TemplateNames.EventDetailsUpdate,
                parameters,
                WhatsAppNotificationType.EventUpdate,
                CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Phase 7A] WhatsApp EventDetailsUpdate SENT: EventId={EventId}, SentCount={Count}", eventId, result.Value);
            }
            else
            {
                _logger.LogWarning("[Phase 7A] WhatsApp EventDetailsUpdate FAILED: {Errors}", string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 7A] WhatsApp EventDetailsUpdate EXCEPTION: EventId={EventId}", eventId);
        }
    }
}
