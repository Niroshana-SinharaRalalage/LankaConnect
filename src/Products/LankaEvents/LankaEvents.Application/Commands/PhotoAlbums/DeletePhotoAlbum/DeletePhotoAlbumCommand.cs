using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Modules.Media.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.DeletePhotoAlbum;

/// <summary>
/// Command to delete a photo album. Only draft albums can be deleted.
/// Removes all photos from blob storage before deleting the album.
/// </summary>
public record DeletePhotoAlbumCommand(
    Guid AlbumId,
    Guid UserId
) : ICommand;

public class DeletePhotoAlbumCommandHandler : ICommandHandler<DeletePhotoAlbumCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly MediaDbContext _mediaContext;
    private readonly ILogger<DeletePhotoAlbumCommandHandler> _logger;

    public DeletePhotoAlbumCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAzureBlobStorageService blobStorageService,
        IMultiContextUnitOfWork unitOfWork,
        MediaDbContext mediaContext,
        ILogger<DeletePhotoAlbumCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
        _mediaContext = mediaContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeletePhotoAlbumCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeletePhotoAlbum"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        {
            _logger.LogInformation(
                "Deleting photo album {AlbumId} by user {UserId}",
                request.AlbumId, request.UserId);

            var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
            if (album == null)
                return Result.Failure($"Album with ID {request.AlbumId} not found");

            // Only organizer can delete
            if (album.OrganizerId != request.UserId)
                return Result.Failure("Only the event organizer can delete albums");

            // Only draft albums can be deleted
            if (album.Status != AlbumStatus.Draft)
                return Result.Failure("Only draft albums can be deleted. Published albums cannot be removed.");

            // Clean up blob storage for all photos
            foreach (var photo in album.Photos)
            {
                try
                {
                    await _blobStorageService.DeleteFileAsync(photo.OriginalBlobName, cancellationToken: cancellationToken);
                    await _blobStorageService.DeleteFileAsync(photo.ThumbnailBlobName, cancellationToken: cancellationToken);
                    if (!string.IsNullOrEmpty(photo.MediumBlobName))
                        await _blobStorageService.DeleteFileAsync(photo.MediumBlobName, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete blob for photo {PhotoId} during album deletion. Continuing with remaining photos.",
                        photo.Id);
                }
            }

            // Wave 6.5.b: Delete + atomic multi-context commit. Repository DeleteAsync no
            // longer self-saves — the ChangeTracker holds the Removed entity until the
            // multi-context commit persists the delete atomically with AppDbContext.
            await _photoAlbumRepository.DeleteAsync(album, cancellationToken);
            await _unitOfWork.CommitAsync(new DbContext[] { _mediaContext }, cancellationToken);

            _logger.LogInformation(
                "Photo album {AlbumId} deleted successfully with {PhotoCount} photos cleaned up",
                request.AlbumId, album.PhotoCount);

            return Result.Success();
        }
    }
}
