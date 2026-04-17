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
/// Phase 7B.3: Sends WhatsApp collection receipt when a contribution payment completes.
/// Parallel to CollectionCompletedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class CollectionCompletedWhatsAppHandler : INotificationHandler<DomainEventNotification<CollectionCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CollectionCompletedWhatsAppHandler> _logger;

    public CollectionCompletedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<CollectionCompletedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<CollectionCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var contributorUserId = domainEvent.ContributorUserId;
        var contributorName = domainEvent.ContributorName;
        var amount = domainEvent.Amount;
        var currency = domainEvent.Currency;

        if (!contributorUserId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp CollectionCompleted SKIPPED: Anonymous contributor - EventId={EventId}",
                eventId);
            return Task.CompletedTask;
        }

        var capturedUserId = contributorUserId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp CollectionCompleted START: EventId={EventId}, ContributorUserId={ContributorUserId}, Amount={Amount} {Currency}",
            eventId, capturedUserId, amount, currency);

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
                    _logger.LogWarning("[Phase 7B.3] WhatsApp CollectionCompleted: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, contributorName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.CollectionParams.ContributionAmount, $"${amount:F2} {currency}" },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.CollectionReceipt,
                    parameters,
                    WhatsAppNotificationType.Collection,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp CollectionCompleted SENT: EventId={EventId}, ContributorUserId={ContributorUserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp CollectionCompleted SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp CollectionCompleted FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp CollectionCompleted EXCEPTION: EventId={EventId}, ContributorUserId={ContributorUserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
