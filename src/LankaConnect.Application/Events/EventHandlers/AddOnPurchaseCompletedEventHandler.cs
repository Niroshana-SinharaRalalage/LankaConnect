using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Add-On Feature: Handles AddOnPurchaseCompletedEvent to send purchase receipt email.
/// Uses fire-and-forget pattern (Phase 6A.122) to avoid blocking HTTP response.
/// Uses new DI scope (Phase 6A.127) to avoid ObjectDisposedException in Task.Run.
/// </summary>
public class AddOnPurchaseCompletedEventHandler
    : INotificationHandler<DomainEventNotification<AddOnPurchaseCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AddOnPurchaseCompletedEventHandler> _logger;

    public AddOnPurchaseCompletedEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AddOnPurchaseCompletedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<AddOnPurchaseCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "AddOnPurchaseCompleted"))
        using (LogContext.PushProperty("AddOnPurchaseId", domainEvent.AddOnPurchaseId))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddOnPurchaseCompleted START: PurchaseId={PurchaseId}, BuyerName={BuyerName}, Quantity={Quantity}, TotalAmount={TotalAmount} {Currency}",
                domainEvent.AddOnPurchaseId, domainEvent.BuyerName,
                domainEvent.Quantity, domainEvent.TotalAmount, domainEvent.Currency);

            try
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "AddOnPurchaseCompleted COMPLETE: Dispatching receipt email async - PurchaseId={PurchaseId}, Duration={ElapsedMs}ms",
                    domainEvent.AddOnPurchaseId, stopwatch.ElapsedMilliseconds);

                // Capture variables before Task.Run to avoid closure on disposed objects
                var capturedBuyerName = domainEvent.BuyerName;
                var capturedBuyerEmail = domainEvent.BuyerEmail;
                var capturedEventId = domainEvent.EventId;
                var capturedPurchaseId = domainEvent.AddOnPurchaseId;
                var capturedAddOnDefinitionId = domainEvent.AddOnDefinitionId;
                var capturedQuantity = domainEvent.Quantity;
                var capturedUnitPrice = domainEvent.UnitPrice;
                var capturedTotalAmount = domainEvent.TotalAmount;
                var capturedCurrency = domainEvent.Currency;
                var capturedPaymentDate = domainEvent.PaymentCompletedAt;
                var capturedPaymentIntentId = domainEvent.PaymentIntentId;
                var capturedScopeFactory = _scopeFactory;

                _ = Task.Run(async () =>
                {
                    // Push LogContext inside Task.Run so structured logging works on the background thread
                    using (LogContext.PushProperty("Operation", "AddOnPurchaseCompleted-Email"))
                    using (LogContext.PushProperty("AddOnPurchaseId", capturedPurchaseId))
                    using (LogContext.PushProperty("EventId", capturedEventId))
                    {
                        try
                        {
                            using var scope = capturedScopeFactory.CreateScope();
                            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                            // Fetch the actual event title from the repository
                            var eventTitle = $"Event {capturedEventId:N}"; // fallback
                            try
                            {
                                var @event = await eventRepository.GetByIdAsync(capturedEventId, trackChanges: false, CancellationToken.None);
                                if (@event != null)
                                    eventTitle = @event.Title.Value;
                            }
                            catch (Exception titleEx)
                            {
                                _logger.LogWarning(titleEx,
                                    "AddOnPurchaseCompleted EMAIL: Failed to load event title for EventId={EventId}, using fallback",
                                    capturedEventId);
                            }

                            // TODO: Create email template and TypedEmailParams for add-on purchase receipts
                            _logger.LogInformation(
                                "AddOnPurchaseCompleted EMAIL PLACEHOLDER: Would send receipt to {Email} for purchase {PurchaseId} on event '{EventTitle}', Quantity={Quantity}, TotalAmount={TotalAmount} {Currency}",
                                capturedBuyerEmail, capturedPurchaseId, eventTitle,
                                capturedQuantity, capturedTotalAmount, capturedCurrency);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "AddOnPurchaseCompleted EMAIL EXCEPTION: Email={Email}, PurchaseId={PurchaseId}",
                                capturedBuyerEmail, capturedPurchaseId);
                        }
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: log but don't throw (avoids rolling back add-on purchase transaction)
                _logger.LogError(ex,
                    "AddOnPurchaseCompleted FAILED: PurchaseId={PurchaseId}, Duration={ElapsedMs}ms",
                    domainEvent.AddOnPurchaseId, stopwatch.ElapsedMilliseconds);
            }
        }

        return Task.CompletedTask;
    }
}
