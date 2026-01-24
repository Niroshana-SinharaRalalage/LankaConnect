using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.UpdateLanguages;

/// <summary>
/// Handler for UpdateLanguagesCommand
/// Validates language codes and creates LanguagePreference value objects
/// </summary>
public class UpdateLanguagesCommandHandler : ICommandHandler<UpdateLanguagesCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateLanguagesCommandHandler> _logger;

    public UpdateLanguagesCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateLanguagesCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateLanguagesCommand command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateLanguages"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", command.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateLanguages START: UserId={UserId}, LanguageCount={LanguageCount}",
                command.UserId, command.Languages.Count);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user
                var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateLanguages FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "UpdateLanguages: User loaded - UserId={UserId}, Email={Email}, CurrentLanguagesCount={CurrentLanguagesCount}",
                    user.Id, user.Email.Value, user.Languages.Count);

                // Convert DTOs to LanguagePreference value objects
                var languagePreferences = new List<LanguagePreference>();

                foreach (var langDto in command.Languages)
                {
                    // Find language code
                    var languageCode = LanguageCode.All.FirstOrDefault(lc => lc.Code == langDto.LanguageCode);
                    if (languageCode == null)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateLanguages FAILED: Invalid language code - UserId={UserId}, Code={Code}, Duration={ElapsedMs}ms",
                            command.UserId, langDto.LanguageCode, stopwatch.ElapsedMilliseconds);

                        return Result.Failure($"Invalid language code: {langDto.LanguageCode}");
                    }

                    // Create LanguagePreference
                    var preferenceResult = LanguagePreference.Create(languageCode, langDto.ProficiencyLevel);
                    if (!preferenceResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateLanguages FAILED: Invalid proficiency level - UserId={UserId}, Code={Code}, ProficiencyLevel={ProficiencyLevel}, Error={Error}, Duration={ElapsedMs}ms",
                            command.UserId, langDto.LanguageCode, langDto.ProficiencyLevel, preferenceResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result.Failure(preferenceResult.Error);
                    }

                    languagePreferences.Add(preferenceResult.Value);
                }

                _logger.LogInformation(
                    "UpdateLanguages: Language preferences created - UserId={UserId}, ValidLanguageCount={ValidLanguageCount}",
                    user.Id, languagePreferences.Count);

                // Update user's languages (domain will validate 1-5 rule)
                var updateResult = user.UpdateLanguages(languagePreferences);
                if (!updateResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateLanguages FAILED: Domain validation failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, string.Join(", ", updateResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure(updateResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "UpdateLanguages: Domain method succeeded - UserId={UserId}, NewLanguagesCount={NewLanguagesCount}",
                    user.Id, languagePreferences.Count);

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateLanguages COMPLETE: UserId={UserId}, LanguageCount={LanguageCount}, Duration={ElapsedMs}ms",
                    command.UserId, languagePreferences.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UpdateLanguages CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateLanguages FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
