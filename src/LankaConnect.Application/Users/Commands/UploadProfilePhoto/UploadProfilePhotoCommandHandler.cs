using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.UploadProfilePhoto;

/// <summary>
/// Handler for uploading user profile photos
/// </summary>
public class UploadProfilePhotoCommandHandler : ICommandHandler<UploadProfilePhotoCommand, UploadProfilePhotoResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger;

    public UploadProfilePhotoCommandHandler(
        IUserRepository userRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadProfilePhotoCommandHandler> logger)
    {
        _userRepository = userRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UploadProfilePhotoResponse>> Handle(
        UploadProfilePhotoCommand command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UploadProfilePhoto"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", command.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UploadProfilePhoto START: UserId={UserId}, FileName={FileName}, FileSize={FileSize}",
                command.UserId, command.ImageFile?.FileName, command.ImageFile?.Length);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Validate image file
                if (command.ImageFile == null || command.ImageFile.Length == 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadProfilePhoto FAILED: Image file is required - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UploadProfilePhotoResponse>.Failure("Image file is required");
                }

                _logger.LogInformation(
                    "UploadProfilePhoto: Image file validated - FileName={FileName}, FileSize={FileSize}",
                    command.ImageFile.FileName, command.ImageFile.Length);

                // Get user
                var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadProfilePhoto FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UploadProfilePhotoResponse>.Failure("User not found");
                }

                _logger.LogInformation(
                    "UploadProfilePhoto: User loaded - UserId={UserId}, Email={Email}, HasExistingPhoto={HasExistingPhoto}",
                    user.Id, user.Email.Value, !string.IsNullOrEmpty(user.ProfilePhotoUrl));

                // Delete existing photo if present
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    _logger.LogInformation(
                        "UploadProfilePhoto: Deleting existing photo - ProfilePhotoUrl={ProfilePhotoUrl}",
                        user.ProfilePhotoUrl);

                    var deleteResult = await _imageService.DeleteImageAsync(
                        user.ProfilePhotoUrl,
                        cancellationToken);

                    if (!deleteResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "UploadProfilePhoto: Old photo deletion failed - ProfilePhotoUrl={ProfilePhotoUrl}, Errors={Errors}",
                            user.ProfilePhotoUrl, string.Join(", ", deleteResult.Errors));
                        // Log if deletion fails but continue with upload
                        // We don't want to block the new upload if old photo deletion fails
                    }
                    else
                    {
                        _logger.LogInformation(
                            "UploadProfilePhoto: Old photo deleted successfully - ProfilePhotoUrl={ProfilePhotoUrl}",
                            user.ProfilePhotoUrl);
                    }
                }

                // Read file bytes
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await command.ImageFile.CopyToAsync(memoryStream, cancellationToken);
                    fileBytes = memoryStream.ToArray();
                }

                _logger.LogInformation(
                    "UploadProfilePhoto: File bytes read - BytesCount={BytesCount}",
                    fileBytes.Length);

                // Upload new image
                var uploadResult = await _imageService.UploadImageAsync(
                    fileBytes,
                    command.ImageFile.FileName,
                    command.UserId,
                    cancellationToken);

                if (!uploadResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadProfilePhoto FAILED: Image upload failed - UserId={UserId}, FileName={FileName}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, command.ImageFile.FileName, string.Join(", ", uploadResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result<UploadProfilePhotoResponse>.Failure(uploadResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "UploadProfilePhoto: Image uploaded successfully - Url={Url}, BlobName={BlobName}, SizeBytes={SizeBytes}",
                    uploadResult.Value.Url, uploadResult.Value.BlobName, uploadResult.Value.SizeBytes);

                // Update user entity
                var updateResult = user.UpdateProfilePhoto(
                    uploadResult.Value.Url,
                    uploadResult.Value.BlobName);

                if (!updateResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UploadProfilePhoto FAILED: Domain validation failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, string.Join(", ", updateResult.Errors), stopwatch.ElapsedMilliseconds);

                    // Clean up uploaded image if entity update fails
                    _logger.LogInformation(
                        "UploadProfilePhoto: Cleaning up uploaded image - Url={Url}",
                        uploadResult.Value.Url);

                    await _imageService.DeleteImageAsync(uploadResult.Value.Url, cancellationToken);

                    return Result<UploadProfilePhotoResponse>.Failure(updateResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "UploadProfilePhoto: Domain method succeeded - UserId={UserId}, NewPhotoUrl={NewPhotoUrl}",
                    user.Id, uploadResult.Value.Url);

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UploadProfilePhoto COMPLETE: UserId={UserId}, PhotoUrl={PhotoUrl}, FileSize={FileSize}, Duration={ElapsedMs}ms",
                    command.UserId, uploadResult.Value.Url, uploadResult.Value.SizeBytes, stopwatch.ElapsedMilliseconds);

                // Return response
                var response = new UploadProfilePhotoResponse
                {
                    PhotoUrl = uploadResult.Value.Url,
                    FileSizeBytes = uploadResult.Value.SizeBytes,
                    UploadedAt = uploadResult.Value.UploadedAt
                };

                return Result<UploadProfilePhotoResponse>.Success(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UploadProfilePhoto CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UploadProfilePhoto FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
