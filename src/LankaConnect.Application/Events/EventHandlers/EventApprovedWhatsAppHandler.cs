using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp notification to organizer when their event is approved.
/// Parallel to EventApprovedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class EventApprovedWhatsAppHandler : INotificationHandler<DomainEventNotification<EventApprovedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventApprovedWhatsAppHandler> _logger;

    public EventApprovedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EventApprovedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<EventApprovedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp EventApproved START: EventId={EventId}",
            eventId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var identityQueries = scope.ServiceProvider.GetRequiredService<IIdentityQueries>();

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventApproved: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var organizer = await identityQueries.GetUserByIdAsync(@event.OrganizerId, CancellationToken.None);
                if (organizer == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventApproved: Organizer not found - OrganizerId={OrganizerId}", @event.OrganizerId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, $"{organizer.FirstName} {organizer.LastName}" },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    @event.OrganizerId,
                    WhatsAppTemplateContract.TemplateNames.EventApproved,
                    parameters,
                    WhatsAppNotificationType.EventApproval,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp EventApproved SENT: EventId={EventId}, OrganizerId={OrganizerId}", eventId, @event.OrganizerId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp EventApproved SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp EventApproved FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp EventApproved EXCEPTION: EventId={EventId}", eventId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
