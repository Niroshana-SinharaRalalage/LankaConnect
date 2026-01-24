using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

/// <summary>
/// Handler for UpdateUserPreferredMetroAreasCommand
/// Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
/// Architecture: Following ADR-008 - Validates metro area existence in Application layer
/// Domain layer validates business rules (max 20, no duplicates)
/// </summary>
public class UpdateUserPreferredMetroAreasCommandHandler : ICommandHandler<UpdateUserPreferredMetroAreasCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserPreferredMetroAreasCommandHandler> _logger;

    public UpdateUserPreferredMetroAreasCommandHandler(
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserPreferredMetroAreasCommandHandler> logger)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateUserPreferredMetroAreasCommand command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdatePreferredMetroAreas"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", command.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdatePreferredMetroAreas START: UserId={UserId}, MetroAreaCount={MetroAreaCount}",
                command.UserId, command.MetroAreaIds.Count());

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user with tracked metro area entities
                var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdatePreferredMetroAreas FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        command.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("User not found");
                }

                _logger.LogInformation(
                    "UpdatePreferredMetroAreas: User loaded - UserId={UserId}, Email={Email}",
                    user.Id, user.Email.Value);

                // Phase 6A.9 FIX: Load MetroArea entities for EF Core persistence per ADR-009
                // Application layer validates existence (ADR-008)
                // Infrastructure layer provides entity references (ADR-009)
                List<Domain.Events.MetroArea> metroAreaEntities = new();

                if (command.MetroAreaIds.Any())
                {
                    _logger.LogInformation(
                        "UpdatePreferredMetroAreas: Loading metro area entities - MetroAreaIds={MetroAreaIds}",
                        string.Join(", ", command.MetroAreaIds));

                    // Load actual entities from database
                    metroAreaEntities = await _dbContext.MetroAreas
                        .Where(m => command.MetroAreaIds.Contains(m.Id))
                        .ToListAsync(cancellationToken);

                    _logger.LogInformation(
                        "UpdatePreferredMetroAreas: Metro areas loaded - RequestedCount={RequestedCount}, FoundCount={FoundCount}",
                        command.MetroAreaIds.Count(), metroAreaEntities.Count);

                    // Validate all IDs exist
                    if (metroAreaEntities.Count != command.MetroAreaIds.Count())
                    {
                        stopwatch.Stop();

                        var foundIds = metroAreaEntities.Select(m => m.Id).ToList();
                        var invalidIds = command.MetroAreaIds.Except(foundIds).ToList();

                        _logger.LogWarning(
                            "UpdatePreferredMetroAreas FAILED: Invalid metro area IDs - UserId={UserId}, InvalidIds={InvalidIds}, Duration={ElapsedMs}ms",
                            command.UserId, string.Join(", ", invalidIds), stopwatch.ElapsedMilliseconds);

                        return Result.Failure($"Invalid metro area IDs: {string.Join(", ", invalidIds)}");
                    }

                    _logger.LogInformation(
                        "UpdatePreferredMetroAreas: All metro area IDs validated - UserId={UserId}, ValidCount={ValidCount}",
                        user.Id, metroAreaEntities.Count);
                }
                else
                {
                    _logger.LogInformation(
                        "UpdatePreferredMetroAreas: Clearing all metro areas - UserId={UserId}",
                        user.Id);
                }

                // Update user's preferred metro areas (domain validation only - no entities)
                var updateResult = user.UpdatePreferredMetroAreas(command.MetroAreaIds);

                if (!updateResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdatePreferredMetroAreas FAILED: Domain validation failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        command.UserId, string.Join(", ", updateResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result.Failure(updateResult.Errors.ToArray());
                }

                _logger.LogInformation(
                    "UpdatePreferredMetroAreas: Domain method succeeded - UserId={UserId}",
                    user.Id);

                // CRITICAL FIX Phase 6A.9: Use EF Core ChangeTracker API to update shadow navigation
                // We cannot modify shadow navigation from domain layer - must use EF Core's API
                // This is the CORRECT way to handle many-to-many with shadow properties per ADR-009
                // Cast to AppDbContext to access Entry() method (infrastructure layer detail)
                var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
                    ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                _logger.LogInformation(
                    "UpdatePreferredMetroAreas: Using EF Core ChangeTracker API to update shadow navigation - UserId={UserId}",
                    user.Id);

                var metroAreasCollection = dbContext.Entry(user).Collection("_preferredMetroAreaEntities");
                await metroAreasCollection.LoadAsync(cancellationToken);  // Ensure tracked

                var currentMetroAreas = metroAreasCollection.CurrentValue as ICollection<Domain.Events.MetroArea>
                    ?? new List<Domain.Events.MetroArea>();

                _logger.LogInformation(
                    "UpdatePreferredMetroAreas: Shadow navigation loaded - CurrentCount={CurrentCount}",
                    currentMetroAreas.Count);

                // Clear existing and add new entities using EF Core's tracked collection
                currentMetroAreas.Clear();

                foreach (var metroArea in metroAreaEntities)
                {
                    currentMetroAreas.Add(metroArea);
                }

                _logger.LogInformation(
                    "UpdatePreferredMetroAreas: Shadow navigation updated - NewCount={NewCount}",
                    currentMetroAreas.Count);

                // Save changes - EF Core now detects changes via ChangeTracker
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdatePreferredMetroAreas COMPLETE: UserId={UserId}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    command.UserId, metroAreaEntities.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UpdatePreferredMetroAreas CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    command.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdatePreferredMetroAreas FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
