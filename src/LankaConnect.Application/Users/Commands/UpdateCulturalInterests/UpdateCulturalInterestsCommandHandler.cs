using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.UpdateCulturalInterests;

/// <summary>
/// Handler for UpdateCulturalInterestsCommand
/// Validates interest codes and updates user's cultural interests
/// </summary>
public class UpdateCulturalInterestsCommandHandler : ICommandHandler<UpdateCulturalInterestsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCulturalInterestsCommandHandler> _logger;

    public UpdateCulturalInterestsCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCulturalInterestsCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateCulturalInterestsCommand command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateCulturalInterests"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", command.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateCulturalInterests START: UserId={UserId}, InterestCount={InterestCount}",
                command.UserId, command.InterestCodes.Count);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user
                var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateCulturalInterests FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "UpdateCulturalInterests: User loaded - UserId={UserId}, Email={Email}, CurrentInterestsCount={CurrentInterestsCount}",
                    user.Id, user.Email.Value, user.CulturalInterests.Count);

                // Phase 6A.47: Accept EventCategory codes from database (dynamic) + legacy hardcoded codes
                // Convert interest codes to CulturalInterest value objects
                var interests = new List<CulturalInterest>();

                foreach (var code in command.InterestCodes)
                {
                    var interestResult = CulturalInterest.FromCode(code);
                    if (!interestResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateCulturalInterests FAILED: Invalid interest code - UserId={UserId}, Code={Code}, Duration={ElapsedMs}ms",
                            command.UserId, code, stopwatch.ElapsedMilliseconds);

                        return Result.Failure($"Invalid interest code: {code}");
                    }
                    interests.Add(interestResult.Value);
                }

                _logger.LogInformation(
                    "UpdateCulturalInterests: Interest codes validated - UserId={UserId}, ValidInterestCount={ValidInterestCount}",
                    user.Id, interests.Count);

                // Update user's cultural interests (unlimited)
                var updateResult = user.UpdateCulturalInterests(interests);
                if (!updateResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateCulturalInterests FAILED: Domain validation failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, string.Join(", ", updateResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure(updateResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "UpdateCulturalInterests: Domain method succeeded - UserId={UserId}, NewInterestsCount={NewInterestsCount}",
                    user.Id, interests.Count);

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateCulturalInterests COMPLETE: UserId={UserId}, InterestCount={InterestCount}, Duration={ElapsedMs}ms",
                    command.UserId, interests.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UpdateCulturalInterests CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateCulturalInterests FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
