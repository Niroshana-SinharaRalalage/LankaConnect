using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Event handler for ImageRemovedFromEventDomainEvent
/// Deletes image blob from Azure Blob Storage when image is removed from event
/// Fail-silent pattern - logs errors but doesn't throw
/// </summary>
public class ImageRemovedEventHandler : INotificationHandler<DomainEventNotification<ImageRemovedFromEventDomainEvent>>
{
    private readonly IImageService _imageService;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<ImageRemovedEventHandler> _logger;

    public ImageRemovedEventHandler(
        IImageService imageService,
        IAzureBlobStorageService blobStorageService,
        ILogger<ImageRemovedEventHandler> logger)
    {
        _imageService = imageService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ImageRemovedFromEventDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "ImageRemoved"))
        using (LogContext.PushProperty("EntityType", "EventImage"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("ImageId", domainEvent.ImageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ImageRemoved START: EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}",
                domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Construct blob URL from blob name using Azure Blob Storage service
                var imageUrl = _blobStorageService.GetBlobUrl(domainEvent.BlobName);

                _logger.LogInformation(
                    "ImageRemoved: Constructed blob URL - BlobUrl={BlobUrl}",
                    imageUrl);

                // Delete image from Azure Blob Storage
                var deleteResult = await _imageService.DeleteImageAsync(imageUrl, cancellationToken);

                stopwatch.Stop();

                if (deleteResult.IsFailure)
                {
                    _logger.LogWarning(
                        "ImageRemoved: Failed to delete blob from storage - EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName, string.Join(", ", deleteResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "ImageRemoved COMPLETE: Blob deleted successfully - EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "ImageRemoved CANCELED: Operation was canceled - EventId={EventId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.ImageId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Fail-silent: Log error but don't throw (image metadata already removed from database)
                _logger.LogError(ex,
                    "ImageRemoved FAILED: Unexpected error deleting blob - EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}
