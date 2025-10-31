using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Application.Users.Commands.UpdateCulturalInterests;

/// <summary>
/// Handler for UpdateCulturalInterestsCommand
/// Validates interest codes and updates user's cultural interests
/// </summary>
public class UpdateCulturalInterestsCommandHandler : ICommandHandler<UpdateCulturalInterestsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCulturalInterestsCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateCulturalInterestsCommand command,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Convert interest codes to CulturalInterest value objects
        var interests = new List<CulturalInterest>();

        foreach (var code in command.InterestCodes)
        {
            var interest = CulturalInterest.All.FirstOrDefault(i => i.Code == code);
            if (interest == null)
            {
                return Result.Failure($"Invalid cultural interest code: {code}");
            }
            interests.Add(interest);
        }

        // Update user's cultural interests (domain will validate 0-10 rule)
        var updateResult = user.UpdateCulturalInterests(interests);
        if (!updateResult.IsSuccess)
        {
            return Result.Failure(updateResult.Errors.ToArray());
        }

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
