using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Application.Users.Commands.UpdateLanguages;

/// <summary>
/// Handler for UpdateLanguagesCommand
/// Validates language codes and creates LanguagePreference value objects
/// </summary>
public class UpdateLanguagesCommandHandler : ICommandHandler<UpdateLanguagesCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLanguagesCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateLanguagesCommand command,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Convert DTOs to LanguagePreference value objects
        var languagePreferences = new List<LanguagePreference>();

        foreach (var langDto in command.Languages)
        {
            // Find language code
            var languageCode = LanguageCode.All.FirstOrDefault(lc => lc.Code == langDto.LanguageCode);
            if (languageCode == null)
            {
                return Result.Failure($"Invalid language code: {langDto.LanguageCode}");
            }

            // Create LanguagePreference
            var preferenceResult = LanguagePreference.Create(languageCode, langDto.ProficiencyLevel);
            if (!preferenceResult.IsSuccess)
            {
                return Result.Failure(preferenceResult.Error);
            }

            languagePreferences.Add(preferenceResult.Value);
        }

        // Update user's languages (domain will validate 1-5 rule)
        var updateResult = user.UpdateLanguages(languagePreferences);
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
