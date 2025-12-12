using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Users;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.CreateEmailGroup;

/// <summary>
/// Handler for CreateEmailGroupCommand
/// Phase 6A.25: Creates a new email group for the current user
/// </summary>
public class CreateEmailGroupCommandHandler : IRequestHandler<CreateEmailGroupCommand, Result<EmailGroupDto>>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmailGroupCommandHandler(
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

    public async Task<Result<EmailGroupDto>> Handle(CreateEmailGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate user is authenticated
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
            return Result<EmailGroupDto>.Failure("User must be authenticated to create an email group");

        // 2. Check for duplicate name for this owner
        var nameExists = await _emailGroupRepository.NameExistsForOwnerAsync(userId, request.Name, null, cancellationToken);
        if (nameExists)
            return Result<EmailGroupDto>.Failure($"An email group with the name '{request.Name}' already exists");

        // 3. Create email group via domain factory method (validates emails)
        var createResult = EmailGroup.Create(
            request.Name,
            userId,
            request.EmailAddresses,
            request.Description);

        if (!createResult.IsSuccess)
            return Result<EmailGroupDto>.Failure(createResult.Errors);

        var emailGroup = createResult.Value;

        // 4. Save to repository
        await _emailGroupRepository.AddAsync(emailGroup, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 5. Get owner name for DTO
        var owner = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var ownerName = owner?.FullName ?? "Unknown";

        // 6. Return DTO
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
