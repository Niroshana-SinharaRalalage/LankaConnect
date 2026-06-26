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

namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Sponsorship Feature: Handles SponsorPaymentCompletedEvent to send money sponsor confirmation email.
/// Uses fire-and-forget pattern (Phase 6A.122) to avoid blocking HTTP response.
/// Uses new DI scope (Phase 6A.127) to avoid ObjectDisposedException in Task.Run.
/// </summary>
public class SponsorPaymentCompletedEventHandler
    : INotificationHandler<DomainEventNotification<SponsorPaymentCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SponsorPaymentCompletedEventHandler> _logger;

    public SponsorPaymentCompletedEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<SponsorPaymentCompletedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<SponsorPaymentCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "SponsorPaymentCompleted"))
        using (LogContext.PushProperty("SponsorId", domainEvent.SponsorId))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SponsorPaymentCompleted START: SponsorId={SponsorId}, SponsorName={SponsorName}, Organization={Organization}, Amount={Amount} {Currency}",
                domainEvent.SponsorId, domainEvent.SponsorName,
                domainEvent.SponsorOrganization ?? "(none)",
                domainEvent.Amount, domainEvent.Currency);

            try
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "SponsorPaymentCompleted COMPLETE: Dispatching confirmation email async - SponsorId={SponsorId}, Duration={ElapsedMs}ms",
                    domainEvent.SponsorId, stopwatch.ElapsedMilliseconds);

                // Capture variables before Task.Run to avoid closure on disposed objects
                var capturedSponsorName = domainEvent.SponsorName;
                var capturedSponsorEmail = domainEvent.SponsorEmail;
                var capturedSponsorOrganization = domainEvent.SponsorOrganization;
                var capturedEventId = domainEvent.EventId;
                var capturedSponsorId = domainEvent.SponsorId;
                var capturedAmount = domainEvent.Amount;
                var capturedCurrency = domainEvent.Currency;
                var capturedPaymentDate = domainEvent.PaymentCompletedAt;
                var capturedPaymentIntentId = domainEvent.PaymentIntentId;
                var capturedScopeFactory = _scopeFactory;

                _ = Task.Run(async () =>
                {
                    // Push LogContext inside Task.Run so structured logging works on the background thread
                    using (LogContext.PushProperty("Operation", "SponsorPaymentCompleted-Email"))
                    using (LogContext.PushProperty("SponsorId", capturedSponsorId))
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
                                    "SponsorPaymentCompleted EMAIL: Failed to load event title for EventId={EventId}, using fallback",
                                    capturedEventId);
                            }

                            // Phase 6A.137B: Build event details URL from configuration
                            var baseUrl = configuration["Application:FrontendBaseUrl"]
                                ?? configuration["FrontendBaseUrl"]
                                ?? "https://lankaconnect.com";
                            var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                            // Phase 6A.137B: Send monetary sponsor confirmation email
                            var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                            var emailParams = SponsorConfirmationEmailParams.CreateForMoneySponsor(
                                sponsorName: capturedSponsorName,
                                sponsorEmail: capturedSponsorEmail,
                                sponsorOrganization: capturedSponsorOrganization,
                                eventTitle: eventTitle,
                                amount: capturedAmount,
                                currency: capturedCurrency,
                                paymentDate: capturedPaymentDate,
                                paymentIntentId: capturedPaymentIntentId,
                                eventDetailsUrl: eventDetailsUrl
                            );

                            var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "SponsorPaymentCompleted EMAIL SENT: Email={Email}, SponsorId={SponsorId}, EventTitle={EventTitle}, Organization={Organization}",
                                    capturedSponsorEmail, capturedSponsorId, eventTitle, capturedSponsorOrganization ?? "(none)");
                            }
                            else
                            {
                                _logger.LogError(
                                    "SponsorPaymentCompleted EMAIL FAILED: Email={Email}, SponsorId={SponsorId}, Errors={Errors}",
                                    capturedSponsorEmail, capturedSponsorId, string.Join(", ", result.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "SponsorPaymentCompleted EMAIL EXCEPTION: Email={Email}, SponsorId={SponsorId}",
                                capturedSponsorEmail, capturedSponsorId);
                        }
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: log but don't throw (avoids rolling back sponsor payment transaction)
                _logger.LogError(ex,
                    "SponsorPaymentCompleted FAILED: SponsorId={SponsorId}, Duration={ElapsedMs}ms",
                    domainEvent.SponsorId, stopwatch.ElapsedMilliseconds);
            }
        }

        return Task.CompletedTask;
    }
}
