using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
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

                            // Phase 6A.137B: Resolve add-on definition name
                            var addOnName = $"Add-On #{capturedAddOnDefinitionId:N}"; // fallback
                            try
                            {
                                var addOnDefRepo = scope.ServiceProvider.GetRequiredService<IAddOnDefinitionRepository>();
                                var addOnDef = await addOnDefRepo.GetByIdAsync(capturedAddOnDefinitionId, CancellationToken.None);
                                if (addOnDef != null)
                                    addOnName = addOnDef.Name;
                            }
                            catch (Exception defEx)
                            {
                                _logger.LogWarning(defEx,
                                    "AddOnPurchaseCompleted EMAIL: Failed to load add-on definition name for DefinitionId={DefinitionId}, using fallback",
                                    capturedAddOnDefinitionId);
                            }

                            // Phase 6A.137B: Build event details URL from configuration
                            var baseUrl = configuration["Application:FrontendBaseUrl"]
                                ?? configuration["FrontendBaseUrl"]
                                ?? "https://lankaconnect.com";
                            var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                            // Phase 6A.137B: Send add-on purchase receipt email
                            var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                            var emailParams = AddOnPurchaseReceiptEmailParams.Create(
                                buyerName: capturedBuyerName,
                                buyerEmail: capturedBuyerEmail,
                                eventTitle: eventTitle,
                                addOnName: addOnName,
                                quantity: capturedQuantity,
                                unitPrice: capturedUnitPrice,
                                totalAmount: capturedTotalAmount,
                                currency: capturedCurrency,
                                paymentDate: capturedPaymentDate,
                                paymentIntentId: capturedPaymentIntentId,
                                eventDetailsUrl: eventDetailsUrl
                            );

                            var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "AddOnPurchaseCompleted EMAIL SENT: Email={Email}, PurchaseId={PurchaseId}, AddOnName={AddOnName}, EventTitle={EventTitle}",
                                    capturedBuyerEmail, capturedPurchaseId, addOnName, eventTitle);
                            }
                            else
                            {
                                _logger.LogError(
                                    "AddOnPurchaseCompleted EMAIL FAILED: Email={Email}, PurchaseId={PurchaseId}, Errors={Errors}",
                                    capturedBuyerEmail, capturedPurchaseId, string.Join(", ", result.Errors));
                            }
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
