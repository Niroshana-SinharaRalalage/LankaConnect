using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.DeleteEmailGroup;

/// <summary>
/// Handler for DeleteEmailGroupCommand
/// Phase 6A.25: Soft deletes an email group by marking it inactive
/// </summary>
public class DeleteEmailGroupCommandHandler : IRequestHandler<DeleteEmailGroupCommand, Result>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmailGroupCommandHandler(
        IEmailGroupRepository emailGroupRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _emailGroupRepository = emailGroupRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteEmailGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate user is authenticated
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
            return Result.Failure("User must be authenticated to delete an email group");

        // 2. Get the email group
        var emailGroup = await _emailGroupRepository.GetByIdAsync(request.Id, cancellationToken);
        if (emailGroup == null)
            return Result.Failure("Email group not found");

        // 3. Check ownership (unless admin)
        var isAdmin = _currentUserService.IsAdmin;
        if (!isAdmin && emailGroup.OwnerId != userId)
            return Result.Failure("You don't have permission to delete this email group");

        // 4. Soft delete via domain method
        emailGroup.Deactivate();

        // 5. Save changes
        _emailGroupRepository.Update(emailGroup);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
