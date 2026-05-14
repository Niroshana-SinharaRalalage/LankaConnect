using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.ScanTicket;

/// <summary>
/// Phase 6A.141 — paid-event ticket scan command handler.
///
/// Flow (per Plan-agent review F1 + F2 + F13):
/// <list type="number">
///   <item>Validate command shape (exactly one of QrPayload / TicketCode).</item>
///   <item>Resolve the event from URL — 404 path if it doesn't exist.</item>
///   <item>If QR scan: parse v1/legacy payload + HMAC-verify (v1 only). Rejections audit OUTSIDE TX.</item>
///   <item>Look up ticket by code. Rejection audits OUTSIDE TX.</item>
///   <item>Verify event match (rejects <c>wrong_event</c> with the actual event's title for staff context).</item>
///   <item>Synchronously check state — invalidated / expired / already-scanned reject early.
///         These read-time checks save an UPDATE round-trip for known-bad states.</item>
///   <item>Open transaction. Run race-safe atomic <c>TryMarkScannedAsync</c>. RowsAffected==0 means
///         someone else won the race between our read and UPDATE → rollback, classify as
///         <c>already_scanned</c>, audit OUTSIDE the rolled-back transaction.</item>
///   <item>RowsAffected==1: write accepted audit IN the same transaction. Commit both atomically.</item>
///   <item>Build accepted response with attendee + tier-breakdown details (F13: TicketCategory-aware).</item>
/// </list>
/// </summary>
public class ScanTicketCommandHandler : ICommandHandler<ScanTicketCommand, ScanTicketResult>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketScanLogRepository _scanLogRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ITicketSignatureService _signatureService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScanTicketCommandHandler> _logger;

    public ScanTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketScanLogRepository scanLogRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ITicketSignatureService signatureService,
        IUnitOfWork unitOfWork,
        ILogger<ScanTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _scanLogRepository = scanLogRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _signatureService = signatureService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ScanTicketResult>> Handle(ScanTicketCommand command, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ScanTicket"))
        using (LogContext.PushProperty("EntityType", "Ticket"))
        using (LogContext.PushProperty("EventId", command.EventId))
        using (LogContext.PushProperty("ScannerUserId", command.ScannerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "ScanTicket START: EventId={EventId}, ScannerUserId={ScannerUserId}, IsQrScan={IsQrScan}, IsManualEntry={IsManualEntry}",
                command.EventId, command.ScannerUserId, command.IsQrScan, command.IsManualEntry);

            try
            {
                // 1. Validate command shape
                if (command.IsQrScan == command.IsManualEntry)
                {
                    // both true or both false — neither valid
                    _logger.LogWarning("ScanTicket malformed request — exactly one of QrPayload/TicketCode required");
                    return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                        ReasonCode.MalformedRequest,
                        "Internal error: scanner sent both a QR payload and a ticket code, or neither."));
                }

                // 2. Verify event exists — 404 path
                var @event = await _eventRepository.GetByIdAsync(command.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("ScanTicket: event not found EventId={EventId}", command.EventId);
                    return Result<ScanTicketResult>.NotFound($"Event {command.EventId} not found");
                }

                // 2a. Authorization — only the event organizer (primary or co-organizer) can
                // scan tickets. Reuses Phase 6A.133 organizer-link pattern via Event.IsOrganizer.
                // Plan-agent F8: this also gates the non-primary co-organizer case which must
                // be exercised in operator UAT.
                if (!@event.IsOrganizer(command.ScannerUserId))
                {
                    _logger.LogWarning(
                        "ScanTicket: caller {ScannerUserId} is not an organizer of event {EventId} — 403",
                        command.ScannerUserId, command.EventId);
                    return Result<ScanTicketResult>.Forbidden("Only event organizers can scan tickets for this event.");
                }

                // 3. Resolve ticketCode + entryMethod, verifying signature if v1 QR
                string? resolvedTicketCode;
                string entryMethod;
                bool usedPreviousKey = false;

                if (command.IsManualEntry)
                {
                    resolvedTicketCode = command.TicketCode!.Trim();
                    entryMethod = TicketScanLog.EntryMethodManual;
                    _logger.LogInformation("ScanTicket: manual-entry path TicketCode={TicketCode}", resolvedTicketCode);
                }
                else
                {
                    var parsed = TicketSignedPayload.TryParse(command.QrPayload);
                    if (parsed is null)
                    {
                        _logger.LogWarning("ScanTicket: malformed QR payload — could not parse");
                        await TryWriteRejectionAuditAsync(command, ticketId: null, ticketCode: null,
                            ReasonCode.MalformedPayload, TicketScanLog.EntryMethodQr, usedPreviousKey: false, cancellationToken);
                        return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                            ReasonCode.MalformedPayload, "Invalid QR code format."));
                    }

                    if (parsed.Version == TicketSignedPayload.PayloadVersion.V1)
                    {
                        var sigBytes = parsed.Signature.ToArray();
                        var verifyResult = _signatureService.Verify(parsed.BodyToSign, sigBytes);
                        if (!verifyResult.IsValid)
                        {
                            _logger.LogWarning(
                                "ScanTicket: HMAC signature invalid — possible forgery attempt. EventId={EventId}, ScannerUserId={ScannerUserId}",
                                command.EventId, command.ScannerUserId);
                            await TryWriteRejectionAuditAsync(command, ticketId: null, ticketCode: null,
                                ReasonCode.InvalidSignature, TicketScanLog.EntryMethodQr, usedPreviousKey: false, cancellationToken);
                            return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                                ReasonCode.InvalidSignature, "This QR code has an invalid signature."));
                        }
                        usedPreviousKey = verifyResult.UsedPreviousKey;
                        entryMethod = TicketScanLog.EntryMethodQr;
                    }
                    else
                    {
                        // Legacy unsigned — accept the format but flag as reduced-trust.
                        entryMethod = TicketScanLog.EntryMethodQrLegacy;
                        _logger.LogInformation(
                            "ScanTicket: legacy unsigned QR — accepted with qr_legacy entry method flag");
                    }
                    resolvedTicketCode = parsed.TicketCode;
                }

                // 4. Look up ticket
                var ticket = await _ticketRepository.GetByTicketCodeAsync(resolvedTicketCode, cancellationToken);
                if (ticket is null)
                {
                    _logger.LogWarning("ScanTicket: ticket not found TicketCode={TicketCode}", resolvedTicketCode);
                    await TryWriteRejectionAuditAsync(command, ticketId: null, ticketCode: resolvedTicketCode,
                        ReasonCode.TicketNotFound, entryMethod, usedPreviousKey, cancellationToken);
                    return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                        ReasonCode.TicketNotFound, $"Ticket {resolvedTicketCode} not found."));
                }

                // 5. Event match
                if (ticket.EventId != command.EventId)
                {
                    var wrongEvent = await _eventRepository.GetByIdAsync(ticket.EventId, cancellationToken);
                    var wrongEventTitle = wrongEvent?.Title?.Value;
                    _logger.LogWarning(
                        "ScanTicket: wrong-event reject TicketCode={TicketCode}, ScannedAtEvent={ScannedAtEventId}, TicketBelongsTo={TicketEventId}",
                        resolvedTicketCode, command.EventId, ticket.EventId);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.WrongEvent, entryMethod, usedPreviousKey, cancellationToken);
                    var rejected = ScanTicketResult.RejectedFor(
                        ReasonCode.WrongEvent,
                        wrongEventTitle is null
                            ? "This ticket is for a different event."
                            : $"This ticket is for a different event: {wrongEventTitle}.");
                    return Result<ScanTicketResult>.Success(rejected with { WrongEventTitle = wrongEventTitle });
                }

                // 6. Synchronous state checks
                if (!ticket.IsValid)
                {
                    _logger.LogInformation("ScanTicket: invalidated ticket TicketCode={TicketCode}", resolvedTicketCode);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.Invalidated, entryMethod, usedPreviousKey, cancellationToken);
                    return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                        ReasonCode.Invalidated, "This ticket has been invalidated (likely refunded or cancelled)."));
                }
                if (ticket.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogInformation("ScanTicket: expired ticket TicketCode={TicketCode}, ExpiresAt={ExpiresAt:o}",
                        resolvedTicketCode, ticket.ExpiresAt);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.Expired, entryMethod, usedPreviousKey, cancellationToken);
                    return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                        ReasonCode.Expired, "This ticket has expired."));
                }
                if (ticket.ValidatedAt.HasValue)
                {
                    _logger.LogInformation(
                        "ScanTicket: already-scanned ticket TicketCode={TicketCode}, ValidatedAt={ValidatedAt:o}",
                        resolvedTicketCode, ticket.ValidatedAt.Value);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.AlreadyScanned, entryMethod, usedPreviousKey, cancellationToken);
                    return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                        ReasonCode.AlreadyScanned,
                        $"Already scanned at {ticket.ValidatedAt.Value:HH:mm} UTC."));
                }

                // 7. Mark-scanned + audit.
                //
                // Phase 6A.141 UAT hotfix (Issue 1): the original design wrapped both steps
                // in an explicit IUnitOfWork.BeginTransactionAsync/CommitTransactionAsync per
                // F2. Operator UAT surfaced that the project's AppDbContext.CommitAsync
                // (Phase 6A.74 RCA) interacts with EF Core 8 ExecuteUpdateAsync inside an
                // open IDbContextTransaction in a way that throws InvalidOperationException
                // → GlobalExceptionMiddleware returned 400 "The requested operation is
                // invalid." for every real ticket scan.
                //
                // Fix per system-architect: run the atomic UPDATE standalone (race-safety
                // is still guaranteed at the row level by the WHERE clause inside
                // TryMarkScannedAsync — `WHERE Id=@id AND ValidatedAt IS NULL`), then write
                // the audit row via a separate CommitAsync. Trade-off: a forensic gap on
                // partial DB failure (UPDATE succeeds but audit insert fails → ticket
                // scanned with no audit row). Acceptable because:
                //   • Door correctly opens (UPDATE succeeded, attendee gets in)
                //   • Rejection-audit path already accepts the same gap (see
                //     TryWriteRejectionAuditAsync which swallows audit failures)
                //   • Reconciliation queries (Tickets with ValidatedAt and no accepted
                //     scan log row) can detect mismatches for post-hoc cleanup
                //   • The clean alternative (xmin concurrency token on Ticket + migration)
                //     is over-engineered for the failure mode; deferred to a future hardening
                //     phase if real-world audit gaps appear in logs.
                var now = DateTime.UtcNow;
                int rowsAffected;
                try
                {
                    rowsAffected = await _ticketRepository.TryMarkScannedAsync(ticket.Id, now, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "ScanTicket: TryMarkScannedAsync threw. TicketCode={TicketCode}",
                        resolvedTicketCode);
                    throw;
                }

                if (rowsAffected == 0)
                {
                    // Race-loser: another scanner marked this ticket scanned between our
                    // read and our atomic UPDATE.
                    _logger.LogWarning(
                        "ScanTicket: race-lost — TryMarkScannedAsync returned 0 rows. TicketCode={TicketCode}",
                        resolvedTicketCode);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.AlreadyScanned, entryMethod, usedPreviousKey, cancellationToken);
                    return Result<ScanTicketResult>.Success(ScanTicketResult.RejectedFor(
                        ReasonCode.AlreadyScanned, "Already scanned (by another scanner just now)."));
                }

                // Winner: queue accepted audit row in a separate CommitAsync.
                // If the audit-insert fails, we log + continue (door already open via UPDATE).
                try
                {
                    var acceptedAudit = TicketScanLog.Accepted(
                        ticket.Id, command.EventId, ticket.TicketCode,
                        command.ScannerUserId, command.ScannerName,
                        entryMethod, usedPreviousKey,
                        command.ClientIp, command.UserAgent);
                    await _scanLogRepository.AddAsync(acceptedAudit, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                }
                catch (Exception auditEx)
                {
                    // Forensic-gap: ticket is marked scanned but no accepted-audit row.
                    // Don't fail the scan response — the door is already open for the
                    // attendee per the successful UPDATE. Surface in logs for later
                    // reconciliation.
                    _logger.LogError(auditEx,
                        "ScanTicket: accepted-audit write failed AFTER successful mark-scanned. TicketCode={TicketCode}. Reconciliation query can detect this gap.",
                        resolvedTicketCode);
                }

                // 8. Build accepted response
                var registration = await _registrationRepository.GetByIdAsync(ticket.RegistrationId, cancellationToken);
                string? attendeeName = registration?.Attendees.FirstOrDefault()?.Name;
                string? tier = registration?.Attendees.FirstOrDefault()?.TicketTierName;
                int? attendeeCount = registration?.GetAttendeeCount();
                IReadOnlyList<TierBreakdownEntry>? breakdown = null;
                if (registration is not null && registration.Attendees.Any())
                {
                    // F13: TicketCategory-aware breakdown. Today TicketCategory==Standard for every
                    // ticket (CreateTiered is dead code per Phase 6A.141 architect finding), so we
                    // render the FULL registration's attendees. If/when CreateTiered is activated
                    // in a future phase, Ticket.TicketCategory == Individual will mean "render only
                    // the attendee at Ticket.AttendeeIndex" — that branch is left for that phase.
                    breakdown = registration.Attendees
                        .Where(a => !string.IsNullOrWhiteSpace(a.TicketTierName))
                        .GroupBy(a => a.TicketTierName!)
                        .Select(g => new TierBreakdownEntry(g.Key, g.Count()))
                        .ToList();
                }

                sw.Stop();
                _logger.LogInformation(
                    "ScanTicket ACCEPTED: TicketCode={TicketCode}, AttendeeName={AttendeeName}, Tier={Tier}, UsedPreviousKey={UsedPreviousKey}, Duration={ElapsedMs}ms",
                    ticket.TicketCode, attendeeName, tier, usedPreviousKey, sw.ElapsedMilliseconds);

                return Result<ScanTicketResult>.Success(ScanTicketResult.AcceptedFor(
                    ticketCode: ticket.TicketCode,
                    attendeeName: attendeeName,
                    tier: tier,
                    attendeeCount: attendeeCount,
                    tierBreakdown: breakdown,
                    scannedAt: now,
                    scannedBy: command.ScannerName,
                    usedPreviousKey: usedPreviousKey));
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "ScanTicket FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    command.EventId, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Writes a rejected-scan audit row OUTSIDE any open transaction. Uses its own
    /// CommitAsync so the audit insert is independent of the caller's flow. If the audit
    /// write fails (DB hiccup), the rejection is still returned to the caller — we log
    /// but don't surface a 500 to the scanner. Forensic gap is better than blocked door.
    /// </summary>
    private async Task TryWriteRejectionAuditAsync(
        ScanTicketCommand command,
        Guid? ticketId,
        string? ticketCode,
        string reason,
        string entryMethod,
        bool usedPreviousKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var audit = TicketScanLog.Rejected(
                ticketId: ticketId,
                eventId: command.EventId,
                ticketCode: ticketCode,
                scannerUserId: command.ScannerUserId,
                scannerName: command.ScannerName,
                rejectionReason: reason,
                entryMethod: entryMethod,
                verifiedWithPreviousKey: usedPreviousKey,
                clientIp: command.ClientIp,
                userAgent: command.UserAgent);
            await _scanLogRepository.AddAsync(audit, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to write rejection audit row (Reason={Reason}, TicketCode={TicketCode}) — forensic gap. Scan rejection still returned to caller.",
                reason, ticketCode);
            // Swallow — don't fail the user-facing rejection on an audit gap.
        }
    }
}
