using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.DeletePhotoAlbum;

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePhotoAlbumCommandHandler> _logger;

    public DeletePhotoAlbumCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAzureBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork,
        ILogger<DeletePhotoAlbumCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
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

            // Delete the album (cascade will delete photos from DB).
            // W4.2: DeleteAsync is self-saving on the new IPhotoAlbumRepository contract;
            // the IUnitOfWork.CommitAsync below remains for AppDbContext-scoped audit writes.
            await _photoAlbumRepository.DeleteAsync(album, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Photo album {AlbumId} deleted successfully with {PhotoCount} photos cleaned up",
                request.AlbumId, album.PhotoCount);

            return Result.Success();
        }
    }
}
