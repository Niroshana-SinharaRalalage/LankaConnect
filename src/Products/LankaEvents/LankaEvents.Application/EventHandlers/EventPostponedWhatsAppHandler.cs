using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp notification to all event attendees when event is postponed.
/// Parallel to EventPostponedEventHandler (email). Email handler is UNTOUCHED.
/// Uses BroadcastToEventAttendeesAsync (sends to all opted-in attendees).
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class EventPostponedWhatsAppHandler : INotificationHandler<DomainEventNotification<EventPostponedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventPostponedWhatsAppHandler> _logger;

    public EventPostponedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EventPostponedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<EventPostponedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var reason = domainEvent.Reason;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp EventPostponed START: EventId={EventId}",
            eventId);

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
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventPostponed: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Postponement.PostponementReason, reason },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.BroadcastToEventAttendeesAsync(
                    eventId,
                    WhatsAppTemplateContract.TemplateNames.EventPostponed,
                    parameters,
                    WhatsAppNotificationType.EventPostponed,
                    CancellationToken.None);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp EventPostponed BROADCAST: EventId={EventId}, SentCount={SentCount}", eventId, result.Value);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventPostponed FAILED: EventId={EventId}, {Errors}", eventId, string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp EventPostponed EXCEPTION: EventId={EventId}", eventId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
