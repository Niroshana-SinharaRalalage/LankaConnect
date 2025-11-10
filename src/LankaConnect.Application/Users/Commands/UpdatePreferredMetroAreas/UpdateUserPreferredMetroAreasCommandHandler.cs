using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

/// <summary>
/// Handler for UpdateUserPreferredMetroAreasCommand
/// Phase 5A: User Preferred Metro Areas
/// Architecture: Following ADR-008 - Validates metro area existence in Application layer
/// Domain layer validates business rules (max 10, no duplicates)
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
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        // Validate metro area IDs exist (Application layer validation per ADR-008)
        if (command.MetroAreaIds.Any())
        {
            var existingMetroAreaIds = await _dbContext.MetroAreas
                .Where(m => command.MetroAreaIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var invalidMetroAreaIds = command.MetroAreaIds.Except(existingMetroAreaIds).ToList();
            if (invalidMetroAreaIds.Any())
            {
                return Result.Failure($"Invalid metro area IDs: {string.Join(", ", invalidMetroAreaIds)}");
            }
        }

        // Update user's preferred metro areas (domain will validate max 10 and duplicates)
        var updateResult = user.UpdatePreferredMetroAreas(command.MetroAreaIds);
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
