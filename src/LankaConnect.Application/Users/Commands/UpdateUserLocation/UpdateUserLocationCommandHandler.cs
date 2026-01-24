using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.UpdateUserLocation;

/// <summary>
/// Handler for UpdateUserLocationCommand
/// Updates user location or clears it if all location fields are null
/// </summary>
public class UpdateUserLocationCommandHandler : ICommandHandler<UpdateUserLocationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserLocationCommandHandler> _logger;

    public UpdateUserLocationCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserLocationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateUserLocationCommand command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateUserLocation"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", command.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateUserLocation START: UserId={UserId}, HasCity={HasCity}, HasState={HasState}, HasZipCode={HasZipCode}, HasCountry={HasCountry}",
                command.UserId, !string.IsNullOrWhiteSpace(command.City), !string.IsNullOrWhiteSpace(command.State),
                !string.IsNullOrWhiteSpace(command.ZipCode), !string.IsNullOrWhiteSpace(command.Country));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user
                var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserLocation FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "UpdateUserLocation: User loaded - UserId={UserId}, Email={Email}, HasCurrentLocation={HasCurrentLocation}",
                    user.Id, user.Email.Value, user.Location != null);

                // Check if we're clearing the location (all fields null) or setting it
                if (string.IsNullOrWhiteSpace(command.City) &&
                    string.IsNullOrWhiteSpace(command.State) &&
                    string.IsNullOrWhiteSpace(command.ZipCode) &&
                    string.IsNullOrWhiteSpace(command.Country))
                {
                    _logger.LogInformation(
                        "UpdateUserLocation: Clearing location (privacy choice) - UserId={UserId}",
                        user.Id);

                    // Clear location (privacy choice)
                    var clearResult = user.UpdateLocation(null);
                    if (!clearResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateUserLocation FAILED: Clear location failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                            user.Id, string.Join(", ", clearResult.Errors), stopwatch.ElapsedMilliseconds);

                        return Result.Failure(clearResult.Errors.ToArray());
                    }

                    _logger.LogInformation(
                        "UpdateUserLocation: Location cleared successfully - UserId={UserId}",
                        user.Id);
                }
                else
                {
                    _logger.LogInformation(
                        "UpdateUserLocation: Setting new location - City={City}, State={State}, ZipCode={ZipCode}, Country={Country}",
                        command.City, command.State, command.ZipCode, command.Country);

                    // Create and set new location
                    var locationResult = UserLocation.Create(
                        command.City!,
                        command.State!,
                        command.ZipCode!,
                        command.Country!);

                    if (!locationResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateUserLocation FAILED: Location creation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            user.Id, locationResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result.Failure(locationResult.Error);
                    }

                    _logger.LogInformation(
                        "UpdateUserLocation: Location value object created - UserId={UserId}",
                        user.Id);

                    var updateResult = user.UpdateLocation(locationResult.Value);
                    if (!updateResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateUserLocation FAILED: Domain validation failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                            user.Id, string.Join(", ", updateResult.Errors), stopwatch.ElapsedMilliseconds);

                        return Result.Failure(updateResult.Errors.ToArray());
                    }

                    _logger.LogInformation(
                        "UpdateUserLocation: Domain method succeeded - UserId={UserId}",
                        user.Id);
                }

                // Save changes
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateUserLocation COMPLETE: UserId={UserId}, HasLocation={HasLocation}, Duration={ElapsedMs}ms",
                    command.UserId, user.Location != null, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UpdateUserLocation CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateUserLocation FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
