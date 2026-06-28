using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.PublishPhotoAlbum;

/// <summary>
/// Command to publish a photo album, making it visible to attendees.
/// Triggers a PhotoAlbumPublishedDomainEvent for notification emails.
/// </summary>
public record PublishPhotoAlbumCommand(
    Guid AlbumId,
    Guid UserId
) : ICommand;

public class PublishPhotoAlbumCommandHandler : ICommandHandler<PublishPhotoAlbumCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PublishPhotoAlbumCommandHandler> _logger;

    public PublishPhotoAlbumCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IUnitOfWork unitOfWork,
        ILogger<PublishPhotoAlbumCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(PublishPhotoAlbumCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "PublishPhotoAlbum"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PublishPhotoAlbum START: AlbumId={AlbumId}, UserId={UserId}",
                request.AlbumId, request.UserId);

            try
            {
                // 1. Get album by ID with change tracking
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishPhotoAlbum FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishPhotoAlbum FAILED: User is not organizer - AlbumId={AlbumId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can publish the photo album");
                }

                // 3. Publish via domain method
                var publishResult = album.Publish();
                if (publishResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishPhotoAlbum FAILED: Domain validation failed - AlbumId={AlbumId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, publishResult.Error, stopwatch.ElapsedMilliseconds);
                    return publishResult;
                }

                // 4. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PublishPhotoAlbum COMPLETE: AlbumId={AlbumId}, EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                    album.Id, album.EventId, album.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "PublishPhotoAlbum FAILED: Exception occurred - AlbumId={AlbumId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
