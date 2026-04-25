using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp notification to organizer when their event is rejected.
/// Parallel to EventRejectedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class EventRejectedWhatsAppHandler : INotificationHandler<DomainEventNotification<EventRejectedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventRejectedWhatsAppHandler> _logger;

    public EventRejectedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EventRejectedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<EventRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var reason = domainEvent.Reason;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp EventRejected START: EventId={EventId}",
            eventId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventRejected: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var organizer = await userRepository.GetByIdAsync(@event.OrganizerId, CancellationToken.None);
                if (organizer == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventRejected: Organizer not found - OrganizerId={OrganizerId}", @event.OrganizerId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, $"{organizer.FirstName} {organizer.LastName}" },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Rejection.RejectionReason, reason }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    @event.OrganizerId,
                    WhatsAppTemplateContract.TemplateNames.EventRejected,
                    parameters,
                    WhatsAppNotificationType.EventApproval,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp EventRejected SENT: EventId={EventId}, OrganizerId={OrganizerId}", eventId, @event.OrganizerId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp EventRejected SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventRejected FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp EventRejected EXCEPTION: EventId={EventId}", eventId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
