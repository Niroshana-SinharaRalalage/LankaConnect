using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;

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

    public UpdateUserPreferredMetroAreasCommandHandler(
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateUserPreferredMetroAreasCommand command,
        CancellationToken cancellationToken)
    {
        // Get user with tracked metro area entities
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Phase 6A.9 FIX: Load MetroArea entities for EF Core persistence per ADR-009
        // Application layer validates existence (ADR-008)
        // Infrastructure layer provides entity references (ADR-009)
        List<Domain.Events.MetroArea> metroAreaEntities = new();

        if (command.MetroAreaIds.Any())
        {
            // Load actual entities from database
            metroAreaEntities = await _dbContext.MetroAreas
                .Where(m => command.MetroAreaIds.Contains(m.Id))
                .ToListAsync(cancellationToken);

            // Validate all IDs exist
            if (metroAreaEntities.Count != command.MetroAreaIds.Count())
            {
                var foundIds = metroAreaEntities.Select(m => m.Id).ToList();
                var invalidIds = command.MetroAreaIds.Except(foundIds).ToList();
                return Result.Failure($"Invalid metro area IDs: {string.Join(", ", invalidIds)}");
            }
        }

        // Update user's preferred metro areas
        // Pass both IDs (for domain logic) AND entities (for EF Core persistence per ADR-009)
        var updateResult = user.UpdatePreferredMetroAreas(
            command.MetroAreaIds,
            metroAreaEntities);  // NEW: Entity references for EF tracking

        if (!updateResult.IsSuccess)
        {
            return Result.Failure(updateResult.Errors.ToArray());
        }

        // Save changes - EF Core now detects changes via _preferredMetroAreaEntities shadow property
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
