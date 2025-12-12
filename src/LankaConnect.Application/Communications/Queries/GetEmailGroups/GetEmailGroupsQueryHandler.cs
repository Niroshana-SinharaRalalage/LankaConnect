using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Communications.Queries.GetEmailGroups;

/// <summary>
/// Handler for GetEmailGroupsQuery
/// Phase 6A.25: Returns email groups based on user role and query parameters
/// </summary>
public class GetEmailGroupsQueryHandler : IQueryHandler<GetEmailGroupsQuery, IReadOnlyList<EmailGroupDto>>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmailGroupsQueryHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<EmailGroupDto>>> Handle(GetEmailGroupsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsAdmin;

        IReadOnlyList<EmailGroup> emailGroups;

        // Admin can see all groups when IncludeAll is true
        if (isAdmin && request.IncludeAll)
        {
            emailGroups = await _emailGroupRepository.GetAllActiveAsync(cancellationToken);
        }
        else
        {
            // Regular users (or admin not requesting all) see only their own groups
            emailGroups = await _emailGroupRepository.GetByOwnerAsync(userId, cancellationToken);
        }

        // Get all unique owner IDs for batch lookup
        var ownerIds = emailGroups.Select(g => g.OwnerId).Distinct().ToList();
        var owners = new Dictionary<Guid, string>();

        foreach (var ownerId in ownerIds)
        {
            var owner = await _userRepository.GetByIdAsync(ownerId, cancellationToken);
            owners[ownerId] = owner?.FullName ?? "Unknown";
        }

        // Map to DTOs
        var dtos = emailGroups
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new EmailGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                OwnerId = g.OwnerId,
                OwnerName = owners.GetValueOrDefault(g.OwnerId, "Unknown"),
                EmailAddresses = g.EmailAddresses,
                EmailCount = g.GetEmailCount(),
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .ToList();

        return Result<IReadOnlyList<EmailGroupDto>>.Success(dtos);
    }
}
