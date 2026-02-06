using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.DeleteEmailGroup;

/// <summary>
/// Handler for DeleteEmailGroupCommand
/// Phase 6A.25: Soft deletes an email group by marking it inactive
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class DeleteEmailGroupCommandHandler : IRequestHandler<DeleteEmailGroupCommand, Result>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteEmailGroupCommandHandler> _logger;

    public DeleteEmailGroupCommandHandler(
        IEmailGroupRepository emailGroupRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteEmailGroupCommandHandler> logger)
    {
        _emailGroupRepository = emailGroupRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteEmailGroupCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteEmailGroup"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        using (LogContext.PushProperty("EmailGroupId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteEmailGroup START: EmailGroupId={EmailGroupId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validation: User is authenticated
                var userId = _currentUserService.UserId;
                if (userId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteEmailGroup FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result.Failure("User must be authenticated to delete an email group");
                }

                // Get the email group
                var emailGroup = await _emailGroupRepository.GetByIdAsync(request.Id, cancellationToken);
                if (emailGroup == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteEmailGroup FAILED: Email group not found - EmailGroupId={EmailGroupId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Email group not found");
                }

                _logger.LogInformation(
                    "DeleteEmailGroup: Email group found - EmailGroupId={EmailGroupId}, Name={Name}, OwnerId={OwnerId}, IsActive={IsActive}",
                    emailGroup.Id,
                    emailGroup.Name,
                    emailGroup.OwnerId,
                    emailGroup.IsActive);

                // Check ownership (unless admin)
                var isAdmin = _currentUserService.IsAdmin;
                if (!isAdmin && emailGroup.OwnerId != userId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteEmailGroup FAILED: User does not have permission - EmailGroupId={EmailGroupId}, UserId={UserId}, OwnerId={OwnerId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        userId,
                        emailGroup.OwnerId,
                        isAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result.Failure("You don't have permission to delete this email group");
                }

                // Soft delete via domain method
                _logger.LogInformation(
                    "DeleteEmailGroup: Deactivating email group - EmailGroupId={EmailGroupId}",
                    request.Id);

                emailGroup.Deactivate();

                // Save changes
                _emailGroupRepository.Update(emailGroup);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "DeleteEmailGroup COMPLETE: EmailGroupId={EmailGroupId}, User={UserId}, Name={Name}, Duration={ElapsedMs}ms",
                    request.Id,
                    userId,
                    emailGroup.Name,
                    stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteEmailGroup FAILED: Unexpected error - EmailGroupId={EmailGroupId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
