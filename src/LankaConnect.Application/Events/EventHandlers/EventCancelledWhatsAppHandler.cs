using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when an event is cancelled.
/// Parallel to EventCancelledEventHandler (email via Hangfire). Email handler is UNTOUCHED.
/// Uses BroadcastToEventAttendeesAsync to notify all opted-in attendees.
/// </summary>
public class EventCancelledWhatsAppHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventCancelledWhatsAppHandler> _logger;

    public EventCancelledWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EventCancelledWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var reason = domainEvent.Reason;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp EventCancelled START: EventId={EventId}, Reason={Reason}",
            eventId, reason);

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
                    _logger.LogWarning("[Phase 7A] WhatsApp EventCancelled: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, "Attendee" },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Cancellation.CancellationReason, reason },
                    { WhatsAppTemplateContract.Cancellation.RefundMessage, @event.IsFree() ? "No payment was collected." : "Refunds will be processed automatically." },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.BroadcastToEventAttendeesAsync(
                    eventId,
                    WhatsAppTemplateContract.TemplateNames.EventCancelled,
                    parameters,
                    WhatsAppNotificationType.EventCancellation,
                    CancellationToken.None);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp EventCancelled BROADCAST: EventId={EventId}, SentCount={Count}", eventId, result.Value);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp EventCancelled FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp EventCancelled EXCEPTION: EventId={EventId}", eventId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
