using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp add-on purchase receipt when payment completes.
/// Parallel to AddOnPurchaseCompletedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class AddOnPurchaseWhatsAppHandler : INotificationHandler<DomainEventNotification<AddOnPurchaseCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AddOnPurchaseWhatsAppHandler> _logger;

    public AddOnPurchaseWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AddOnPurchaseWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<AddOnPurchaseCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var buyerUserId = domainEvent.BuyerUserId;
        var buyerName = domainEvent.BuyerName;
        var addOnDefinitionId = domainEvent.AddOnDefinitionId;
        var quantity = domainEvent.Quantity;
        var totalAmount = domainEvent.TotalAmount;
        var currency = domainEvent.Currency;

        if (!buyerUserId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp AddOnPurchase SKIPPED: Anonymous buyer - EventId={EventId}",
                eventId);
            return Task.CompletedTask;
        }

        var capturedUserId = buyerUserId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp AddOnPurchase START: EventId={EventId}, BuyerUserId={BuyerUserId}, Amount={Amount} {Currency}",
            eventId, capturedUserId, totalAmount, currency);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var addOnRepository = scope.ServiceProvider.GetRequiredService<IAddOnDefinitionRepository>();

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp AddOnPurchase: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var addOnDefinition = await addOnRepository.GetByIdAsync(addOnDefinitionId, CancellationToken.None);
                var addOnName = addOnDefinition?.Name ?? "Add-on";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, buyerName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.AddOn.AddOnName, addOnName },
                    { WhatsAppTemplateContract.AddOn.Quantity, quantity.ToString() },
                    { WhatsAppTemplateContract.AddOn.TotalAmount, $"${totalAmount:F2} {currency}" },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.AddOnPurchaseReceipt,
                    parameters,
                    WhatsAppNotificationType.AddOnPurchase,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp AddOnPurchase SENT: EventId={EventId}, BuyerUserId={BuyerUserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp AddOnPurchase SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp AddOnPurchase FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp AddOnPurchase EXCEPTION: EventId={EventId}, BuyerUserId={BuyerUserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
