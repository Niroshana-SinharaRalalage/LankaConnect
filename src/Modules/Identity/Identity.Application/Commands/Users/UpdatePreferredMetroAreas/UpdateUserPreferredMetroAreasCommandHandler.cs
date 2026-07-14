using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Modules.Identity.Infrastructure.Data; // Wave 6.5.f mirror (2026-07-09 Day 4): IdentityDbContext
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // 4C.h Day 5: LankaEventsDbContext for MetroAreas (cross-module)
namespace LankaConnect.Modules.Identity.Application.Commands.Users.UpdatePreferredMetroAreas;

/// <summary>
/// Handler for UpdateUserPreferredMetroAreasCommand
/// Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
/// Architecture: Following ADR-008 - Validates metro area existence in Application layer
/// Domain layer validates business rules (max 20, no duplicates)
/// </summary>
public class UpdateUserPreferredMetroAreasCommandHandler : ICommandHandler<UpdateUserPreferredMetroAreasCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IdentityDbContext _dbContext;
    private readonly LankaEventsDbContext _eventsContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserPreferredMetroAreasCommandHandler> _logger;

    public UpdateUserPreferredMetroAreasCommandHandler(
        IUserRepository userRepository,
        IdentityDbContext dbContext,
        LankaEventsDbContext eventsContext,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserPreferredMetroAreasCommandHandler> logger)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _eventsContext = eventsContext;
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
                List<LankaConnect.Products.LankaEvents.Domain.MetroArea> metroAreaEntities = new();

                if (command.MetroAreaIds.Any())
                {
                    _logger.LogInformation(
                        "UpdatePreferredMetroAreas: Loading metro area entities - MetroAreaIds={MetroAreaIds}",
                        string.Join(", ", command.MetroAreaIds));

                    // Load actual entities from database
                    metroAreaEntities = await _eventsContext.MetroAreas
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

                // Sprint-Day 7 (2026-07-14) hotfix: replaced shadow-nav ChangeTracker write path
                // (which throws under IdentityDbContext because MetroArea is Ignored per Blueprint
                // §7.8) with direct raw-SQL junction upsert. The junction table
                // `identity.user_preferred_metro_areas` is owned by IdentityDbContext + declared
                // in UserConfiguration; writing to it via ExecuteSqlRaw sidesteps the shadow-nav
                // requirement while preserving atomicity within the User update transaction.
                _logger.LogInformation(
                    "UpdatePreferredMetroAreas: Rewriting junction rows via raw SQL - UserId={UserId}, NewCount={NewCount}",
                    user.Id, metroAreaEntities.Count);

                // Delete existing junction rows for this user.
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM identity.user_preferred_metro_areas WHERE user_id = {0}",
                    new object[] { user.Id },
                    cancellationToken);

                // Insert new rows (one per requested metro area).
                foreach (var metroArea in metroAreaEntities)
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO identity.user_preferred_metro_areas (user_id, metro_area_id) VALUES ({0}, {1})",
                        new object[] { user.Id, metroArea.Id },
                        cancellationToken);
                }

                // Persist the domain-side _preferredMetroAreaIds list (already updated by
                // user.UpdatePreferredMetroAreas above) — SaveChanges dispatches domain events.
                _userRepository.Update(user);
                await _dbContext.SaveChangesAsync(cancellationToken);

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
