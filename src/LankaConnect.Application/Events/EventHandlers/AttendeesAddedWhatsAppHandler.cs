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
/// Phase 7B.3: Sends WhatsApp notification when attendees are added to an existing registration.
/// Parallel to AttendeesAddedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class AttendeesAddedWhatsAppHandler : INotificationHandler<DomainEventNotification<AttendeesAddedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendeesAddedWhatsAppHandler> _logger;

    public AttendeesAddedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AttendeesAddedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<AttendeesAddedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var userId = domainEvent.UserId;
        var contactEmail = domainEvent.ContactEmail;
        var addedCount = domainEvent.AddedAttendeeCount;
        var newTotalCount = domainEvent.NewTotalAttendeeCount;

        if (!userId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp AttendeesAdded SKIPPED: Anonymous user - EventId={EventId}",
                eventId);
            return Task.CompletedTask;
        }

        var capturedUserId = userId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp AttendeesAdded START: EventId={EventId}, UserId={UserId}, Added={Added}, NewTotal={NewTotal}",
            eventId, capturedUserId, addedCount, newTotalCount);

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
                    _logger.LogWarning("[Phase 7B.3] WhatsApp AttendeesAdded: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, contactEmail },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Attendees.AddedCount, addedCount.ToString() },
                    { WhatsAppTemplateContract.Attendees.NewTotalCount, newTotalCount.ToString() },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.AttendeesAdded,
                    parameters,
                    WhatsAppNotificationType.AttendeesAdded,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp AttendeesAdded SENT: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp AttendeesAdded SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp AttendeesAdded FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp AttendeesAdded EXCEPTION: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
