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

namespace LankaConnect.Application.Support.Commands.AssignSupportTicket;

/// <summary>
/// Handler for AssignSupportTicketCommand
/// Phase 6A.90: Assigns support ticket to an admin user
/// </summary>
public class AssignSupportTicketCommandHandler : ICommandHandler<AssignSupportTicketCommand>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignSupportTicketCommandHandler> _logger;

    public AssignSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AssignSupportTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignSupportTicketCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AssignSupportTicket"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("TicketId", request.TicketId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AssignSupportTicket START: TicketId={TicketId}, AssignTo={AssignTo}, AdminUserId={AdminUserId}",
                request.TicketId, request.AssignToUserId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Verify admin permissions
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AssignSupportTicket FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AssignSupportTicket FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Verify assignee is an admin
                var assignee = await _userRepository.GetByIdAsync(request.AssignToUserId, cancellationToken);
                if (assignee == null)
                {
                    _logger.LogWarning(
                        "AssignSupportTicket FAILED: Assignee not found - AssignTo={AssignTo}",
                        request.AssignToUserId);
                    return Result.Failure("Assignee user not found");
                }

                if (assignee.Role != UserRole.Admin && assignee.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AssignSupportTicket FAILED: Assignee is not an admin - AssignTo={AssignTo}, Role={Role}",
                        request.AssignToUserId, assignee.Role);
                    return Result.Failure("Tickets can only be assigned to admin users");
                }

                // Get the ticket
                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
                if (ticket == null)
                {
                    _logger.LogWarning(
                        "AssignSupportTicket FAILED: Ticket not found - TicketId={TicketId}",
                        request.TicketId);
                    return Result.Failure("Support ticket not found");
                }

                var previousAssignee = ticket.AssignedToUserId;

                _logger.LogInformation(
                    "AssignSupportTicket: Assigning ticket - TicketId={TicketId}, PreviousAssignee={PreviousAssignee}, NewAssignee={NewAssignee}",
                    ticket.Id, previousAssignee, request.AssignToUserId);

                // Assign ticket
                var result = ticket.AssignTo(request.AssignToUserId);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AssignSupportTicket FAILED: Domain validation failed - TicketId={TicketId}, Error={Error}",
                        request.TicketId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    ReferenceId = ticket.ReferenceId,
                    PreviousAssignee = previousAssignee?.ToString(),
                    NewAssignee = request.AssignToUserId.ToString(),
                    AssigneeName = assignee.FullName
                });

                var auditLog = AdminAuditLog.CreateForTicketAction(
                    _currentUserService.UserId,
                    AdminAuditActions.TicketAssigned,
                    ticket.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AssignSupportTicket COMPLETE: TicketId={TicketId}, AssignedTo={AssignedTo}, Duration={ElapsedMs}ms",
                    request.TicketId, request.AssignToUserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AssignSupportTicket CANCELED: TicketId={TicketId}, Duration={ElapsedMs}ms",
                    request.TicketId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AssignSupportTicket FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TicketId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
