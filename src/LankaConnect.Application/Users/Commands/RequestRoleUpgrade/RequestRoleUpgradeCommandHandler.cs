using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Users.Commands.RequestRoleUpgrade;

/// <summary>
/// Handler for RequestRoleUpgradeCommand
/// Phase 6A.7: User Upgrade Workflow - Allows users to request role upgrade
/// </summary>
public class RequestRoleUpgradeCommandHandler : ICommandHandler<RequestRoleUpgradeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RequestRoleUpgradeCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RequestRoleUpgradeCommand request, CancellationToken cancellationToken)
    {
        // Get current user
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            return Result.Failure("User must be authenticated");

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
            return Result.Failure("User not found");

        // Validate reason is provided
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure("Reason is required for role upgrade request");

        // Request the role upgrade (domain validates business rules)
        var result = user.SetPendingUpgradeRole(request.TargetRole);
        if (result.IsFailure)
            return result;

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
