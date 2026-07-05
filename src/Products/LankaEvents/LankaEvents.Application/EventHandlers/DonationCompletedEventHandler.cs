using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Donation Feature: Handles DonationCompletedEvent to send receipt email.
/// Uses fire-and-forget pattern (Phase 6A.122) to avoid blocking HTTP response.
/// Uses new DI scope (Phase 6A.127) to avoid ObjectDisposedException in Task.Run.
/// </summary>
public class DonationCompletedEventHandler
    : INotificationHandler<DomainEventNotification<DonationCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DonationCompletedEventHandler> _logger;

    public DonationCompletedEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<DonationCompletedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<DonationCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "DonationCompleted"))
        using (LogContext.PushProperty("DonationId", domainEvent.DonationId))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DonationCompleted START: DonationId={DonationId}, DonorName={DonorName}, DonorEmail={DonorEmail}, Amount={Amount} {Currency}",
                domainEvent.DonationId, domainEvent.DonorName, domainEvent.DonorEmail,
                domainEvent.Amount, domainEvent.Currency);

            try
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "DonationCompleted COMPLETE: Dispatching receipt email async - DonationId={DonationId}, Duration={ElapsedMs}ms",
                    domainEvent.DonationId, stopwatch.ElapsedMilliseconds);

                // Capture variables before Task.Run to avoid closure on disposed objects
                var capturedDonorName = domainEvent.DonorName;
                var capturedDonorEmail = domainEvent.DonorEmail;
                var capturedEventId = domainEvent.EventId;
                var capturedDonationId = domainEvent.DonationId;
                var capturedAmount = domainEvent.Amount;
                var capturedCurrency = domainEvent.Currency;
                var capturedPaymentDate = domainEvent.PaymentCompletedAt;
                var capturedPaymentIntentId = domainEvent.PaymentIntentId;
                var capturedScopeFactory = _scopeFactory;

                _ = Task.Run(async () =>
                {
                    // Push LogContext inside Task.Run so structured logging works on the background thread
                    using (LogContext.PushProperty("Operation", "DonationCompleted-Email"))
                    using (LogContext.PushProperty("DonationId", capturedDonationId))
                    using (LogContext.PushProperty("EventId", capturedEventId))
                    {
                        try
                        {
                            using var scope = capturedScopeFactory.CreateScope();
                            var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
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
                                    "DonationCompleted EMAIL: Failed to load event title for EventId={EventId}, using fallback",
                                    capturedEventId);
                            }

                            // Build event details URL from configuration
                            var baseUrl = configuration["Application:FrontendBaseUrl"]
                                ?? configuration["FrontendBaseUrl"]
                                ?? "https://lankaconnect.com";
                            var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                            var emailParams = DonationReceiptEmailParams.Create(
                                donorName: capturedDonorName,
                                donorEmail: capturedDonorEmail,
                                eventTitle: eventTitle,
                                donationAmount: capturedAmount,
                                currency: capturedCurrency,
                                paymentDate: capturedPaymentDate,
                                paymentIntentId: capturedPaymentIntentId,
                                eventDetailsUrl: eventDetailsUrl
                            );

                            var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "DonationCompleted EMAIL SENT: Email={Email}, DonationId={DonationId}, EventTitle={EventTitle}",
                                    capturedDonorEmail, capturedDonationId, eventTitle);
                            }
                            else
                            {
                                _logger.LogError(
                                    "DonationCompleted EMAIL FAILED: Email={Email}, Errors={Errors}",
                                    capturedDonorEmail, string.Join(", ", result.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "DonationCompleted EMAIL EXCEPTION: Email={Email}, DonationId={DonationId}",
                                capturedDonorEmail, capturedDonationId);
                        }
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: log but don't throw (avoids rolling back donation transaction)
                _logger.LogError(ex,
                    "DonationCompleted FAILED: DonationId={DonationId}, Duration={ElapsedMs}ms",
                    domainEvent.DonationId, stopwatch.ElapsedMilliseconds);
            }
        }

        return Task.CompletedTask;
    }
}
