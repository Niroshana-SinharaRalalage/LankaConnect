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
/// Phase 7B.3: Sends WhatsApp donation receipt when a donation payment completes.
/// Parallel to DonationCompletedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class DonationCompletedWhatsAppHandler : INotificationHandler<DomainEventNotification<DonationCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DonationCompletedWhatsAppHandler> _logger;

    public DonationCompletedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<DonationCompletedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<DonationCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var donorUserId = domainEvent.DonorUserId;
        var donorName = domainEvent.DonorName;
        var amount = domainEvent.Amount;
        var currency = domainEvent.Currency;

        // Skip anonymous donors — WhatsApp requires a registered user with preferences
        if (!donorUserId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp DonationCompleted SKIPPED: Anonymous donor - EventId={EventId}",
                eventId);
            return Task.CompletedTask;
        }

        var capturedUserId = donorUserId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp DonationCompleted START: EventId={EventId}, DonorUserId={DonorUserId}, Amount={Amount} {Currency}",
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
                    _logger.LogWarning("[Phase 7B.3] WhatsApp DonationCompleted: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, donorName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Donation.DonationAmount, $"${amount:F2} {currency}" },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.DonationReceipt,
                    parameters,
                    WhatsAppNotificationType.Donation,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp DonationCompleted SENT: EventId={EventId}, DonorUserId={DonorUserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp DonationCompleted SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp DonationCompleted FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp DonationCompleted EXCEPTION: EventId={EventId}, DonorUserId={DonorUserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
