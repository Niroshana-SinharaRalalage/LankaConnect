using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Collection Feature: Handles CollectionCompletedEvent to send contribution receipt email.
/// Uses fire-and-forget pattern (Phase 6A.122) to avoid blocking HTTP response.
/// Uses new DI scope (Phase 6A.127) to avoid ObjectDisposedException in Task.Run.
/// </summary>
public class CollectionCompletedEventHandler
    : INotificationHandler<DomainEventNotification<CollectionCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CollectionCompletedEventHandler> _logger;

    public CollectionCompletedEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<CollectionCompletedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<CollectionCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "CollectionCompleted"))
        using (LogContext.PushProperty("CollectionId", domainEvent.CollectionId))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CollectionCompleted START: CollectionId={CollectionId}, ContributorName={ContributorName}, Amount={Amount} {Currency}",
                domainEvent.CollectionId, domainEvent.ContributorName,
                domainEvent.Amount, domainEvent.Currency);

            try
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "CollectionCompleted COMPLETE: Dispatching receipt email async - CollectionId={CollectionId}, Duration={ElapsedMs}ms",
                    domainEvent.CollectionId, stopwatch.ElapsedMilliseconds);

                // Capture variables before Task.Run to avoid closure on disposed objects
                var capturedContributorName = domainEvent.ContributorName;
                var capturedContributorEmail = domainEvent.ContributorEmail;
                var capturedEventId = domainEvent.EventId;
                var capturedCollectionId = domainEvent.CollectionId;
                var capturedAmount = domainEvent.Amount;
                var capturedCurrency = domainEvent.Currency;
                var capturedPaymentDate = domainEvent.PaymentCompletedAt;
                var capturedPaymentIntentId = domainEvent.PaymentIntentId;
                var capturedScopeFactory = _scopeFactory;

                _ = Task.Run(async () =>
                {
                    // Push LogContext inside Task.Run so structured logging works on the background thread
                    using (LogContext.PushProperty("Operation", "CollectionCompleted-Email"))
                    using (LogContext.PushProperty("CollectionId", capturedCollectionId))
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
                                    "CollectionCompleted EMAIL: Failed to load event title for EventId={EventId}, using fallback",
                                    capturedEventId);
                            }

                            // Phase 6A.137B: Build event details URL from configuration
                            var baseUrl = configuration["Application:FrontendBaseUrl"]
                                ?? configuration["FrontendBaseUrl"]
                                ?? "https://lankaconnect.com";
                            var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                            // Phase 6A.137B: Send collection receipt email
                            var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                            var emailParams = CollectionReceiptEmailParams.Create(
                                contributorName: capturedContributorName,
                                contributorEmail: capturedContributorEmail,
                                eventTitle: eventTitle,
                                contributionAmount: capturedAmount,
                                currency: capturedCurrency,
                                paymentDate: capturedPaymentDate,
                                paymentIntentId: capturedPaymentIntentId,
                                eventDetailsUrl: eventDetailsUrl
                            );

                            var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "CollectionCompleted EMAIL SENT: Email={Email}, CollectionId={CollectionId}, EventTitle={EventTitle}",
                                    capturedContributorEmail, capturedCollectionId, eventTitle);
                            }
                            else
                            {
                                _logger.LogError(
                                    "CollectionCompleted EMAIL FAILED: Email={Email}, CollectionId={CollectionId}, Errors={Errors}",
                                    capturedContributorEmail, capturedCollectionId, string.Join(", ", result.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "CollectionCompleted EMAIL EXCEPTION: Email={Email}, CollectionId={CollectionId}",
                                capturedContributorEmail, capturedCollectionId);
                        }
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: log but don't throw (avoids rolling back collection transaction)
                _logger.LogError(ex,
                    "CollectionCompleted FAILED: CollectionId={CollectionId}, Duration={ElapsedMs}ms",
                    domainEvent.CollectionId, stopwatch.ElapsedMilliseconds);
            }
        }

        return Task.CompletedTask;
    }
}
