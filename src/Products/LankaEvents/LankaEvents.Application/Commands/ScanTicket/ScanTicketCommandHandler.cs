using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket;

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
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly ITicketSignatureService _signatureService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScanTicketCommandHandler> _logger;

    public ScanTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketScanLogRepository scanLogRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        ITicketSignatureService signatureService,
        IUnitOfWork unitOfWork,
        ILogger<ScanTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _scanLogRepository = scanLogRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
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
                    var wrongEventRejection = await BuildTicketResolvedRejectionAsync(
                        ticket,
                        wrongEvent, // pass the ORIGINAL event so its TicketTiers resolve per-attendee prices
                        ReasonCode.WrongEvent,
                        wrongEventTitle is null
                            ? "This ticket is for a different event."
                            : $"This ticket is for a different event: {wrongEventTitle}.",
                        wrongEventTitle,
                        usedPreviousKey,
                        cancellationToken);
                    return Result<ScanTicketResult>.Success(wrongEventRejection);
                }

                // 6. Synchronous state checks
                if (!ticket.IsValid)
                {
                    _logger.LogInformation("ScanTicket: invalidated ticket TicketCode={TicketCode}", resolvedTicketCode);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.Invalidated, entryMethod, usedPreviousKey, cancellationToken);
                    var invalidatedRejection = await BuildTicketResolvedRejectionAsync(
                        ticket,
                        @event,
                        ReasonCode.Invalidated,
                        "This ticket has been invalidated (likely refunded or cancelled).",
                        wrongEventTitle: null,
                        usedPreviousKey,
                        cancellationToken);
                    return Result<ScanTicketResult>.Success(invalidatedRejection);
                }
                if (ticket.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogInformation("ScanTicket: expired ticket TicketCode={TicketCode}, ExpiresAt={ExpiresAt:o}",
                        resolvedTicketCode, ticket.ExpiresAt);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.Expired, entryMethod, usedPreviousKey, cancellationToken);
                    var expiredRejection = await BuildTicketResolvedRejectionAsync(
                        ticket,
                        @event,
                        ReasonCode.Expired,
                        "This ticket has expired.",
                        wrongEventTitle: null,
                        usedPreviousKey,
                        cancellationToken);
                    return Result<ScanTicketResult>.Success(expiredRejection);
                }
                if (ticket.ValidatedAt.HasValue)
                {
                    _logger.LogInformation(
                        "ScanTicket: already-scanned ticket TicketCode={TicketCode}, ValidatedAt={ValidatedAt:o}",
                        resolvedTicketCode, ticket.ValidatedAt.Value);
                    await TryWriteRejectionAuditAsync(command, ticket.Id, resolvedTicketCode,
                        ReasonCode.AlreadyScanned, entryMethod, usedPreviousKey, cancellationToken);
                    var alreadyScannedRejection = await BuildTicketResolvedRejectionAsync(
                        ticket,
                        @event,
                        ReasonCode.AlreadyScanned,
                        $"Already scanned at {ticket.ValidatedAt.Value:HH:mm} UTC.",
                        wrongEventTitle: null,
                        usedPreviousKey,
                        cancellationToken);
                    return Result<ScanTicketResult>.Success(alreadyScannedRejection);
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
                    var raceLostRejection = await BuildTicketResolvedRejectionAsync(
                        ticket,
                        @event,
                        ReasonCode.AlreadyScanned,
                        "Already scanned (by another scanner just now).",
                        wrongEventTitle: null,
                        usedPreviousKey,
                        cancellationToken);
                    return Result<ScanTicketResult>.Success(raceLostRejection);
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

                var attendees = BuildAttendeeDetails(registration, @event);
                var addOns = await BuildAddOnsAsync(registration, command.EventId, cancellationToken);

                return Result<ScanTicketResult>.Success(ScanTicketResult.AcceptedFor(
                    ticketCode: ticket.TicketCode,
                    attendeeName: attendeeName,
                    tier: tier,
                    attendeeCount: attendeeCount,
                    tierBreakdown: breakdown,
                    scannedAt: now,
                    scannedBy: command.ScannerName,
                    usedPreviousKey: usedPreviousKey,
                    attendees: attendees,
                    addOns: addOns));
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
    /// UAT R3 — projects every attendee on a registration into the wire-shape used by
    /// the scanner UI's per-attendee list. Pure projection (no DB calls); both the
    /// registration's Attendees collection and the event's TicketTiers are eager-loaded
    /// by their respective GetByIdAsync repos, so this runs entirely in-memory.
    ///
    /// Returns <c>null</c> for head-count-mode registrations or any case where the
    /// Attendees collection is empty — the UI then falls back to the legacy aggregate
    /// fields (AttendeeName / Tier / AttendeeCount / TierBreakdown).
    ///
    /// Per-attendee price is computed via <c>TicketTier.CalculatePriceForAttendee</c>
    /// matched on the attendee's <c>TicketTierId</c>. When the tier isn't found on the
    /// event (data drift after a tier deletion), price fields stay null but the
    /// attendee's name + age category + tier name (denormalized) are still surfaced.
    /// </summary>
    private static IReadOnlyList<AttendeeDetail>? BuildAttendeeDetails(
        LankaConnect.Products.LankaEvents.Domain.Registration? registration,
        LankaConnect.Products.LankaEvents.Domain.Event? @event)
    {
        if (registration is null || registration.Attendees is null || !registration.Attendees.Any())
            return null;

        // Pre-index tiers by Id for an O(1) per-attendee lookup; the list is tiny (≤~5)
        // so a Dictionary is overkill but readable. Null @event (e.g. wrong-event branch
        // where the target event was deleted) means we render attendees without prices —
        // names + ages + tier-name-denormalized still surface.
        var tiersById = @event?.TicketTiers?
            .Where(t => t.Id != Guid.Empty)
            .ToDictionary(t => t.Id);

        var details = new List<AttendeeDetail>(registration.Attendees.Count);
        foreach (var a in registration.Attendees)
        {
            decimal? priceAmount = null;
            string? priceCurrency = null;
            if (a.TicketTierId.HasValue && tiersById is not null
                && tiersById.TryGetValue(a.TicketTierId.Value, out var tier))
            {
                var price = tier.CalculatePriceForAttendee(a.AgeCategory);
                priceAmount = price.Amount;
                priceCurrency = price.Currency.ToString();
            }

            details.Add(new AttendeeDetail(
                Name: a.Name,
                AgeCategory: a.AgeCategory.ToString(),
                Gender: a.Gender?.ToString(),
                TicketTierName: a.TicketTierName,
                PriceAmount: priceAmount,
                PriceCurrency: priceCurrency,
                SeatLabel: a.SeatLabel));
        }
        return details;
    }

    /// <summary>
    /// UAT R4 — project the add-on purchases bundled with this registration so the
    /// scanner UI can surface what extras the attendee paid for (e.g. dinner add-on,
    /// merch, parking pass).
    ///
    /// Filter: only Completed add-on purchases whose RegistrationId matches THIS
    /// registration. Standalone add-ons (purchased separately from the event page)
    /// and pending/failed/abandoned/refunded purchases are excluded — at the gate,
    /// the operator only cares about confirmed bundled items for the ticket in hand.
    ///
    /// Two DB round-trips on the hot scan path:
    ///   1. <see cref="IAddOnPurchaseRepository.GetByUserIdAndEventIdAsync"/> for known users,
    ///      or <see cref="IAddOnPurchaseRepository.GetAllByCheckoutSessionIdAsync"/> for anonymous.
    ///   2. <see cref="IAddOnDefinitionRepository.GetByEventIdAsync"/> for the display names.
    /// Both bounded — typically ≤3 purchases and ≤5 definitions per event.
    ///
    /// Returns null when there are no matching bundled add-ons; the UI then renders
    /// nothing for the add-ons section (no empty card).
    /// </summary>
    private async Task<IReadOnlyList<AddOnSummary>?> BuildAddOnsAsync(
        LankaConnect.Products.LankaEvents.Domain.Registration? registration,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (registration is null) return null;

        try
        {
            IReadOnlyList<AddOnPurchase>? purchases = null;
            if (registration.UserId.HasValue && registration.UserId.Value != Guid.Empty)
            {
                purchases = await _addOnPurchaseRepository.GetByUserIdAndEventIdAsync(
                    registration.UserId.Value, eventId, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(registration.StripeCheckoutSessionId))
            {
                purchases = await _addOnPurchaseRepository.GetAllByCheckoutSessionIdAsync(
                    registration.StripeCheckoutSessionId, cancellationToken);
            }

            // Gate-staff filter: Completed AND bundled with THIS registration.
            var bundled = purchases?
                .Where(p => p.Status == AddOnPurchaseStatus.Completed
                         && p.RegistrationId == registration.Id)
                .ToList();
            if (bundled is null || bundled.Count == 0) return null;

            // One query for names; index by Id.
            var definitions = await _addOnDefinitionRepository
                .GetByEventIdAsync(eventId, cancellationToken);
            var namesById = definitions?.ToDictionary(d => d.Id, d => d.Name);

            return bundled.Select(p => new AddOnSummary(
                Name: namesById is not null && namesById.TryGetValue(p.AddOnDefinitionId, out var name)
                    ? name
                    : "Add-on",
                Quantity: p.Quantity,
                UnitPrice: p.UnitPrice.Amount,
                TotalAmount: p.TotalAmount.Amount,
                Currency: p.UnitPrice.Currency.ToString())).ToList();
        }
        catch (Exception ex)
        {
            // Don't fail the scan if add-on enrichment hits a transient DB error — the
            // door still opens for the attendee. Log forensic detail; return null so the
            // UI shows the scan panel without the add-ons section.
            _logger.LogWarning(ex,
                "ScanTicket: failed to enrich add-ons for RegistrationId={RegistrationId}. " +
                "Scan response will omit the add-ons section.",
                registration.Id);
            return null;
        }
    }

    /// <summary>
    /// UAT R2 Issue A — assemble an enriched rejection DTO for the 4 ticket-resolved
    /// rejection reasons (already_scanned, expired, invalidated, wrong_event). The
    /// scanner UI renders attendee details on these panels (operator wants to see who
    /// the ticket belongs to regardless of accept/reject), and renders an amber
    /// "Already Scanned (Nx)" panel with scan history when reason==already_scanned.
    ///
    /// UAT R3 — also projects the full per-attendee list via <see cref="BuildAttendeeDetails"/>.
    /// The wrong_event branch deliberately passes the ORIGINAL event (the one the ticket
    /// belongs to) so attendee tiers resolve correctly even though the scanner was
    /// pointed at a different event.
    ///
    /// Skipped for invalid_signature / malformed_payload / ticket_not_found /
    /// malformed_request because there is no resolved ticket to enrich from.
    /// </summary>
    private async Task<ScanTicketResult> BuildTicketResolvedRejectionAsync(
        LankaConnect.Products.LankaEvents.Domain.Entities.Ticket ticket,
        LankaConnect.Products.LankaEvents.Domain.Event? ticketEvent,
        string reason,
        string reasonMessage,
        string? wrongEventTitle,
        bool usedPreviousKey,
        CancellationToken cancellationToken)
    {
        var registration = await _registrationRepository.GetByIdAsync(ticket.RegistrationId, cancellationToken);
        var attendeeName = registration?.Attendees.FirstOrDefault()?.Name;
        var tier = registration?.Attendees.FirstOrDefault()?.TicketTierName;
        var attendeeCount = registration?.GetAttendeeCount();
        IReadOnlyList<TierBreakdownEntry>? breakdown = null;
        if (registration is not null && registration.Attendees.Any())
        {
            breakdown = registration.Attendees
                .Where(a => !string.IsNullOrWhiteSpace(a.TicketTierName))
                .GroupBy(a => a.TicketTierName!)
                .Select(g => new TierBreakdownEntry(g.Key, g.Count()))
                .ToList();
        }

        var attendees = BuildAttendeeDetails(registration, ticketEvent);
        // UAT R4: enrich rejection with bundled add-ons for the ORIGINAL event (wrong_event
        // branch passes the ticket's home event; same-event branches pass @event).
        var addOns = await BuildAddOnsAsync(registration, ticket.EventId, cancellationToken);

        var (acceptedCount, lastScannerName, lastAcceptedAt) =
            await _scanLogRepository.GetAcceptedSummaryForTicketAsync(ticket.Id, cancellationToken);

        return new ScanTicketResult
        {
            Result = "rejected",
            Reason = reason,
            ReasonMessage = reasonMessage,
            TicketCode = ticket.TicketCode,
            AttendeeName = attendeeName,
            Tier = tier,
            AttendeeCount = attendeeCount,
            TierBreakdown = breakdown,
            Attendees = attendees,
            AddOns = addOns,
            // For already_scanned, prefer the audit-log timestamp (denormalized scanner name
            // pairs with it). Fall back to Ticket.ValidatedAt for the rare case where the
            // accepted audit row was lost to the forensic gap documented in the main flow.
            ScannedAt = lastAcceptedAt ?? ticket.ValidatedAt,
            ScannedBy = null, // ScannedBy is for the CURRENT operator on accept; rejections don't populate it
            UsedPreviousKey = usedPreviousKey,
            WrongEventTitle = wrongEventTitle,
            PreviousScanCount = acceptedCount > 0 ? acceptedCount : null,
            PreviousScannedBy = lastScannerName,
        };
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
