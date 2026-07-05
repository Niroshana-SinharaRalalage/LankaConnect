using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Phase 6A.148: An attendee or organizer-initiated request to refund some/all of a
/// paid registration's charges (ticket, add-ons, collections, sponsorships).
///
/// Lives inside the <c>Registration</c> aggregate — invariants (single active request,
/// scan guard) are enforced in <c>Registration.CreateRefundRequest</c>. Approval moves
/// the request to <see cref="RefundRequestStatus.Approved"/>; the application layer's
/// <c>RefundExecutionService</c> then dispatches Stripe outside the approve transaction
/// (architect F10) and calls <see cref="BeginProcessing"/> to advance state.
///
/// State machine: Pending → Approved → Processing → Completed | Rejected | Withdrawn.
/// Backward transitions from Approved onward are forbidden — money is moving.
/// </summary>
public class RefundRequest : LegacyBaseEntity
{
    public Guid RegistrationId { get; private set; }

    /// <summary>
    /// For attendee path: the attendee's UserId.
    /// For organizer path: the organizer's UserId (we don't track "on behalf of whom" separately —
    /// the parent Registration's UserId identifies the attendee).
    /// </summary>
    public Guid RequestedByUserId { get; private set; }

    public bool IsOrganizerInitiated { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public RefundRequestStatus Status { get; private set; }
    public string? RequesterReason { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? OrganizerNotes { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool ScanGuardOverridden { get; private set; }

    private readonly List<RefundRequestLineItem> _lineItems = new();
    public IReadOnlyList<RefundRequestLineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>
    /// True when the request is in any non-terminal state — used by the aggregate's
    /// "single active request" guard (architect F1: covers Pending/Approved/Processing).
    /// </summary>
    public bool IsActive =>
        Status == RefundRequestStatus.Pending ||
        Status == RefundRequestStatus.Approved ||
        Status == RefundRequestStatus.Processing;

    // EF Core
    private RefundRequest() { }

    private RefundRequest(
        Guid registrationId,
        Guid requestedByUserId,
        bool isOrganizerInitiated,
        string? requesterReason,
        string? organizerNotes,
        bool scanGuardOverridden,
        RefundRequestStatus initialStatus)
    {
        RegistrationId = registrationId;
        RequestedByUserId = requestedByUserId;
        IsOrganizerInitiated = isOrganizerInitiated;
        RequesterReason = requesterReason;
        OrganizerNotes = organizerNotes;
        ScanGuardOverridden = scanGuardOverridden;
        RequestedAt = DateTime.UtcNow;
        Status = initialStatus;

        if (isOrganizerInitiated)
        {
            ReviewedByUserId = requestedByUserId;
            ReviewedAt = RequestedAt;
        }
    }

    /// <summary>
    /// Attendee path. Line items start in <see cref="RefundLineItemStatus.Requested"/>;
    /// an organizer must call <see cref="Approve"/> later to set per-line amounts.
    /// </summary>
    public static Result<RefundRequest> CreatePending(
        Guid registrationId,
        Guid attendeeUserId,
        string? requesterReason,
        IReadOnlyList<RefundRequestLineItemInput> lineItems)
    {
        if (lineItems is null || lineItems.Count == 0)
            return Result<RefundRequest>.Failure("Refund request must include at least one line item");

        var req = new RefundRequest(
            registrationId, attendeeUserId,
            isOrganizerInitiated: false,
            requesterReason: requesterReason,
            organizerNotes: null,
            scanGuardOverridden: false,
            initialStatus: RefundRequestStatus.Pending);

        foreach (var input in lineItems)
        {
            var lineResult = RefundRequestLineItem.Create(
                req.Id, input.Type, input.ReferenceId, input.RequestedAmount);
            if (lineResult.IsFailure)
                return Result<RefundRequest>.Failure(lineResult.Errors);
            req._lineItems.Add(lineResult.Value);
        }

        // Domain event — organizer notified, attendee confirmed.
        req.RaiseDomainEvent(new RefundRequestCreatedEvent(
            EventId: Guid.Empty, // populated by Registration aggregate which knows the EventId
            RegistrationId: registrationId,
            RefundRequestId: req.Id,
            RequestedByUserId: attendeeUserId,
            RequesterReason: requesterReason,
            RequestedAt: req.RequestedAt));

        return Result<RefundRequest>.Success(req);
    }

    /// <summary>
    /// Organizer path. Skips Pending: request is created directly in
    /// <see cref="RefundRequestStatus.Approved"/> with all line items pre-approved at
    /// their requested amount. Caller specifies <paramref name="scanGuardOverridden"/>
    /// only when bypassing the ticket-scanned check; that path requires non-empty
    /// <paramref name="organizerNotes"/> (architect F7).
    /// </summary>
    public static Result<RefundRequest> CreateOrganizerInitiated(
        Guid registrationId,
        Guid organizerUserId,
        string? organizerNotes,
        bool scanGuardOverridden,
        IReadOnlyList<RefundRequestLineItemInput> lineItems)
    {
        if (lineItems is null || lineItems.Count == 0)
            return Result<RefundRequest>.Failure("Refund request must include at least one line item");

        if (scanGuardOverridden && string.IsNullOrWhiteSpace(organizerNotes))
            return Result<RefundRequest>.Failure(
                "OrganizerNotes are required when overriding the ticket-scan guard.");

        var req = new RefundRequest(
            registrationId, organizerUserId,
            isOrganizerInitiated: true,
            requesterReason: null,
            organizerNotes: organizerNotes,
            scanGuardOverridden: scanGuardOverridden,
            initialStatus: RefundRequestStatus.Approved);

        foreach (var input in lineItems)
        {
            var lineResult = RefundRequestLineItem.Create(
                req.Id, input.Type, input.ReferenceId, input.RequestedAmount);
            if (lineResult.IsFailure)
                return Result<RefundRequest>.Failure(lineResult.Errors);

            var line = lineResult.Value;
            // Auto-approve at full requested amount.
            var approveResult = line.Approve(input.RequestedAmount);
            if (approveResult.IsFailure)
                return Result<RefundRequest>.Failure(approveResult.Errors);

            req._lineItems.Add(line);
        }

        req.RaiseDomainEvent(new OrganizerInitiatedRefundCreatedEvent(
            EventId: Guid.Empty,
            RegistrationId: registrationId,
            RefundRequestId: req.Id,
            OrganizerUserId: organizerUserId,
            OrganizerNotes: organizerNotes,
            ScanGuardOverridden: scanGuardOverridden,
            CreatedAt: req.RequestedAt));

        return Result<RefundRequest>.Success(req);
    }

    /// <summary>
    /// Organizer approves the request. Per-line amounts: zero rejects the line; any line
    /// with a positive amount is approved. Lines not present in the dictionary are
    /// treated as rejected (organizer explicitly opted out). At least one line must end
    /// up Approved with a non-zero amount (architect F2 — all-zero approve returns
    /// failure; organizer must use Reject instead).
    /// </summary>
    public Result Approve(
        Guid organizerUserId,
        string? organizerNotes,
        IReadOnlyDictionary<Guid, Money> perLineApprovedAmounts)
    {
        if (Status != RefundRequestStatus.Pending)
            return Result.Failure(
                $"Approve only Pending refund requests; current status is {Status}.");

        if (perLineApprovedAmounts is null)
            return Result.Failure("perLineApprovedAmounts is required");

        // Architect F2: pre-validate BEFORE mutating any line — at least one line must
        // have a non-zero approved amount. Otherwise this is a reject masquerading as an
        // approve; force the organizer to use the /reject endpoint where they must supply
        // a rejection reason for the attendee email.
        if (!perLineApprovedAmounts.Values.Any(m => m is not null && m.Amount > 0))
            return Result.Failure(
                "At least one line item must be approved with a non-zero amount. " +
                "To decline the entire request, use Reject instead.");

        // Pre-validate per-line amounts against requested amounts before mutating any line,
        // so a partial-failure leaves the aggregate clean.
        var validationErrors = new List<string>();
        foreach (var line in _lineItems)
        {
            if (!perLineApprovedAmounts.TryGetValue(line.Id, out var amount))
                continue; // missing entry = will be Rejected, which always succeeds

            if (amount is null)
            {
                validationErrors.Add($"ApprovedAmount for line {line.Id} cannot be null");
                continue;
            }

            if (amount.Currency != line.RequestedAmount.Currency)
                validationErrors.Add(
                    $"ApprovedAmount currency must match RequestedAmount currency for line {line.Id} " +
                    $"({line.RequestedAmount.Currency} expected, {amount.Currency} provided).");

            if (amount.Amount < 0)
                validationErrors.Add($"ApprovedAmount for line {line.Id} cannot be negative");

            if (amount.Amount > line.RequestedAmount.Amount)
                validationErrors.Add(
                    $"ApprovedAmount ({amount.Amount}) cannot exceed RequestedAmount " +
                    $"({line.RequestedAmount.Amount}) for line item {line.Id}.");
        }

        if (validationErrors.Count > 0)
            return Result.Failure(validationErrors);

        // Apply per-line decisions, treating absence as rejection. All inputs already validated.
        foreach (var line in _lineItems)
        {
            var lineResult = perLineApprovedAmounts.TryGetValue(line.Id, out var amount)
                ? line.Approve(amount!)
                : line.Reject();

            // Pre-validation should have caught everything; treat any remaining error as bug.
            if (lineResult.IsFailure)
                return Result.Failure(lineResult.Errors);
        }

        Status = RefundRequestStatus.Approved;
        ReviewedByUserId = organizerUserId;
        ReviewedAt = DateTime.UtcNow;
        OrganizerNotes = organizerNotes;

        RaiseDomainEvent(new RefundRequestApprovedEvent(
            EventId: Guid.Empty,
            RegistrationId: RegistrationId,
            RefundRequestId: Id,
            OrganizerUserId: organizerUserId,
            OrganizerNotes: organizerNotes,
            ApprovedAt: ReviewedAt.Value));

        return Result.Success();
    }

    public Result Reject(Guid organizerUserId, string rejectionReason)
    {
        if (Status != RefundRequestStatus.Pending)
            return Result.Failure(
                $"Reject only Pending refund requests; current status is {Status}.");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Failure("RejectionReason is required");

        // Per-line: mark all as rejected for clean accounting.
        foreach (var line in _lineItems.Where(l => l.Status == RefundLineItemStatus.Requested))
            line.Reject();

        Status = RefundRequestStatus.Rejected;
        ReviewedByUserId = organizerUserId;
        ReviewedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason;

        RaiseDomainEvent(new RefundRequestRejectedEvent(
            EventId: Guid.Empty,
            RegistrationId: RegistrationId,
            RefundRequestId: Id,
            OrganizerUserId: organizerUserId,
            RejectionReason: rejectionReason,
            RejectedAt: ReviewedAt.Value));

        return Result.Success();
    }

    public Result Withdraw(Guid byUserId)
    {
        if (Status != RefundRequestStatus.Pending)
            return Result.Failure(
                $"Withdraw only Pending refund requests; current status is {Status}.");

        if (byUserId != RequestedByUserId)
            return Result.Failure("Refund requests can be withdrawn only the requester themselves.");

        Status = RefundRequestStatus.Withdrawn;

        RaiseDomainEvent(new RefundRequestWithdrawnEvent(
            EventId: Guid.Empty,
            RegistrationId: RegistrationId,
            RefundRequestId: Id,
            WithdrawnByUserId: byUserId,
            WithdrawnAt: DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Internal — invoked by <c>RefundExecutionService</c> as soon as the first Stripe
    /// refund dispatch succeeds for any approved line. Idempotent (callable from
    /// Processing).
    /// </summary>
    public Result BeginProcessing()
    {
        if (Status == RefundRequestStatus.Processing)
            return Result.Success();

        if (Status != RefundRequestStatus.Approved)
            return Result.Failure(
                $"BeginProcessing requires Approved status; current status is {Status}.");

        Status = RefundRequestStatus.Processing;
        return Result.Success();
    }

    /// <summary>
    /// Internal — invoked by webhook handlers after a line item transitions to terminal
    /// state. If all approved/rejected lines have reached a terminal state (Refunded,
    /// Failed, or Rejected — the latter never gets a Stripe call), the request rolls to
    /// <see cref="RefundRequestStatus.Completed"/>.
    /// </summary>
    public Result MarkCompletedIfAllSettled()
    {
        if (Status == RefundRequestStatus.Completed)
            return Result.Success();

        if (Status != RefundRequestStatus.Processing)
            return Result.Failure(
                $"MarkCompletedIfAllSettled requires Processing status; current status is {Status}.");

        // A line is "settled" when it's Refunded, Failed (terminal Stripe outcomes), or
        // Rejected (never went to Stripe — organizer set it to 0).
        var allSettled = _lineItems.All(l =>
            l.Status == RefundLineItemStatus.Refunded ||
            l.Status == RefundLineItemStatus.Failed ||
            l.Status == RefundLineItemStatus.Rejected);

        if (!allSettled)
            return Result.Success();

        Status = RefundRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        // Phase 6A.148.W5.6.B G1 — raise the email-driving event at the EXACT moment Status
        // flips to Completed. The guard above proves every line is in a terminal state, so
        // the totals below are final by construction — no race window can produce an
        // undercount (closes the 4th-report regression: RR 86d0a7dc 21:04:26.540, email
        // fired $94 while Sponsor line was still 831ms from committing).
        var refundedLines = _lineItems
            .Where(l => l.Status == RefundLineItemStatus.Refunded)
            .ToList();

        var totalRefunded = refundedLines.Sum(l => l.ApprovedAmount?.Amount ?? 0m);

        // Currency: prefer a refunded line; else any line that has an approved amount;
        // else fall back to the first requested amount (every line has one).
        var currency = refundedLines.Select(l => l.ApprovedAmount?.Currency)
            .FirstOrDefault(c => c is not null)
            ?? _lineItems.Select(l => l.ApprovedAmount?.Currency).FirstOrDefault(c => c is not null)
            ?? _lineItems[0].RequestedAmount.Currency;

        // Primary stripe refund id: ticket line wins (operator-recognisable as "the
        // registration refund"); else first refunded line; else null (all-Rejected request).
        var ticketLine = refundedLines.FirstOrDefault(l => l.Type == RefundLineItemType.Ticket);
        var primaryStripeRefundId = ticketLine?.StripeRefundId
            ?? refundedLines.FirstOrDefault()?.StripeRefundId;

        RaiseDomainEvent(new RefundRequestCompletedEvent(
            RefundRequestId: Id,
            RegistrationId: RegistrationId,
            PrimaryStripeRefundId: primaryStripeRefundId,
            TotalRefundedAmount: totalRefunded,
            Currency: currency.ToString(),
            CompletedAt: CompletedAt.Value));

        return Result.Success();
    }
}
