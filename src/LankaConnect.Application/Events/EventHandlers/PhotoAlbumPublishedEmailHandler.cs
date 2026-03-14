using LankaConnect.Application.Common;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles the PhotoAlbumPublished domain event.
/// Log-only: Email notifications are now sent explicitly via SendAlbumNotificationCommand.
/// This handler is kept for audit/logging purposes.
/// </summary>
public class PhotoAlbumPublishedEmailHandler : INotificationHandler<DomainEventNotification<PhotoAlbumPublishedDomainEvent>>
{
    private readonly ILogger<PhotoAlbumPublishedEmailHandler> _logger;

    public PhotoAlbumPublishedEmailHandler(ILogger<PhotoAlbumPublishedEmailHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<PhotoAlbumPublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "PhotoAlbumPublished: AlbumId={AlbumId}, EventId={EventId}, EventTitle={EventTitle}, AlbumName={AlbumName}. " +
            "Email notifications are sent separately via SendAlbumNotificationCommand.",
            domainEvent.AlbumId, domainEvent.EventId, domainEvent.EventTitle, domainEvent.AlbumName);

        return Task.CompletedTask;
    }
}
