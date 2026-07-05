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
/// Phase 7B.3: Sends WhatsApp payment pending reminder when a registration is created with pending payment.
/// Parallel to RegistrationPendingPaymentEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class PaymentPendingWhatsAppHandler : INotificationHandler<DomainEventNotification<RegistrationPendingPaymentEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentPendingWhatsAppHandler> _logger;

    public PaymentPendingWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentPendingWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<RegistrationPendingPaymentEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var registrationId = domainEvent.RegistrationId;
        var userId = domainEvent.UserId;
        var contactName = domainEvent.ContactName;
        var totalAmount = domainEvent.TotalAmount;
        var currency = domainEvent.Currency;
        var checkoutExpiresAt = domainEvent.CheckoutExpiresAt;

        if (!userId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp PaymentPending SKIPPED: Anonymous user - EventId={EventId}, RegistrationId={RegistrationId}",
                eventId, registrationId);
            return Task.CompletedTask;
        }

        var capturedUserId = userId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp PaymentPending START: EventId={EventId}, RegistrationId={RegistrationId}, UserId={UserId}, Amount={Amount} {Currency}",
            eventId, registrationId, capturedUserId, totalAmount, currency);

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
                    _logger.LogWarning("[Phase 7B.3] WhatsApp PaymentPending: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var timeUntilExpiry = checkoutExpiresAt - DateTime.UtcNow;
                var expiryText = timeUntilExpiry.TotalHours >= 1
                    ? $"{(int)timeUntilExpiry.TotalHours} hours"
                    : $"{(int)timeUntilExpiry.TotalMinutes} minutes";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, contactName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Payment.PaymentAmount, $"${totalAmount:F2} {currency}" },
                    { WhatsAppTemplateContract.Payment.ExpiryTime, expiryText },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.PaymentPendingReminder,
                    parameters,
                    WhatsAppNotificationType.PaymentPending,
                    eventId,
                    registrationId,
                    CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp PaymentPending SENT: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp PaymentPending SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp PaymentPending FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp PaymentPending EXCEPTION: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
