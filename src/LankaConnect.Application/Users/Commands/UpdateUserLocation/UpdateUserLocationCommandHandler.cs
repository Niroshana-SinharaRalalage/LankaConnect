using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Application.Users.Commands.UpdateUserLocation;

/// <summary>
/// Handler for UpdateUserLocationCommand
/// Updates user location or clears it if all location fields are null
/// </summary>
public class UpdateUserLocationCommandHandler : ICommandHandler<UpdateUserLocationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserLocationCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateUserLocationCommand command,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Check if we're clearing the location (all fields null) or setting it
        if (string.IsNullOrWhiteSpace(command.City) &&
            string.IsNullOrWhiteSpace(command.State) &&
            string.IsNullOrWhiteSpace(command.ZipCode) &&
            string.IsNullOrWhiteSpace(command.Country))
        {
            // Clear location (privacy choice)
            var clearResult = user.UpdateLocation(null);
            if (!clearResult.IsSuccess)
            {
                return Result.Failure(clearResult.Errors.ToArray());
            }
        }
        else
        {
            // Create and set new location
            var locationResult = UserLocation.Create(
                command.City!,
                command.State!,
                command.ZipCode!,
                command.Country!);

            if (!locationResult.IsSuccess)
            {
                return Result.Failure(locationResult.Error);
            }

            var updateResult = user.UpdateLocation(locationResult.Value);
            if (!updateResult.IsSuccess)
            {
                return Result.Failure(updateResult.Errors.ToArray());
            }
        }

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
