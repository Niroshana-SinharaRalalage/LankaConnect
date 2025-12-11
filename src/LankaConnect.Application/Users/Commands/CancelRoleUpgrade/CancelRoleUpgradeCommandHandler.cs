using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Users.Commands.CancelRoleUpgrade;

/// <summary>
/// Handler for CancelRoleUpgradeCommand
/// Phase 6A.7: User Upgrade Workflow - Allows users to cancel their pending upgrade request
/// </summary>
public class CancelRoleUpgradeCommandHandler : ICommandHandler<CancelRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CancelRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        // Get current user
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            return Result.Failure("User must be authenticated");

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
            return Result.Failure("User not found");

        // Cancel the role upgrade (domain validates business rules)
        var result = user.CancelRoleUpgrade();
        if (result.IsFailure)
            return result;

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
