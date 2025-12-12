using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Communications.Queries.GetEmailGroupById;

/// <summary>
/// Handler for GetEmailGroupByIdQuery
/// Phase 6A.25: Returns a single email group with access control
/// </summary>
public class GetEmailGroupByIdQueryHandler : IQueryHandler<GetEmailGroupByIdQuery, EmailGroupDto>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmailGroupByIdQueryHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<EmailGroupDto>> Handle(GetEmailGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsAdmin;

        // Get the email group
        var emailGroup = await _emailGroupRepository.GetByIdAsync(request.Id, cancellationToken);
        if (emailGroup == null)
            return Result<EmailGroupDto>.Failure("Email group not found");

        // Check access rights (owner or admin)
        if (!isAdmin && emailGroup.OwnerId != userId)
            return Result<EmailGroupDto>.Failure("You don't have permission to view this email group");

        // Get owner name
        var owner = await _userRepository.GetByIdAsync(emailGroup.OwnerId, cancellationToken);
        var ownerName = owner?.FullName ?? "Unknown";

        // Map to DTO
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
