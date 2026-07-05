using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Phase 6A.157 — handles <see cref="PackageSponsorCompletedEvent"/> to send
/// the forked package-sponsor confirmation email (template
/// <c>template-package-sponsor-confirmation</c>). Sibling to
/// <see cref="SponsorPaymentCompletedEventHandler"/>; the existing generic
/// handler is unchanged.
///
/// Fire-and-forget pattern (Phase 6A.122) + new DI scope (Phase 6A.127) so
/// the background email send doesn't block the HTTP response and doesn't
/// hit ObjectDisposedException on the scoped <c>ITypedEmailService</c>.
///
/// Loads the package via <see cref="ISponsorshipPackageRepository"/> to
/// extract the perks list (the domain event carries everything else
/// snapshot-style, but perks would bloat the event payload — repo lookup
/// is cheaper and still correct because per architect lock package edits
/// after purchase MUST not retroactively rewrite confirmation emails —
/// but the lookup is for perks-DISPLAY only, the price/name/tier/tickets
/// come from the snapshotted event).
/// </summary>
public class PackageSponsorCompletedEventHandler
    : INotificationHandler<DomainEventNotification<PackageSponsorCompletedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PackageSponsorCompletedEventHandler> _logger;

    public PackageSponsorCompletedEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<PackageSponsorCompletedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<PackageSponsorCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "PackageSponsorCompleted"))
        using (LogContext.PushProperty("SponsorId", domainEvent.SponsorId))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("PackageId", domainEvent.SponsorshipPackageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PackageSponsorCompleted START: SponsorId={SponsorId}, SponsorName={SponsorName}, PackageName={PackageName}, Tier={Tier}, IncludedTickets={IncludedTickets}, Amount={Amount} {Currency}",
                domainEvent.SponsorId, domainEvent.SponsorName,
                domainEvent.PackageNameSnapshot, domainEvent.PackageTierSnapshot ?? "(none)",
                domainEvent.IncludedTicketCountSnapshot, domainEvent.Amount, domainEvent.Currency);

            try
            {
                stopwatch.Stop();

                // Capture for closure safety (avoid disposed-scope hits inside Task.Run)
                var capturedSponsorName = domainEvent.SponsorName;
                var capturedSponsorEmail = domainEvent.SponsorEmail;
                var capturedSponsorOrganization = domainEvent.SponsorOrganization;
                var capturedEventId = domainEvent.EventId;
                var capturedSponsorId = domainEvent.SponsorId;
                var capturedPackageId = domainEvent.SponsorshipPackageId;
                var capturedPackageName = domainEvent.PackageNameSnapshot;
                var capturedPackageTier = domainEvent.PackageTierSnapshot;
                var capturedAmount = domainEvent.Amount;
                var capturedCurrency = domainEvent.Currency;
                var capturedPaymentDate = domainEvent.PaymentCompletedAt;
                var capturedPaymentIntentId = domainEvent.PaymentIntentId;
                var capturedIncludedTicketCount = domainEvent.IncludedTicketCountSnapshot;
                var capturedScopeFactory = _scopeFactory;

                _ = Task.Run(async () =>
                {
                    using (LogContext.PushProperty("Operation", "PackageSponsorCompleted-Email"))
                    using (LogContext.PushProperty("SponsorId", capturedSponsorId))
                    using (LogContext.PushProperty("EventId", capturedEventId))
                    {
                        try
                        {
                            using var scope = capturedScopeFactory.CreateScope();
                            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                            var packageRepository = scope.ServiceProvider.GetRequiredService<ISponsorshipPackageRepository>();
                            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                            // Resolve event title (fallback to GUID if lookup fails)
                            var eventTitle = $"Event {capturedEventId:N}";
                            try
                            {
                                var @event = await eventRepository.GetByIdAsync(capturedEventId, trackChanges: false, CancellationToken.None);
                                if (@event != null)
                                    eventTitle = @event.Title.Value;
                            }
                            catch (Exception titleEx)
                            {
                                _logger.LogWarning(titleEx,
                                    "PackageSponsorCompleted EMAIL: Failed to load event title for EventId={EventId}, using fallback",
                                    capturedEventId);
                            }

                            // Resolve perks via package lookup (perks are DISPLAY-only —
                            // not snapshotted on Sponsor row; if the organizer edited the
                            // package after purchase, the displayed perks may differ
                            // slightly from what the buyer saw at checkout. Acceptable
                            // tradeoff per architect — alternative would bloat the event).
                            IReadOnlyList<string> perks = Array.Empty<string>();
                            try
                            {
                                var package = await packageRepository.GetByIdAsync(capturedPackageId, CancellationToken.None);
                                if (package != null)
                                    perks = package.Perks;
                            }
                            catch (Exception perksEx)
                            {
                                _logger.LogWarning(perksEx,
                                    "PackageSponsorCompleted EMAIL: Failed to load package perks for PackageId={PackageId}, sending email with empty perks",
                                    capturedPackageId);
                            }

                            var baseUrl = configuration["Application:FrontendBaseUrl"]
                                ?? configuration["FrontendBaseUrl"]
                                ?? "https://lankaconnect.com";
                            var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                            var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                            var emailParams = PackageSponsorConfirmationEmailParams.Create(
                                sponsorName: capturedSponsorName,
                                sponsorEmail: capturedSponsorEmail,
                                sponsorOrganization: capturedSponsorOrganization,
                                eventTitle: eventTitle,
                                packageNameSnapshot: capturedPackageName,
                                packageTierSnapshot: capturedPackageTier,
                                amountPaid: capturedAmount,
                                currency: capturedCurrency,
                                paymentDate: capturedPaymentDate,
                                paymentIntentId: capturedPaymentIntentId,
                                includedTicketCount: capturedIncludedTicketCount,
                                perks: perks,
                                eventDetailsUrl: eventDetailsUrl);

                            var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "PackageSponsorCompleted EMAIL SENT: Email={Email}, SponsorId={SponsorId}, PackageName={PackageName}, EventTitle={EventTitle}",
                                    capturedSponsorEmail, capturedSponsorId, capturedPackageName, eventTitle);
                            }
                            else
                            {
                                _logger.LogError(
                                    "PackageSponsorCompleted EMAIL FAILED: Email={Email}, SponsorId={SponsorId}, Errors={Errors}",
                                    capturedSponsorEmail, capturedSponsorId, string.Join(", ", result.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "PackageSponsorCompleted EMAIL EXCEPTION: Email={Email}, SponsorId={SponsorId}",
                                capturedSponsorEmail, capturedSponsorId);
                        }
                    }
                }, CancellationToken.None);

                _logger.LogInformation(
                    "PackageSponsorCompleted COMPLETE: Dispatched confirmation email async - SponsorId={SponsorId}, Duration={ElapsedMs}ms",
                    domainEvent.SponsorId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: log but don't throw (avoid rolling back sponsor payment)
                _logger.LogError(ex,
                    "PackageSponsorCompleted FAILED: SponsorId={SponsorId}, Duration={ElapsedMs}ms",
                    domainEvent.SponsorId, stopwatch.ElapsedMilliseconds);
            }
        }

        return Task.CompletedTask;
    }
}
