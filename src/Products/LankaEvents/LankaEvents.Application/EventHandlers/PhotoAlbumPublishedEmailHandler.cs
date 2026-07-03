using LankaConnect.Modules.Media.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Wave 6.5.b canary: subscribes to <see cref="PhotoAlbumPublishedIntegrationEventV1"/>
/// dispatched via the outbox — replaces the previous subscription to
/// <c>DomainEventNotification&lt;PhotoAlbumPublishedDomainEvent&gt;</c>. Log-only
/// (email notifications are sent explicitly via <c>SendAlbumNotificationCommand</c>);
/// the handler exists as an audit trail proving the outbox dispatch reached its
/// subscriber.
/// </summary>
/// <remarks>
/// This is the "one consumer wired" acceptance criterion per architect Q4 ruling
/// (2026-07-02). Dispatch path: MediaDbContext.Outbox → <c>OutboxProcessor&lt;MediaDbContext&gt;</c>
/// → <c>MediatRIntegrationEventDispatcher</c> → this handler. When the outbox row
/// is marked ProcessedAt non-null, this log line proves the round-trip completed.
/// </remarks>
public class PhotoAlbumPublishedEmailHandler : INotificationHandler<PhotoAlbumPublishedIntegrationEventV1>
{
    private readonly ILogger<PhotoAlbumPublishedEmailHandler> _logger;

    public PhotoAlbumPublishedEmailHandler(ILogger<PhotoAlbumPublishedEmailHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        PhotoAlbumPublishedIntegrationEventV1 notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PhotoAlbumPublishedIntegrationEventV1 received (via outbox): " +
            "AlbumId={AlbumId}, OwningEventId={OwningEventId}, EventTitle={EventTitle}, AlbumName={AlbumName}, PublishedBy={UserId}. " +
            "Email notifications sent separately via SendAlbumNotificationCommand.",
            notification.AlbumId, notification.OwningEventId, notification.EventTitle,
            notification.AlbumName, notification.PublishedByUserId);

        return Task.CompletedTask;
    }
}
