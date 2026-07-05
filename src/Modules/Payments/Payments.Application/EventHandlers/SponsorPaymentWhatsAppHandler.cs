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
namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp sponsor confirmation when a money-based sponsorship payment completes.
/// Parallel to SponsorPaymentCompletedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class SponsorPaymentWhatsAppHandler : INotificationHandler<DomainEventNotification<SponsorPaymentCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SponsorPaymentWhatsAppHandler> _logger;

    public SponsorPaymentWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<SponsorPaymentWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<SponsorPaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var sponsorUserId = domainEvent.SponsorUserId;
        var sponsorName = domainEvent.SponsorName;
        var amount = domainEvent.Amount;
        var currency = domainEvent.Currency;

        if (!sponsorUserId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp SponsorPayment SKIPPED: Anonymous sponsor - EventId={EventId}",
                eventId);
            return Task.CompletedTask;
        }

        var capturedUserId = sponsorUserId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp SponsorPayment START: EventId={EventId}, SponsorUserId={SponsorUserId}, Amount={Amount} {Currency}",
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
                    _logger.LogWarning("[Phase 7B.3] WhatsApp SponsorPayment: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, sponsorName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Sponsor.SponsorType, "Money" },
                    { WhatsAppTemplateContract.Sponsor.SponsorDetails, $"${amount:F2} {currency}" },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.SponsorConfirmation,
                    parameters,
                    WhatsAppNotificationType.Sponsorship,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp SponsorPayment SENT: EventId={EventId}, SponsorUserId={SponsorUserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp SponsorPayment SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp SponsorPayment FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp SponsorPayment EXCEPTION: EventId={EventId}, SponsorUserId={SponsorUserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
