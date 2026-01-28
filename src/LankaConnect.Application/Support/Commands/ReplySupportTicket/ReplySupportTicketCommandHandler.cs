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

namespace LankaConnect.Application.Support.Commands.ReplySupportTicket;

/// <summary>
/// Handler for ReplySupportTicketCommand
/// Phase 6A.90: Adds admin reply to ticket and sends email notification
/// </summary>
public class ReplySupportTicketCommandHandler : ICommandHandler<ReplySupportTicketCommand>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReplySupportTicketCommandHandler> _logger;

    public ReplySupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<ReplySupportTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _urlsService = urlsService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ReplySupportTicketCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ReplySupportTicket"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("TicketId", request.TicketId))
        using (LogContext.PushProperty("AdminUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ReplySupportTicket START: TicketId={TicketId}, AdminUserId={AdminUserId}",
                request.TicketId, _currentUserService.UserId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get admin user to verify permissions
                var adminUser = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
                if (adminUser == null)
                {
                    _logger.LogWarning(
                        "ReplySupportTicket FAILED: Admin user not found - AdminUserId={AdminUserId}",
                        _currentUserService.UserId);
                    return Result.Failure("Admin user not found");
                }

                if (adminUser.Role != UserRole.Admin && adminUser.Role != UserRole.AdminManager)
                {
                    _logger.LogWarning(
                        "ReplySupportTicket FAILED: Insufficient permissions - AdminUserId={AdminUserId}, Role={Role}",
                        _currentUserService.UserId, adminUser.Role);
                    return Result.Failure("Insufficient permissions to perform this action");
                }

                // Get the ticket
                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
                if (ticket == null)
                {
                    _logger.LogWarning(
                        "ReplySupportTicket FAILED: Ticket not found - TicketId={TicketId}",
                        request.TicketId);
                    return Result.Failure("Support ticket not found");
                }

                _logger.LogInformation(
                    "ReplySupportTicket: Adding reply - TicketId={TicketId}, ReferenceId={ReferenceId}",
                    ticket.Id, ticket.ReferenceId);

                // Add reply
                var result = ticket.AddReply(request.Content, _currentUserService.UserId);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "ReplySupportTicket FAILED: Domain validation failed - TicketId={TicketId}, Error={Error}",
                        request.TicketId, result.Error);
                    return result;
                }

                // Create audit log
                var auditDetails = JsonSerializer.Serialize(new
                {
                    ReferenceId = ticket.ReferenceId,
                    SubmitterEmail = ticket.Email.Value,
                    ReplyContentLength = request.Content.Length
                });

                var auditLog = AdminAuditLog.CreateForTicketAction(
                    _currentUserService.UserId,
                    AdminAuditActions.TicketReplied,
                    ticket.Id,
                    auditDetails,
                    request.IpAddress,
                    request.UserAgent);

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send reply notification email (fail-silent)
                await SendReplyEmailAsync(ticket, adminUser.FullName, request.Content, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ReplySupportTicket COMPLETE: TicketId={TicketId}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    request.TicketId, _currentUserService.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ReplySupportTicket CANCELED: TicketId={TicketId}, Duration={ElapsedMs}ms",
                    request.TicketId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ReplySupportTicket FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TicketId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task SendReplyEmailAsync(SupportTicket ticket, string adminName, string replyContent, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "Name", ticket.Name },
                { "ReferenceId", ticket.ReferenceId },
                { "Subject", ticket.Subject },
                { "ReplyContent", replyContent },
                { "AdminName", adminName },
                { "RepliedAt", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") + " UTC" }
            };

            _logger.LogInformation(
                "[Phase 6A.90] Sending support ticket reply email to {Email}, ReferenceId={ReferenceId}",
                ticket.Email.Value, ticket.ReferenceId);

            var result = await _emailService.SendTemplatedEmailAsync(
                "template-support-ticket-reply",
                ticket.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.90] Support ticket reply email sent successfully to {Email}",
                    ticket.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.90] Failed to send support ticket reply email to {Email}: {Errors}",
                    ticket.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.90] Error sending support ticket reply email to {Email}",
                ticket.Email.Value);
        }
    }
}
