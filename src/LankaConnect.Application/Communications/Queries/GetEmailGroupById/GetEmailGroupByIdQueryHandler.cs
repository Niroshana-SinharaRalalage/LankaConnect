using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<GetEmailGroupByIdQueryHandler> _logger;

    public GetEmailGroupByIdQueryHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<GetEmailGroupByIdQueryHandler> logger)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<EmailGroupDto>> Handle(GetEmailGroupByIdQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEmailGroupById"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        using (LogContext.PushProperty("EmailGroupId", request.Id))
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = _currentUserService.UserId;
            var isAdmin = _currentUserService.IsAdmin;

            _logger.LogInformation(
                "GetEmailGroupById START: EmailGroupId={EmailGroupId}, RequesterId={RequesterId}, IsAdmin={IsAdmin}",
                request.Id, userId, isAdmin);

            try
            {
                // Validate request
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEmailGroupById FAILED: Invalid EmailGroupId - EmailGroupId={EmailGroupId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result<EmailGroupDto>.Failure("Email group ID is required");
                }

                // Get the email group
                var emailGroup = await _emailGroupRepository.GetByIdAsync(request.Id, cancellationToken);
                if (emailGroup == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEmailGroupById FAILED: EmailGroup not found - EmailGroupId={EmailGroupId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result<EmailGroupDto>.Failure("Email group not found");
                }

                // Check access rights (owner or admin)
                if (!isAdmin && emailGroup.OwnerId != userId)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEmailGroupById FAILED: Access denied - EmailGroupId={EmailGroupId}, RequesterId={RequesterId}, OwnerId={OwnerId}, Duration={ElapsedMs}ms",
                        request.Id, userId, emailGroup.OwnerId, stopwatch.ElapsedMilliseconds);

                    return Result<EmailGroupDto>.Failure("You don't have permission to view this email group");
                }

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

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEmailGroupById COMPLETE: EmailGroupId={EmailGroupId}, Name={GroupName}, EmailCount={EmailCount}, IsActive={IsActive}, Duration={ElapsedMs}ms",
                    request.Id, emailGroup.Name, emailGroup.GetEmailCount(), emailGroup.IsActive, stopwatch.ElapsedMilliseconds);

                return Result<EmailGroupDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEmailGroupById FAILED: Exception occurred - EmailGroupId={EmailGroupId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
