using LankaConnect.Modules.Identity.Contracts; // W4.7.d.2
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a refund is requested.
/// Parallel to RefundRequestedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// </summary>
public class RefundRequestedWhatsAppHandler : INotificationHandler<DomainEventNotification<RefundRequestedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefundRequestedWhatsAppHandler> _logger;

    public RefundRequestedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<RefundRequestedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<RefundRequestedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var registrationId = domainEvent.RegistrationId;
        var userId = domainEvent.UserId;
        var refundAmount = domainEvent.RefundAmount + domainEvent.AddOnRefundAmount;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp RefundRequested START: EventId={EventId}, RegistrationId={RegistrationId}, Amount={Amount}",
            eventId, registrationId, refundAmount);

        if (!userId.HasValue)
        {
            _logger.LogInformation("[Phase 7A] WhatsApp RefundRequested SKIPPED: No UserId (anonymous user)");
            return Task.CompletedTask;
        }

        var capturedUserId = userId.Value;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var identityQueries = scope.ServiceProvider.GetRequiredService<IIdentityQueries>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var user = await identityQueries.GetUserByIdAsync(capturedUserId, CancellationToken.None);
                if (user == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RefundRequested: User not found - UserId={UserId}", capturedUserId);
                    return;
                }

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                var eventTitle = @event?.Title.Value ?? "Event";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, $"{user.FirstName} {user.LastName}" },
                    { WhatsAppTemplateContract.Refund.RefundAmount, $"${refundAmount:F2}" },
                    { WhatsAppTemplateContract.Common.EventTitle, eventTitle },
                    { WhatsAppTemplateContract.Refund.RefundStatus, "Processing" },
                    { WhatsAppTemplateContract.Refund.ReferenceId, domainEvent.PaymentIntentId }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.RefundInitiated,
                    parameters,
                    WhatsAppNotificationType.Refund,
                    eventId,
                    registrationId,
                    CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RefundRequested SENT: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RefundRequested SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RefundRequested FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp RefundRequested EXCEPTION: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
