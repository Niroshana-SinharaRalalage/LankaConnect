using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket;

/// <summary>
/// Phase 6A.141 — admin override: reverses a prior accepted scan so the ticket can
/// be scanned again. The original accepted audit row stays; this handler writes a
/// new <c>scan_result = 'unmarked'</c> row carrying the admin's stated reason.
/// Both rows transition in one transaction so the state change and audit binding
/// are atomic.
/// </summary>
public class UnmarkScannedCommandHandler : ICommandHandler<UnmarkScannedCommand, UnmarkScannedResult>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketScanLogRepository _scanLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnmarkScannedCommandHandler> _logger;

    public UnmarkScannedCommandHandler(
        ITicketRepository ticketRepository,
        ITicketScanLogRepository scanLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnmarkScannedCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _scanLogRepository = scanLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UnmarkScannedResult>> Handle(UnmarkScannedCommand command, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UnmarkScanned"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("EventId", command.EventId))
        using (LogContext.PushProperty("TicketCode", command.TicketCode))
        using (LogContext.PushProperty("AdminUserId", command.AdminUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "UnmarkScanned START: EventId={EventId}, TicketCode={TicketCode}, AdminUserId={AdminUserId}",
                command.EventId, command.TicketCode, command.AdminUserId);

            try
            {
                if (string.IsNullOrWhiteSpace(command.Reason))
                {
                    return Result<UnmarkScannedResult>.Failure("A reason is required for an admin unmark.");
                }

                var ticket = await _ticketRepository.GetByTicketCodeAsync(command.TicketCode, cancellationToken);
                if (ticket is null)
                {
                    return Result<UnmarkScannedResult>.NotFound($"Ticket {command.TicketCode} not found.");
                }

                if (ticket.EventId != command.EventId)
                {
                    return Result<UnmarkScannedResult>.Forbidden(
                        "Ticket belongs to a different event; admin scope must match the ticket's event.");
                }

                var unmarkResult = ticket.UnmarkScanned();
                if (unmarkResult.IsFailure)
                {
                    return Result<UnmarkScannedResult>.Failure(unmarkResult.Errors.First());
                }

                _ticketRepository.Update(ticket);

                var auditLog = TicketScanLog.AdminUnmark(
                    ticketId: ticket.Id,
                    eventId: command.EventId,
                    ticketCode: ticket.TicketCode,
                    scannerUserId: command.AdminUserId,
                    scannerName: command.AdminName,
                    reason: command.Reason,
                    clientIp: command.ClientIp,
                    userAgent: command.UserAgent);
                await _scanLogRepository.AddAsync(auditLog, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                sw.Stop();
                var now = DateTime.UtcNow;
                _logger.LogInformation(
                    "UnmarkScanned COMPLETE: TicketCode={TicketCode}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms",
                    command.TicketCode, command.AdminUserId, sw.ElapsedMilliseconds);

                return Result<UnmarkScannedResult>.Success(new UnmarkScannedResult(ticket.TicketCode, now));
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "UnmarkScanned FAILED: TicketCode={TicketCode}, AdminUserId={AdminUserId}, Duration={ElapsedMs}ms, Error={Error}",
                    command.TicketCode, command.AdminUserId, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
