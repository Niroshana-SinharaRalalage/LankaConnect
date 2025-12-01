using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

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

        try
        {
            _logger.LogInformation(
                "Processing ImageRemovedFromEventDomainEvent: EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}",
                domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName);

            // Construct blob URL from blob name using Azure Blob Storage service
            var imageUrl = _blobStorageService.GetBlobUrl(domainEvent.BlobName);

            // Delete image from Azure Blob Storage
            var deleteResult = await _imageService.DeleteImageAsync(imageUrl, cancellationToken);

            if (deleteResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to delete blob from storage: EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}, Errors={Errors}",
                    domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName, string.Join(", ", deleteResult.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "Successfully deleted blob from storage: EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}",
                    domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName);
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw (image metadata already removed from database)
            _logger.LogError(ex,
                "Unexpected error deleting blob from storage: EventId={EventId}, ImageId={ImageId}, BlobName={BlobName}",
                domainEvent.EventId, domainEvent.ImageId, domainEvent.BlobName);
        }
    }
}
