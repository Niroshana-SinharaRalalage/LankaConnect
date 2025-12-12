using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Users;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.UpdateEmailGroup;

/// <summary>
/// Handler for UpdateEmailGroupCommand
/// Phase 6A.25: Updates an existing email group
/// </summary>
public class UpdateEmailGroupCommandHandler : IRequestHandler<UpdateEmailGroupCommand, Result<EmailGroupDto>>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailGroupCommandHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EmailGroupDto>> Handle(UpdateEmailGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate user is authenticated
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
            return Result<EmailGroupDto>.Failure("User must be authenticated to update an email group");

        // 2. Get the email group
        var emailGroup = await _emailGroupRepository.GetByIdAsync(request.Id, cancellationToken);
        if (emailGroup == null)
            return Result<EmailGroupDto>.Failure("Email group not found");

        // 3. Check ownership (unless admin)
        var isAdmin = _currentUserService.IsAdmin;
        if (!isAdmin && emailGroup.OwnerId != userId)
            return Result<EmailGroupDto>.Failure("You don't have permission to update this email group");

        // 4. Check for duplicate name for this owner (excluding current group)
        var nameExists = await _emailGroupRepository.NameExistsForOwnerAsync(
            emailGroup.OwnerId, request.Name, request.Id, cancellationToken);
        if (nameExists)
            return Result<EmailGroupDto>.Failure($"An email group with the name '{request.Name}' already exists");

        // 5. Update via domain method (validates emails)
        var updateResult = emailGroup.Update(request.Name, request.EmailAddresses, request.Description);
        if (!updateResult.IsSuccess)
            return Result<EmailGroupDto>.Failure(updateResult.Errors);

        // 6. Save changes
        _emailGroupRepository.Update(emailGroup);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 7. Get owner name for DTO
        var owner = await _userRepository.GetByIdAsync(emailGroup.OwnerId, cancellationToken);
        var ownerName = owner?.FullName ?? "Unknown";

        // 8. Return DTO
        var dto = new EmailGroupDto
        {
            Id = emailGroup.Id,
            Name = emailGroup.Name,
            Description = emailGroup.Description,
            OwnerId = emailGroup.OwnerId,
            OwnerName = ownerName,
            EmailAddresses = emailGroup.EmailAddresses,
            EmailCount = emailGroup.GetEmailCount(),
            IsActive = emailGroup.IsActive,
            CreatedAt = emailGroup.CreatedAt,
            UpdatedAt = emailGroup.UpdatedAt
        };

        return Result<EmailGroupDto>.Success(dto);
    }
}
