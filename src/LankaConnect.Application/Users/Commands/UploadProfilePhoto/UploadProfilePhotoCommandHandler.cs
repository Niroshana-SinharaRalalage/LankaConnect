using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Users.Commands.UploadProfilePhoto;

/// <summary>
/// Handler for uploading user profile photos
/// </summary>
public class UploadProfilePhotoCommandHandler : ICommandHandler<UploadProfilePhotoCommand, UploadProfilePhotoResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadProfilePhotoCommandHandler(
        IUserRepository userRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UploadProfilePhotoResponse>> Handle(
        UploadProfilePhotoCommand command,
        CancellationToken cancellationToken)
    {
        // Validate image file
        if (command.ImageFile == null || command.ImageFile.Length == 0)
        {
            return Result<UploadProfilePhotoResponse>.Failure("Image file is required");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result<UploadProfilePhotoResponse>.Failure("User not found");
        }

        // Delete existing photo if present
        if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
        {
            var deleteResult = await _imageService.DeleteImageAsync(
                user.ProfilePhotoUrl,
                cancellationToken);

            // Log if deletion fails but continue with upload
            // We don't want to block the new upload if old photo deletion fails
        }

        // Read file bytes
        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            await command.ImageFile.CopyToAsync(memoryStream, cancellationToken);
            fileBytes = memoryStream.ToArray();
        }

        // Upload new image
        var uploadResult = await _imageService.UploadImageAsync(
            fileBytes,
            command.ImageFile.FileName,
            command.UserId,
            cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            return Result<UploadProfilePhotoResponse>.Failure(uploadResult.Errors.ToArray());
        }

        // Update user entity
        var updateResult = user.UpdateProfilePhoto(
            uploadResult.Value.Url,
            uploadResult.Value.BlobName);

        if (!updateResult.IsSuccess)
        {
            // Clean up uploaded image if entity update fails
            await _imageService.DeleteImageAsync(uploadResult.Value.Url, cancellationToken);
            return Result<UploadProfilePhotoResponse>.Failure(updateResult.Errors.ToArray());
        }

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Return response
        var response = new UploadProfilePhotoResponse
        {
            PhotoUrl = uploadResult.Value.Url,
            FileSizeBytes = uploadResult.Value.SizeBytes,
            UploadedAt = uploadResult.Value.UploadedAt
        };

        return Result<UploadProfilePhotoResponse>.Success(response);
    }
}
