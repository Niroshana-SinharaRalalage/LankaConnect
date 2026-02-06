using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.UpdateEmailGroup;

/// <summary>
/// Handler for UpdateEmailGroupCommand
/// Phase 6A.25: Updates an existing email group
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class UpdateEmailGroupCommandHandler : IRequestHandler<UpdateEmailGroupCommand, Result<EmailGroupDto>>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEmailGroupCommandHandler> _logger;

    public UpdateEmailGroupCommandHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEmailGroupCommandHandler> logger)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<EmailGroupDto>> Handle(UpdateEmailGroupCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEmailGroup"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        using (LogContext.PushProperty("EmailGroupId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEmailGroup START: EmailGroupId={EmailGroupId}, User={UserId}, Name={Name}",
                request.Id,
                _currentUserService.UserId,
                request.Name);

            try
            {
                // Validation: User is authenticated
                var userId = _currentUserService.UserId;
                if (userId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailGroup FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure("User must be authenticated to update an email group");
                }

                // Get the email group
                var emailGroup = await _emailGroupRepository.GetByIdAsync(request.Id, cancellationToken);
                if (emailGroup == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailGroup FAILED: Email group not found - EmailGroupId={EmailGroupId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure("Email group not found");
                }

                _logger.LogInformation(
                    "UpdateEmailGroup: Email group found - EmailGroupId={EmailGroupId}, OwnerId={OwnerId}, CurrentName={CurrentName}",
                    emailGroup.Id,
                    emailGroup.OwnerId,
                    emailGroup.Name);

                // Check ownership (unless admin)
                var isAdmin = _currentUserService.IsAdmin;
                if (!isAdmin && emailGroup.OwnerId != userId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailGroup FAILED: User does not have permission - EmailGroupId={EmailGroupId}, UserId={UserId}, OwnerId={OwnerId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        userId,
                        emailGroup.OwnerId,
                        isAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure("You don't have permission to update this email group");
                }

                // Check for duplicate name for this owner (excluding current group)
                var nameExists = await _emailGroupRepository.NameExistsForOwnerAsync(
                    emailGroup.OwnerId, request.Name, request.Id, cancellationToken);
                if (nameExists)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailGroup FAILED: Duplicate name - EmailGroupId={EmailGroupId}, Name={Name}, Duration={ElapsedMs}ms",
                        request.Id,
                        request.Name,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure($"An email group with the name '{request.Name}' already exists");
                }

                _logger.LogInformation(
                    "UpdateEmailGroup: Updating email group - EmailGroupId={EmailGroupId}, NewName={NewName}",
                    request.Id,
                    request.Name);

                // Update via domain method (validates emails)
                var updateResult = emailGroup.Update(request.Name, request.EmailAddresses, request.Description);
                if (!updateResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailGroup FAILED: Update validation failed - EmailGroupId={EmailGroupId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.Id,
                        string.Join("; ", updateResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure(updateResult.Errors);
                }

                // Save changes
                _emailGroupRepository.Update(emailGroup);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Get owner name for DTO
                var owner = await _userRepository.GetByIdAsync(emailGroup.OwnerId, cancellationToken);
                var ownerName = owner?.FullName ?? "Unknown";

                // Return DTO
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
                    "UpdateEmailGroup COMPLETE: EmailGroupId={EmailGroupId}, User={UserId}, Name={Name}, EmailCount={EmailCount}, Duration={ElapsedMs}ms",
                    emailGroup.Id,
                    userId,
                    emailGroup.Name,
                    emailGroup.GetEmailCount(),
                    stopwatch.ElapsedMilliseconds);

                return Result<EmailGroupDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateEmailGroup FAILED: Unexpected error - EmailGroupId={EmailGroupId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
