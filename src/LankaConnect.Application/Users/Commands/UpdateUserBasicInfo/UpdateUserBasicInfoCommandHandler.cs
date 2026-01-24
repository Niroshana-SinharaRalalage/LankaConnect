using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "UpdateUserBasicInfo"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateUserBasicInfo START: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, HasPhoneNumber={HasPhoneNumber}, HasBio={HasBio}",
                request.UserId, request.FirstName, request.LastName, !string.IsNullOrWhiteSpace(request.PhoneNumber), !string.IsNullOrWhiteSpace(request.Bio));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserBasicInfo FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UserDto>.Failure("User not found");
                }

                _logger.LogInformation(
                    "UpdateUserBasicInfo: User loaded - UserId={UserId}, Email={Email}, CurrentFirstName={CurrentFirstName}, CurrentLastName={CurrentLastName}",
                    user.Id, user.Email.Value, user.FirstName, user.LastName);

                // Parse phone number (if provided)
                PhoneNumber? phoneNumber = null;
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var phoneResult = PhoneNumber.Create(request.PhoneNumber);
                    if (phoneResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateUserBasicInfo FAILED: Invalid phone number format - UserId={UserId}, PhoneNumber={PhoneNumber}, Error={Error}, Duration={ElapsedMs}ms",
                            request.UserId, request.PhoneNumber, phoneResult.Errors.FirstOrDefault(), stopwatch.ElapsedMilliseconds);

                        return Result<UserDto>.Failure(phoneResult.Errors.FirstOrDefault() ?? "Invalid phone number format");
                    }
                    phoneNumber = phoneResult.Value;

                    _logger.LogInformation(
                        "UpdateUserBasicInfo: Phone number parsed - PhoneNumber={PhoneNumber}",
                        phoneNumber.Value);
                }

                // Update profile using domain method
                var updateResult = user.UpdateProfile(
                    request.FirstName,
                    request.LastName,
                    phoneNumber,
                    request.Bio);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserBasicInfo FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, updateResult.Errors.FirstOrDefault(), stopwatch.ElapsedMilliseconds);

                    return Result<UserDto>.Failure(updateResult.Errors.FirstOrDefault() ?? "Failed to update basic info");
                }

                _logger.LogInformation(
                    "UpdateUserBasicInfo: Domain method succeeded - UserId={UserId}",
                    user.Id);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateUserBasicInfo COMPLETE: UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                // Map to DTO and return
                var userDto = MapToDto(user);
                return Result<UserDto>.Success(userDto);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UpdateUserBasicInfo CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateUserBasicInfo FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<UserDto>.Failure("An error occurred while updating basic info");
            }
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
