using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Event handler for VideoRemovedFromEventDomainEvent
/// Deletes video and thumbnail blobs from Azure Blob Storage when video is removed from event
/// Fail-silent pattern - logs errors but doesn't throw
/// </summary>
public class VideoRemovedEventHandler : INotificationHandler<DomainEventNotification<VideoRemovedFromEventDomainEvent>>
{
    private readonly IImageService _imageService; // Reusing for video blob deletion
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<VideoRemovedEventHandler> _logger;

    public VideoRemovedEventHandler(
        IImageService imageService,
        IAzureBlobStorageService blobStorageService,
        ILogger<VideoRemovedEventHandler> logger)
    {
        _imageService = imageService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<VideoRemovedFromEventDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            _logger.LogInformation(
                "Processing VideoRemovedFromEventDomainEvent: EventId={EventId}, VideoId={VideoId}, VideoBlobName={VideoBlobName}, ThumbnailBlobName={ThumbnailBlobName}",
                domainEvent.EventId, domainEvent.VideoId, domainEvent.VideoBlobName, domainEvent.ThumbnailBlobName);

            // Construct blob URLs from blob names using Azure Blob Storage service
            var videoUrl = _blobStorageService.GetBlobUrl(domainEvent.VideoBlobName);
            var thumbnailUrl = _blobStorageService.GetBlobUrl(domainEvent.ThumbnailBlobName);

            // Delete video blob from Azure Blob Storage
            var deleteVideoResult = await _imageService.DeleteImageAsync(videoUrl, cancellationToken);

            if (deleteVideoResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to delete video blob from storage: EventId={EventId}, VideoId={VideoId}, BlobName={BlobName}, Errors={Errors}",
                    domainEvent.EventId, domainEvent.VideoId, domainEvent.VideoBlobName, string.Join(", ", deleteVideoResult.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Successfully deleted video blob from storage: EventId={EventId}, VideoId={VideoId}, BlobName={BlobName}",
                    domainEvent.EventId, domainEvent.VideoId, domainEvent.VideoBlobName);
            }

            // Delete thumbnail blob from Azure Blob Storage
            var deleteThumbnailResult = await _imageService.DeleteImageAsync(thumbnailUrl, cancellationToken);

            if (deleteThumbnailResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to delete thumbnail blob from storage: EventId={EventId}, VideoId={VideoId}, BlobName={BlobName}, Errors={Errors}",
                    domainEvent.EventId, domainEvent.VideoId, domainEvent.ThumbnailBlobName, string.Join(", ", deleteThumbnailResult.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Successfully deleted thumbnail blob from storage: EventId={EventId}, VideoId={VideoId}, BlobName={BlobName}",
                    domainEvent.EventId, domainEvent.VideoId, domainEvent.ThumbnailBlobName);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw (video metadata already removed from database)
            _logger.LogError(ex,
                "Unexpected error deleting video/thumbnail blobs from storage: EventId={EventId}, VideoId={VideoId}, VideoBlobName={VideoBlobName}, ThumbnailBlobName={ThumbnailBlobName}",
                domainEvent.EventId, domainEvent.VideoId, domainEvent.VideoBlobName, domainEvent.ThumbnailBlobName);
        }
    }
}
