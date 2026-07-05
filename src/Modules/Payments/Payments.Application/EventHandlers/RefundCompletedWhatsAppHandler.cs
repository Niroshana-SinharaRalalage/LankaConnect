using LankaConnect.Modules.Identity.Contracts; // W4.7.d.2
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a refund is completed.
/// Parallel to RefundCompletedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// </summary>
public class RefundCompletedWhatsAppHandler : INotificationHandler<DomainEventNotification<RefundCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefundCompletedWhatsAppHandler> _logger;

    public RefundCompletedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<RefundCompletedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<RefundCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var registrationId = domainEvent.RegistrationId;
        var userId = domainEvent.UserId;
        // Phase 6A.148.W5.6.A — legacy fallback; the WhatsApp Task.Run below will replace
        // this with the workflow-aggregated total via IRefundTotalCalculator when applicable.
        var legacyFallbackTotal = domainEvent.RefundAmount + domainEvent.AddOnRefundAmount;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp RefundCompleted START: EventId={EventId}, RegistrationId={RegistrationId}, LegacyAmount={Amount}",
            eventId, registrationId, legacyFallbackTotal);

        if (!userId.HasValue)
        {
            _logger.LogInformation("[Phase 7A] WhatsApp RefundCompleted SKIPPED: No UserId (anonymous user)");
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
                // Phase 6A.148.W5.6.A — workflow-aware total (parity with email handler).
                var totalCalculator = scope.ServiceProvider.GetRequiredService<IRefundTotalCalculator>();

                var refundAmount = await totalCalculator.ComputeAttendeeFacingTotalAsync(
                    domainEvent.StripeRefundId, legacyFallbackTotal, CancellationToken.None);

                var user = await identityQueries.GetUserByIdAsync(capturedUserId, CancellationToken.None);
                if (user == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RefundCompleted: User not found - UserId={UserId}", capturedUserId);
                    return;
                }

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                var eventTitle = @event?.Title.Value ?? "Event";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, $"{user.FirstName} {user.LastName}" },
                    { WhatsAppTemplateContract.Refund.RefundAmount, $"${refundAmount:F2}" },
                    { WhatsAppTemplateContract.Common.EventTitle, eventTitle },
                    { WhatsAppTemplateContract.Refund.ReferenceId, domainEvent.StripeRefundId }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.RefundCompleted,
                    parameters,
                    WhatsAppNotificationType.Refund,
                    eventId,
                    registrationId,
                    CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RefundCompleted SENT: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RefundCompleted SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RefundCompleted FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp RefundCompleted EXCEPTION: EventId={EventId}, UserId={UserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
