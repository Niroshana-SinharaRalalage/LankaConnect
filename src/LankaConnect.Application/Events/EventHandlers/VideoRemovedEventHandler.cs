using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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

        using (LogContext.PushProperty("Operation", "VideoRemoved"))
        using (LogContext.PushProperty("EntityType", "EventVideo"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("VideoId", domainEvent.VideoId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "VideoRemoved START: EventId={EventId}, VideoId={VideoId}, VideoBlobName={VideoBlobName}, ThumbnailBlobName={ThumbnailBlobName}",
                domainEvent.EventId, domainEvent.VideoId, domainEvent.VideoBlobName, domainEvent.ThumbnailBlobName);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Construct blob URLs from blob names using Azure Blob Storage service
                var videoUrl = _blobStorageService.GetBlobUrl(domainEvent.VideoBlobName);
                var thumbnailUrl = _blobStorageService.GetBlobUrl(domainEvent.ThumbnailBlobName);

                _logger.LogInformation(
                    "VideoRemoved: Constructed blob URLs - VideoUrl={VideoUrl}, ThumbnailUrl={ThumbnailUrl}",
                    videoUrl, thumbnailUrl);

                // Delete video blob from Azure Blob Storage
                _logger.LogInformation(
                    "VideoRemoved: Deleting video blob - BlobName={BlobName}",
                    domainEvent.VideoBlobName);

                var deleteVideoResult = await _imageService.DeleteImageAsync(videoUrl, cancellationToken);

                if (deleteVideoResult.IsFailure)
                {
                    _logger.LogWarning(
                        "VideoRemoved: Failed to delete video blob - EventId={EventId}, VideoId={VideoId}, BlobName={BlobName}, Errors={Errors}",
                        domainEvent.EventId, domainEvent.VideoId, domainEvent.VideoBlobName, string.Join(", ", deleteVideoResult.Errors));
                }
                else
                {
                    _logger.LogInformation(
                        "VideoRemoved: Video blob deleted successfully - BlobName={BlobName}",
                        domainEvent.VideoBlobName);
                }

                // Delete thumbnail blob from Azure Blob Storage
                _logger.LogInformation(
                    "VideoRemoved: Deleting thumbnail blob - BlobName={BlobName}",
                    domainEvent.ThumbnailBlobName);

                var deleteThumbnailResult = await _imageService.DeleteImageAsync(thumbnailUrl, cancellationToken);

                stopwatch.Stop();

                if (deleteThumbnailResult.IsFailure)
                {
                    _logger.LogWarning(
                        "VideoRemoved: Failed to delete thumbnail blob - EventId={EventId}, VideoId={VideoId}, BlobName={BlobName}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.VideoId, domainEvent.ThumbnailBlobName, string.Join(", ", deleteThumbnailResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "VideoRemoved COMPLETE: Thumbnail blob deleted successfully - EventId={EventId}, VideoId={VideoId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.VideoId, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "VideoRemoved CANCELED: Operation was canceled - EventId={EventId}, VideoId={VideoId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.VideoId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Fail-silent: Log error but don't throw (video metadata already removed from database)
                _logger.LogError(ex,
                    "VideoRemoved FAILED: Unexpected error deleting blobs - EventId={EventId}, VideoId={VideoId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.EventId, domainEvent.VideoId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}
