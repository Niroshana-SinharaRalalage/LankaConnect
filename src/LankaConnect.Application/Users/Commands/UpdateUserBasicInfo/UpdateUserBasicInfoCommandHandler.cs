using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Users.Commands.UpdateUserBasicInfo;

/// <summary>
/// Handler for updating user's basic information
/// Phase 6A.70: Profile Basic Info Section
///
/// Updates: firstName, lastName, phoneNumber, bio
/// Does NOT update email - see UpdateUserEmailCommand for email changes
/// </summary>
public class UpdateUserBasicInfoCommandHandler : IRequestHandler<UpdateUserBasicInfoCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserBasicInfoCommandHandler> _logger;

    public UpdateUserBasicInfoCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserBasicInfoCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserBasicInfoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating basic info for user {UserId}: firstName={FirstName}, lastName={LastName}",
                request.UserId, request.FirstName, request.LastName);

            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID {UserId}", request.UserId);
                return Result<UserDto>.Failure("User not found");
            }

            // Parse phone number (if provided)
            PhoneNumber? phoneNumber = null;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var phoneResult = PhoneNumber.Create(request.PhoneNumber);
                if (phoneResult.IsFailure)
                {
                    _logger.LogWarning("Invalid phone number format for user {UserId}: {Error}",
                        request.UserId, phoneResult.Errors.FirstOrDefault());
                    return Result<UserDto>.Failure(phoneResult.Errors.FirstOrDefault() ?? "Invalid phone number format");
                }
                phoneNumber = phoneResult.Value;
            }

            // Update profile using domain method
            var updateResult = user.UpdateProfile(
                request.FirstName,
                request.LastName,
                phoneNumber,
                request.Bio);

            if (updateResult.IsFailure)
            {
                _logger.LogWarning("Failed to update basic info for user {UserId}: {Error}",
                    request.UserId, updateResult.Errors.FirstOrDefault());
                return Result<UserDto>.Failure(updateResult.Errors.FirstOrDefault() ?? "Failed to update basic info");
            }

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Basic info updated successfully for user {UserId}", request.UserId);

            // Map to DTO and return
            var userDto = MapToDto(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating basic info for user {UserId}", request.UserId);
            return Result<UserDto>.Failure("An error occurred while updating basic info");
        }
    }

    /// <summary>
    /// Map User entity to UserDto
    /// </summary>
    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber?.Value,
            Bio = user.Bio,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Location = user.Location != null ? new UserLocationDto
            {
                City = user.Location.City,
                State = user.Location.State,
                ZipCode = user.Location.ZipCode,
                Country = user.Location.Country
            } : null,
            CulturalInterests = user.CulturalInterests.Select(ci => ci.Code).ToList(),
            Languages = user.Languages.Select(l => new LanguageDto
            {
                LanguageCode = l.Language.Code,
                ProficiencyLevel = l.Proficiency
            }).ToList(),
            PreferredMetroAreas = user.PreferredMetroAreaIds.ToList()
        };
    }
}
