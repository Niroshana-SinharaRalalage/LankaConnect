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

namespace LankaConnect.Application.Support.Commands.AddSupportTicketNote;

/// <summary>
/// Handler for AddSupportTicketNoteCommand
/// Phase 6A.90: Adds internal note to ticket (not visible to submitter)
/// </summary>
public class AddSupportTicketNoteCommandHandler : ICommandHandler<AddSupportTicketNoteCommand>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddSupportTicketNoteCommandHandler> _logger;

    public AddSupportTicketNoteCommandHandler(
        ISupportTicketRepository ticketRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AddSupportTicketNoteCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AddSupportTicketNoteCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddSupportTicketNote"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("TicketId", request.TicketId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddSupportTicketNote START: TicketId={TicketId}, AdminUserId={AdminUserId}",
                request.TicketId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Verify admin permissions
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "AddSupportTicketNote FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "AddSupportTicketNote FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get the ticket
                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
                if (ticket == null)
                {
                    _logger.LogWarning(
                        "AddSupportTicketNote FAILED: Ticket not found - TicketId={TicketId}",
                        request.TicketId);
                    return Result.Failure("Support ticket not found");
                }

                _logger.LogInformation(
                    "AddSupportTicketNote: Adding note - TicketId={TicketId}, ReferenceId={ReferenceId}",
                    ticket.Id, ticket.ReferenceId);

                // Add note
                var result = ticket.AddNote(request.Content, _currentUserService.UserId);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "AddSupportTicketNote FAILED: Domain validation failed - TicketId={TicketId}, Error={Error}",
                        request.TicketId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    ReferenceId = ticket.ReferenceId,
                    NoteContentLength = request.Content.Length
                });

                var auditLog = AdminAuditLog.CreateForTicketAction(
                    _currentUserService.UserId,
                    AdminAuditActions.TicketNoteAdded,
                    ticket.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddSupportTicketNote COMPLETE: TicketId={TicketId}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.TicketId, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "AddSupportTicketNote CANCELED: TicketId={TicketId}, Duration={ElapsedMs}ms",
                    request.TicketId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AddSupportTicketNote FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TicketId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
