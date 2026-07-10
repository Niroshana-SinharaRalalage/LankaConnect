using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp sponsor confirmation when an item-based sponsorship is recorded.
/// Parallel to ItemSponsorRecordedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// </summary>
public class ItemSponsorWhatsAppHandler : INotificationHandler<DomainEventNotification<ItemSponsorRecordedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ItemSponsorWhatsAppHandler> _logger;

    public ItemSponsorWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ItemSponsorWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<ItemSponsorRecordedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var sponsorUserId = domainEvent.SponsorUserId;
        var sponsorName = domainEvent.SponsorName;
        var itemName = domainEvent.ItemName;

        if (!sponsorUserId.HasValue)
        {
            _logger.LogInformation(
                "[Phase 7B.3] WhatsApp ItemSponsor SKIPPED: Anonymous sponsor - EventId={EventId}",
                eventId);
            return Task.CompletedTask;
        }

        var capturedUserId = sponsorUserId.Value;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp ItemSponsor START: EventId={EventId}, SponsorUserId={SponsorUserId}, Item={ItemName}",
            eventId, capturedUserId, itemName);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp ItemSponsor: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var sponsorDetails = !string.IsNullOrWhiteSpace(domainEvent.ItemDescription)
                    ? $"{itemName} - {domainEvent.ItemDescription}"
                    : itemName;

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, sponsorName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Sponsor.SponsorType, "Item" },
                    { WhatsAppTemplateContract.Sponsor.SponsorDetails, sponsorDetails },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    capturedUserId,
                    WhatsAppTemplateContract.TemplateNames.SponsorConfirmation,
                    parameters,
                    WhatsAppNotificationType.Sponsorship,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp ItemSponsor SENT: EventId={EventId}, SponsorUserId={SponsorUserId}", eventId, capturedUserId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp ItemSponsor SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp ItemSponsor FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp ItemSponsor EXCEPTION: EventId={EventId}, SponsorUserId={SponsorUserId}", eventId, capturedUserId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
