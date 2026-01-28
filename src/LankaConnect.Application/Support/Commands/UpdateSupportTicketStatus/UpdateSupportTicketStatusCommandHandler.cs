using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Support.Commands.UpdateSupportTicketStatus;

/// <summary>
/// Handler for UpdateSupportTicketStatusCommand
/// Phase 6A.90: Updates support ticket status with audit logging
/// </summary>
public class UpdateSupportTicketStatusCommandHandler : ICommandHandler<UpdateSupportTicketStatusCommand>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSupportTicketStatusCommandHandler> _logger;

    public UpdateSupportTicketStatusCommandHandler(
        ISupportTicketRepository ticketRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSupportTicketStatusCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSupportTicketStatusCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateSupportTicketStatus"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("TicketId", request.TicketId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateSupportTicketStatus START: TicketId={TicketId}, NewStatus={NewStatus}, AdminUserId={AdminUserId}",
                request.TicketId, request.NewStatus, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Verify admin permissions
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "UpdateSupportTicketStatus FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "UpdateSupportTicketStatus FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get the ticket
                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
                if (ticket == null)
                {
                    _logger.LogWarning(
                        "UpdateSupportTicketStatus FAILED: Ticket not found - TicketId={TicketId}",
                        request.TicketId);
                    return Result.Failure("Support ticket not found");
                }

                var oldStatus = ticket.Status;

                _logger.LogInformation(
                    "UpdateSupportTicketStatus: Updating status - TicketId={TicketId}, OldStatus={OldStatus}, NewStatus={NewStatus}",
                    ticket.Id, oldStatus, request.NewStatus);

                // Update status
                var result = ticket.UpdateStatus(request.NewStatus);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "UpdateSupportTicketStatus FAILED: Domain validation failed - TicketId={TicketId}, Error={Error}",
                        request.TicketId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    ReferenceId = ticket.ReferenceId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = request.NewStatus.ToString()
                });

                var auditLog = AdminAuditLog.CreateForTicketAction(
                    _currentUserService.UserId,
                    AdminAuditActions.TicketStatusChanged,
                    ticket.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateSupportTicketStatus COMPLETE: TicketId={TicketId}, OldStatus={OldStatus}, NewStatus={NewStatus}, Duration={ElapsedMs}ms",
                    request.TicketId, oldStatus, request.NewStatus, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "UpdateSupportTicketStatus CANCELED: TicketId={TicketId}, Duration={ElapsedMs}ms",
                    request.TicketId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateSupportTicketStatus FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TicketId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
