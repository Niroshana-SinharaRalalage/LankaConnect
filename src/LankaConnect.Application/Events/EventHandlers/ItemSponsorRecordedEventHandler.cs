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

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Sponsorship Feature: Handles ItemSponsorRecordedEvent to send item sponsor acknowledgment email.
/// Uses fire-and-forget pattern (Phase 6A.122) to avoid blocking HTTP response.
/// Uses new DI scope (Phase 6A.127) to avoid ObjectDisposedException in Task.Run.
/// Note: No IConfiguration needed since there is no payment/URL involved for item sponsors.
/// </summary>
public class ItemSponsorRecordedEventHandler
    : INotificationHandler<DomainEventNotification<ItemSponsorRecordedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ItemSponsorRecordedEventHandler> _logger;

    public ItemSponsorRecordedEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ItemSponsorRecordedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<ItemSponsorRecordedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "ItemSponsorRecorded"))
        using (LogContext.PushProperty("SponsorId", domainEvent.SponsorId))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ItemSponsorRecorded START: SponsorId={SponsorId}, SponsorName={SponsorName}, ItemName={ItemName}",
                domainEvent.SponsorId, domainEvent.SponsorName, domainEvent.ItemName);

            try
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "ItemSponsorRecorded COMPLETE: Dispatching acknowledgment email async - SponsorId={SponsorId}, Duration={ElapsedMs}ms",
                    domainEvent.SponsorId, stopwatch.ElapsedMilliseconds);

                // Capture variables before Task.Run to avoid closure on disposed objects
                var capturedSponsorName = domainEvent.SponsorName;
                var capturedSponsorEmail = domainEvent.SponsorEmail;
                var capturedSponsorOrganization = domainEvent.SponsorOrganization;
                var capturedEventId = domainEvent.EventId;
                var capturedSponsorId = domainEvent.SponsorId;
                var capturedItemName = domainEvent.ItemName;
                var capturedItemDescription = domainEvent.ItemDescription;
                var capturedEstimatedValue = domainEvent.EstimatedValue;
                var capturedRecordedAt = domainEvent.RecordedAt;
                var capturedScopeFactory = _scopeFactory;

                _ = Task.Run(async () =>
                {
                    // Push LogContext inside Task.Run so structured logging works on the background thread
                    using (LogContext.PushProperty("Operation", "ItemSponsorRecorded-Email"))
                    using (LogContext.PushProperty("SponsorId", capturedSponsorId))
                    using (LogContext.PushProperty("EventId", capturedEventId))
                    {
                        try
                        {
                            using var scope = capturedScopeFactory.CreateScope();
                            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

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
                                    "ItemSponsorRecorded EMAIL: Failed to load event title for EventId={EventId}, using fallback",
                                    capturedEventId);
                            }

                            // Phase 6A.137B: Build event details URL from configuration
                            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                            var baseUrl = configuration["Application:FrontendBaseUrl"]
                                ?? configuration["FrontendBaseUrl"]
                                ?? "https://lankaconnect.com";
                            var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                            // Phase 6A.137B: Send item sponsor acknowledgment email
                            var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                            var emailParams = SponsorConfirmationEmailParams.CreateForItemSponsor(
                                sponsorName: capturedSponsorName,
                                sponsorEmail: capturedSponsorEmail,
                                sponsorOrganization: capturedSponsorOrganization,
                                eventTitle: eventTitle,
                                itemName: capturedItemName,
                                itemDescription: capturedItemDescription,
                                estimatedValue: capturedEstimatedValue,
                                recordedAt: capturedRecordedAt,
                                eventDetailsUrl: eventDetailsUrl
                            );

                            var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "ItemSponsorRecorded EMAIL SENT: Email={Email}, SponsorId={SponsorId}, ItemName={ItemName}, EventTitle={EventTitle}",
                                    capturedSponsorEmail, capturedSponsorId, capturedItemName, eventTitle);
                            }
                            else
                            {
                                _logger.LogError(
                                    "ItemSponsorRecorded EMAIL FAILED: Email={Email}, SponsorId={SponsorId}, Errors={Errors}",
                                    capturedSponsorEmail, capturedSponsorId, string.Join(", ", result.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "ItemSponsorRecorded EMAIL EXCEPTION: Email={Email}, SponsorId={SponsorId}",
                                capturedSponsorEmail, capturedSponsorId);
                        }
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: log but don't throw (avoids rolling back item sponsor transaction)
                _logger.LogError(ex,
                    "ItemSponsorRecorded FAILED: SponsorId={SponsorId}, Duration={ElapsedMs}ms",
                    domainEvent.SponsorId, stopwatch.ElapsedMilliseconds);
            }
        }

        return Task.CompletedTask;
    }
}
