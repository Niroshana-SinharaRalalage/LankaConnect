using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Users.Commands.DeleteProfilePhoto;

/// <summary>
/// Handler for deleting user profile photos
/// </summary>
public class DeleteProfilePhotoCommandHandler : ICommandHandler<DeleteProfilePhotoCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProfilePhotoCommandHandler(
        IUserRepository userRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteProfilePhotoCommand command,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Check if user has a profile photo
        if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
        {
            return Result.Failure("No profile photo to remove");
        }

        // Delete image from storage
        var deleteResult = await _imageService.DeleteImageAsync(
            user.ProfilePhotoUrl,
            cancellationToken);

        if (!deleteResult.IsSuccess)
        {
            return Result.Failure(deleteResult.Errors.ToArray());
        }

        // Update user entity
        var removeResult = user.RemoveProfilePhoto();
        if (!removeResult.IsSuccess)
        {
            return Result.Failure(removeResult.Errors.ToArray());
        }

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
