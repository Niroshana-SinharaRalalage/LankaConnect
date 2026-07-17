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
namespace LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.UpdateAlbumDetails;

/// <summary>
/// Command to update photo album details (name and description).
/// Only allowed by the album organizer.
/// </summary>
public record UpdateAlbumDetailsCommand(
    Guid AlbumId,
    Guid UserId,
    string Name,
    string? Description = null
) : ICommand;

public class UpdateAlbumDetailsCommandHandler : ICommandHandler<UpdateAlbumDetailsCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly MediaDbContext _mediaContext;
    private readonly ILogger<UpdateAlbumDetailsCommandHandler> _logger;

    public UpdateAlbumDetailsCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        MediaDbContext mediaContext,
        ILogger<UpdateAlbumDetailsCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _mediaContext = mediaContext;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateAlbumDetailsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateAlbumDetails"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateAlbumDetails START: AlbumId={AlbumId}, UserId={UserId}, Name={AlbumName}",
                request.AlbumId, request.UserId, request.Name);

            try
            {
                // 1. Get album by ID with change tracking
                var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: true, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateAlbumDetails FAILED: Album not found - AlbumId={AlbumId}, Duration={ElapsedMs}ms",
                        request.AlbumId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateAlbumDetails FAILED: User is not organizer - AlbumId={AlbumId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.AlbumId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can update album details");
                }

                // 3. Check for duplicate name (if name is changing)
                if (!string.Equals(album.Name, request.Name, StringComparison.Ordinal))
                {
                    var nameExists = await _photoAlbumRepository.ExistsByEventIdAndNameAsync(
                        album.EventId, request.Name, cancellationToken);
                    if (nameExists)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "UpdateAlbumDetails FAILED: Name already exists - AlbumId={AlbumId}, Name={AlbumName}, Duration={ElapsedMs}ms",
                            request.AlbumId, request.Name, stopwatch.ElapsedMilliseconds);
                        return Result.Failure($"A photo album with the name '{request.Name}' already exists for this event");
                    }
                }

                // 4. Update details via domain method
                var updateResult = album.UpdateDetails(request.Name, request.Description);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateAlbumDetails FAILED: Domain validation failed - AlbumId={AlbumId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.AlbumId, updateResult.Error, stopwatch.ElapsedMilliseconds);
                    return updateResult;
                }

                // 5. Wave 8.5.g direct-SaveChanges on MediaDbContext (Wave 8.5.f interceptor dispatches domain events)
                await _mediaContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateAlbumDetails COMPLETE: AlbumId={AlbumId}, Name={AlbumName}, Duration={ElapsedMs}ms",
                    album.Id, album.Name, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateAlbumDetails FAILED: Exception occurred - AlbumId={AlbumId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.AlbumId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
