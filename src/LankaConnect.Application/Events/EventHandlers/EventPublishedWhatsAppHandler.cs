using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a new event is published.
/// Parallel to EventPublishedEventHandler (email). Email handler is UNTOUCHED.
/// Uses BroadcastToEventAttendeesAsync — but since this is a NEW event announcement,
/// we broadcast to all users opted in for NewEvent notifications (handled by WhatsAppService).
/// NOTE: For new event announcements, there are no "attendees" yet. The WhatsApp service
/// will need to handle this by checking user preferences for NewEvent notification type.
/// For Phase 7A.3, we log this as a placeholder — full broadcast logic comes with background jobs.
/// </summary>
public class EventPublishedWhatsAppHandler : INotificationHandler<DomainEventNotification<EventPublishedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventPublishedWhatsAppHandler> _logger;

    public EventPublishedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EventPublishedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<EventPublishedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp EventPublished START: EventId={EventId}, PublishedBy={PublishedBy}",
            eventId, domainEvent.PublishedBy);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp EventPublished: Event not found - EventId={EventId}", eventId);
                    return;
                }

                // Phase 8YA.2 (Q1=A): TBD events can be Published. Skip the WhatsApp
                // broadcast — Twilio approved templates have a required {{EventDate}}
                // parameter and substituting "Date TBD" yields an unprofessional
                // notification. Once the organiser sets dates and re-publishes (or we
                // add a SetDates → re-broadcast hook in a later phase), recipients
                // get a notification with a real date.
                if (!@event.StartDate.HasValue)
                {
                    _logger.LogInformation(
                        "[Phase 8YA.2] WhatsApp EventPublished SKIPPED: TBD event has no " +
                        "confirmed start date - EventId={EventId}.",
                        eventId);
                    return;
                }

                var location = @event.Location?.Address != null
                    ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}".Trim(',', ' ')
                    : "Online Event";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, "Community Member" },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Common.EventDate, EmailDateTimeHelper.FormatEventDate(@event.StartDate, @event.TimeZoneId) },
                    { WhatsAppTemplateContract.Common.EventLocation, location },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                // Broadcast to all opted-in users for this event
                // NOTE: BroadcastToEventAttendeesAsync checks WhatsAppNotificationType.NewEvent
                var result = await whatsAppService.BroadcastToEventAttendeesAsync(
                    eventId,
                    WhatsAppTemplateContract.TemplateNames.NewEventAnnouncement,
                    parameters,
                    WhatsAppNotificationType.NewEvent,
                    CancellationToken.None);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp EventPublished BROADCAST: EventId={EventId}, SentCount={Count}", eventId, result.Value);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp EventPublished FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp EventPublished EXCEPTION: EventId={EventId}", eventId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
