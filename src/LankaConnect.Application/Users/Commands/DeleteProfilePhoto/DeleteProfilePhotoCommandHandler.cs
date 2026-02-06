using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.DeleteProfilePhoto;

/// <summary>
/// Handler for deleting user profile photos
/// </summary>
public class DeleteProfilePhotoCommandHandler : ICommandHandler<DeleteProfilePhotoCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProfilePhotoCommandHandler> _logger;

    public DeleteProfilePhotoCommandHandler(
        IUserRepository userRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProfilePhotoCommandHandler> logger)
    {
        _userRepository = userRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteProfilePhotoCommand command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteProfilePhoto"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", command.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteProfilePhoto START: UserId={UserId}",
                command.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user
                var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteProfilePhoto FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "DeleteProfilePhoto: User loaded - UserId={UserId}, Email={Email}, ProfilePhotoUrl={ProfilePhotoUrl}",
                    user.Id, user.Email.Value, user.ProfilePhotoUrl);

                // Check if user has a profile photo
                if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteProfilePhoto FAILED: No profile photo to remove - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("No profile photo to remove");
                }

                _logger.LogInformation(
                    "DeleteProfilePhoto: Deleting image from storage - ProfilePhotoUrl={ProfilePhotoUrl}",
                    user.ProfilePhotoUrl);

                // Delete image from storage
                var deleteResult = await _imageService.DeleteImageAsync(
                    user.ProfilePhotoUrl,
                    cancellationToken);

                if (!deleteResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteProfilePhoto FAILED: Image deletion failed - UserId={UserId}, ProfilePhotoUrl={ProfilePhotoUrl}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, user.ProfilePhotoUrl, string.Join(", ", deleteResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure(deleteResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "DeleteProfilePhoto: Image deleted from storage - ProfilePhotoUrl={ProfilePhotoUrl}",
                    user.ProfilePhotoUrl);

                // Update user entity
                var removeResult = user.RemoveProfilePhoto();
                if (!removeResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteProfilePhoto FAILED: Domain validation failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, string.Join(", ", removeResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure(removeResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "DeleteProfilePhoto: Domain method succeeded - UserId={UserId}",
                    user.Id);

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteProfilePhoto COMPLETE: UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "DeleteProfilePhoto CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "DeleteProfilePhoto FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
