using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Modules.Media.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.SetAlbumCoverPhoto;

/// <summary>
/// Command to set a photo as the album cover.
/// Only the event organizer can set the cover photo.
/// </summary>
public record SetAlbumCoverPhotoCommand(
    Guid AlbumId,
    Guid PhotoId,
    Guid UserId
) : ICommand;

public class SetAlbumCoverPhotoCommandHandler : ICommandHandler<SetAlbumCoverPhotoCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly MediaDbContext _mediaContext;
    private readonly ILogger<SetAlbumCoverPhotoCommandHandler> _logger;

    public SetAlbumCoverPhotoCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IMultiContextUnitOfWork unitOfWork,
        MediaDbContext mediaContext,
        ILogger<SetAlbumCoverPhotoCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _unitOfWork = unitOfWork;
        _mediaContext = mediaContext;
        _logger = logger;
    }

    public async Task<Result> Handle(SetAlbumCoverPhotoCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetAlbumCoverPhoto"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("PhotoId", request.PhotoId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SetAlbumCoverPhoto START: AlbumId={AlbumId}, PhotoId={PhotoId}, UserId={UserId}",
                request.AlbumId, request.PhotoId, request.UserId);

            try
            {
                // 1. Get album by ID with change tracking
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SetAlbumCoverPhoto FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SetAlbumCoverPhoto FAILED: User is not organizer - AlbumId={AlbumId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can set the album cover photo");
                }

                // 3. Set cover photo via domain method
                var setCoverResult = album.SetCoverPhoto(request.PhotoId);
                if (setCoverResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SetAlbumCoverPhoto FAILED: Domain validation failed - AlbumId={AlbumId}, PhotoId={PhotoId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.PhotoId, setCoverResult.Error, stopwatch.ElapsedMilliseconds);
                    return setCoverResult;
                }

                // 4. Wave 6.5.b: atomic multi-context commit. Replaces the F30a workaround.
                await _unitOfWork.CommitAsync(new DbContext[] { _mediaContext }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "SetAlbumCoverPhoto COMPLETE: PhotoId={PhotoId}, AlbumId={AlbumId}, CoverPhotoUrl={CoverPhotoUrl}, Duration={ElapsedMs}ms",
                    request.PhotoId, album.Id, album.CoverPhotoUrl, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SetAlbumCoverPhoto FAILED: Exception occurred - AlbumId={AlbumId}, PhotoId={PhotoId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, request.PhotoId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
