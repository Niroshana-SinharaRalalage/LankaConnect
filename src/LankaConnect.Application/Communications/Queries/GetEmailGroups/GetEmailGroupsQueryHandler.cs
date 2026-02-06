using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<GetEmailGroupsQueryHandler> _logger;

    public GetEmailGroupsQueryHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<GetEmailGroupsQueryHandler> logger)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EmailGroupDto>>> Handle(GetEmailGroupsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEmailGroups"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = _currentUserService.UserId;
            var isAdmin = _currentUserService.IsAdmin;

            _logger.LogInformation(
                "GetEmailGroups START: RequesterId={RequesterId}, IsAdmin={IsAdmin}, IncludeAll={IncludeAll}",
                userId, isAdmin, request.IncludeAll);

            try
            {
                IReadOnlyList<EmailGroup> emailGroups;

                // Admin can see all groups when IncludeAll is true
                if (isAdmin && request.IncludeAll)
                {
                    emailGroups = await _emailGroupRepository.GetAllActiveAsync(cancellationToken);

                    _logger.LogInformation(
                        "GetEmailGroups: Admin fetched all groups - GroupCount={GroupCount}",
                        emailGroups.Count);
                }
                else
                {
                    // Regular users (or admin not requesting all) see only their own groups
                    emailGroups = await _emailGroupRepository.GetByOwnerAsync(userId, cancellationToken);

                    _logger.LogInformation(
                        "GetEmailGroups: Fetched user's groups - RequesterId={RequesterId}, GroupCount={GroupCount}",
                        userId, emailGroups.Count);
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

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEmailGroups COMPLETE: RequesterId={RequesterId}, GroupCount={GroupCount}, OwnerCount={OwnerCount}, Duration={ElapsedMs}ms",
                    userId, dtos.Count, ownerIds.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EmailGroupDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEmailGroups FAILED: Exception occurred - RequesterId={RequesterId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    userId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
