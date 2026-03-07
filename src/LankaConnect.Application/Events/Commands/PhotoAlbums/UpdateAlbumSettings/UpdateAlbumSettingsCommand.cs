using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.UpdateAlbumSettings;

/// <summary>
/// Command to update photo album settings (upload permission, moderation mode, description).
/// Only allowed while album is in Draft or Published status.
/// </summary>
public record UpdateAlbumSettingsCommand(
    Guid EventId,
    Guid UserId,
    AlbumUploadPermission? UploadPermission = null,
    AlbumModerationMode? ModerationMode = null,
    string? Description = null
) : ICommand;

public class UpdateAlbumSettingsCommandHandler : ICommandHandler<UpdateAlbumSettingsCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAlbumSettingsCommandHandler> _logger;

    public UpdateAlbumSettingsCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAlbumSettingsCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateAlbumSettingsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateAlbumSettings"))
        using (LogContext.PushProperty("EntityType", "PhotoAlbum"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateAlbumSettings START: EventId={EventId}, UserId={UserId}, UploadPermission={UploadPermission}, ModerationMode={ModerationMode}",
                request.EventId, request.UserId, request.UploadPermission, request.ModerationMode);

            try
            {
                // 1. Get album by event ID with change tracking
                var album = await _photoAlbumRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                if (album == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateAlbumSettings FAILED: Album not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Photo album not found for this event");
                }

                // 2. Verify the user is the organizer
                if (album.OrganizerId != request.UserId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateAlbumSettings FAILED: User is not organizer - EventId={EventId}, UserId={UserId}, OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, album.OrganizerId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Only the event organizer can update album settings");
                }

                // 3. Update settings via domain method
                var updateResult = album.UpdateSettings(
                    request.UploadPermission,
                    request.ModerationMode,
                    request.Description);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateAlbumSettings FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, updateResult.Error, stopwatch.ElapsedMilliseconds);
                    return updateResult;
                }

                // 4. Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateAlbumSettings COMPLETE: AlbumId={AlbumId}, EventId={EventId}, UploadPermission={UploadPermission}, ModerationMode={ModerationMode}, Duration={ElapsedMs}ms",
                    album.Id, request.EventId, album.UploadPermission, album.ModerationMode, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateAlbumSettings FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
