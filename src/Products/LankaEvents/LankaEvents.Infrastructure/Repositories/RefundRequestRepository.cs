using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Phase 6A.148 — EF Core implementation of <see cref="IRefundRequestRepository"/>.
///
/// <para>
/// Consult #22 (2026-07-10): moved from <c>Payments.Infrastructure.Repositories</c> to
/// <c>LankaEvents.Infrastructure.Repositories</c> per Consult #21 Q2 (2a) ruling.
/// Ctor injects <c>LankaEventsDbContext</c> — the module DbContext that owns RefundRequest,
/// RefundRequestLineItem, and Registration aggregates. Previously injected AppDbContext
/// (cross-module boundary violation masked by dual-mapping ApplyConfigurationsFromAssembly).
/// </para>
///
/// Read operations involving the organizer queue use AsNoTracking projections per the
/// architect's review (read-side repository pattern). Command operations (Approve /
/// Reject / Withdraw) load tracked entities via <see cref="GetByIdAsync"/> so the
/// xmin concurrency token participates in SaveChanges.
/// </summary>
public class RefundRequestRepository : IRefundRequestRepository
{
    private readonly LankaEventsDbContext _context;
    private readonly ILogger<RefundRequestRepository> _logger;

    public RefundRequestRepository(LankaEventsDbContext context, ILogger<RefundRequestRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RefundRequests
                .Include(r => r.LineItems)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RefundRequestRepository] GetByIdAsync failed for Id={Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefundRequest>> GetByRegistrationIdAsync(
        Guid registrationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RefundRequests
                .Include(r => r.LineItems)
                .Where(r => r.RegistrationId == registrationId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] GetByRegistrationIdAsync failed for RegistrationId={RegId}",
                registrationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RefundRequest?> GetMyMostRecentForEventAsync(
        Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find ALL registration IDs for this attendee on this event. A user can
            // have multiple historical registrations (cancelled + re-registered, etc.),
            // so we don't pick "the" registration here — we go straight to the refund
            // requests across any of them and return the most recent by RequestedAt.
            // Untracked — caller is rendering for /me.
            var registrationIds = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.EventId == eventId && r.UserId == userId)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (registrationIds.Count == 0) return null;

            return await _context.RefundRequests
                .AsNoTracking()
                .Include(r => r.LineItems)
                .Where(r => registrationIds.Contains(r.RegistrationId))
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] GetMyMostRecentForEventAsync failed for EventId={EventId} UserId={UserId}",
                eventId, userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefundRequest>> ListByEventAsync(
        Guid eventId,
        RefundRequestStatus? statusFilter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Join via Registration since RefundRequest doesn't carry EventId directly
            // (it lives on the parent registration). Untracked — read-only projection.
            var query =
                from rr in _context.RefundRequests.AsNoTracking().Include(r => r.LineItems)
                join reg in _context.Registrations.AsNoTracking()
                    on rr.RegistrationId equals reg.Id
                where reg.EventId == eventId
                select rr;

            if (statusFilter.HasValue)
                query = query.Where(rr => rr.Status == statusFilter.Value);

            return await query
                .OrderByDescending(rr => rr.RequestedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] ListByEventAsync failed for EventId={EventId} StatusFilter={Filter}",
                eventId, statusFilter);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefundRequest>> ListStuckApprovedAsync(
        DateTime olderThanUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            // Architect F11: candidates to re-dispatch via RefundReconciliationService.
            // Tracked because the reconciler will call BeginProcessing on these.
            return await _context.RefundRequests
                .Include(r => r.LineItems)
                .Where(r => r.Status == RefundRequestStatus.Approved &&
                            (r.UpdatedAt ?? r.CreatedAt) < olderThanUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] ListStuckApprovedAsync failed for olderThanUtc={Threshold}",
                olderThanUtc);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWorkflowLineItemForSponsorAsync(
        Guid sponsorId, string stripeRefundId, CancellationToken cancellationToken = default)
    {
        // Defensive: a null/empty stripeRefundId cannot match a stored line, so don't
        // even hit the DB — return false so the caller falls back to the legacy email.
        if (string.IsNullOrWhiteSpace(stripeRefundId))
            return false;

        // W5.5.D5: retry until true. The D9 suppression path is the operator-UAT Bug 3 —
        // the legacy "Sponsorship Refund Confirmed" email leaked through for a workflow
        // refund because this check ran BEFORE the dispatcher's per-line StripeRefundId
        // commit was visible to this scope. Bounded retry covers the race window.
        // Returns false after all retries exhaust → fail-OPEN to legacy email (preferred
        // to silencing notifications on a transient DB lookup miss).
        int[] backoffMs = { 0, 100, 300, 1000 };
        for (var i = 0; i < backoffMs.Length; i++)
        {
            if (backoffMs[i] > 0)
                await Task.Delay(backoffMs[i], cancellationToken);

            try
            {
                var found = await _context.RefundRequestLineItems
                    .AsNoTracking()
                    .AnyAsync(li =>
                        li.Type == RefundLineItemType.Sponsor &&
                        li.ReferenceId == sponsorId &&
                        li.StripeRefundId == stripeRefundId,
                        cancellationToken);
                if (found)
                {
                    if (i > 0)
                        _logger.LogInformation(
                            "[RefundRequestRepository.W5.5.D5] ExistsWorkflowLineItemForSponsorAsync succeeded on attempt {Attempt}/{Total} after {DelayMs}ms cumulative backoff for SponsorId={SponsorId} StripeRefundId={StripeRefundId}",
                            i + 1, backoffMs.Length,
                            backoffMs.Take(i + 1).Sum(), sponsorId, stripeRefundId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[RefundRequestRepository] ExistsWorkflowLineItemForSponsorAsync threw on attempt {Attempt}/{Total} for SponsorId={SponsorId} StripeRefundId={StripeRefundId}",
                    i + 1, backoffMs.Length, sponsorId, stripeRefundId);
                throw;
            }
        }

        _logger.LogInformation(
            "[RefundRequestRepository.W5.5.D5] ExistsWorkflowLineItemForSponsorAsync false after all {Total} attempts for SponsorId={SponsorId} StripeRefundId={StripeRefundId} — caller will fall back to legacy email",
            backoffMs.Length, sponsorId, stripeRefundId);
        return false;
    }

    /// <inheritdoc />
    public async Task<string?> GetWorkflowOwnedAttendeeEmailForSponsorAsync(
        Guid sponsorId, string stripeRefundId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stripeRefundId))
            return null;

        // Same bounded-retry pattern as ExistsWorkflowLineItemForSponsorAsync — covers
        // the race window where the webhook arrives before the dispatcher's per-line
        // StripeRefundId commit is visible to this scope.
        int[] backoffMs = { 0, 100, 300, 1000 };
        for (var i = 0; i < backoffMs.Length; i++)
        {
            if (backoffMs[i] > 0)
                await Task.Delay(backoffMs[i], cancellationToken);

            try
            {
                // Join: line → refund_request → registration.contact.email (owned via OwnsOne).
                // Returns null when no workflow line yet exists for the (sponsor, sri) pair.
                var attendeeEmail = await _context.RefundRequestLineItems
                    .AsNoTracking()
                    .Where(li =>
                        li.Type == RefundLineItemType.Sponsor &&
                        li.ReferenceId == sponsorId &&
                        li.StripeRefundId == stripeRefundId)
                    .Join(_context.RefundRequests.AsNoTracking(),
                        li => li.RefundRequestId,
                        rr => rr.Id,
                        (li, rr) => rr.RegistrationId)
                    .Join(_context.Registrations.AsNoTracking(),
                        regId => regId,
                        reg => reg.Id,
                        // Reg.Contact is the RegistrationContact owned value-object — never null
                        // for a paid registration (set at CreateWithAttendees time), but use
                        // null-conditional to satisfy nullable analysis on the projection.
                        (regId, reg) => reg.Contact != null ? reg.Contact.Email : null)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(attendeeEmail))
                {
                    if (i > 0)
                        _logger.LogInformation(
                            "[RefundRequestRepository.W5.6.B] GetWorkflowOwnedAttendeeEmailForSponsorAsync resolved on attempt {Attempt}/{Total} for SponsorId={SponsorId} StripeRefundId={StripeRefundId}",
                            i + 1, backoffMs.Length, sponsorId, stripeRefundId);
                    return attendeeEmail;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[RefundRequestRepository.W5.6.B] GetWorkflowOwnedAttendeeEmailForSponsorAsync threw on attempt {Attempt}/{Total} for SponsorId={SponsorId} StripeRefundId={StripeRefundId}",
                    i + 1, backoffMs.Length, sponsorId, stripeRefundId);
                throw;
            }
        }

        _logger.LogInformation(
            "[RefundRequestRepository.W5.6.B] GetWorkflowOwnedAttendeeEmailForSponsorAsync null after all {Total} attempts for SponsorId={SponsorId} StripeRefundId={StripeRefundId} — caller will fall back to legacy email",
            backoffMs.Length, sponsorId, stripeRefundId);
        return null;
    }

    /// <inheritdoc />
    public async Task<Guid?> GetWorkflowLineReferenceIdAsync(
        RefundLineItemType type, string stripeRefundId, CancellationToken cancellationToken = default)
    {
        // Defensive: a null/empty stripeRefundId cannot match a stored line; return null
        // so the caller falls back to legacy semantics (refund-all-on-PI for AddOn cart;
        // send-legacy-email for Sponsor/Collection).
        if (string.IsNullOrWhiteSpace(stripeRefundId))
            return null;

        try
        {
            // Untracked single-row projection. (Type, StripeRefundId) is unique by
            // construction — Stripe refund IDs are globally unique and each workflow
            // line owns at most one Stripe refund.
            return await _context.RefundRequestLineItems
                .AsNoTracking()
                .Where(li => li.Type == type && li.StripeRefundId == stripeRefundId)
                .Select(li => (Guid?)li.ReferenceId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] GetWorkflowLineReferenceIdAsync failed for Type={Type} StripeRefundId={StripeRefundId}",
                type, stripeRefundId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RefundRequestLineItem?> GetLineItemByIdAsync(
        Guid lineItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tracked load — caller mutates within the same scope and calls SaveChangesAsync.
            // No Include() on RefundRequest navigation: we deliberately want only this line's
            // row touched so EF doesn't pick up changes to a stale-loaded parent aggregate
            // (the W5.D7 xmin clash root cause). RefundRequestLineItem table has no xmin
            // concurrency token (verified RefundRequestLineItemConfiguration.cs), so per-line
            // saves never conflict with concurrent Registration writes.
            return await _context.RefundRequestLineItems
                .FirstOrDefaultAsync(li => li.Id == lineItemId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] GetLineItemByIdAsync failed for LineItemId={LineItemId}",
                lineItemId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RefundRequestLineItem?> GetWorkflowLineByStripeRefundIdAsync(
        string stripeRefundId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stripeRefundId))
            return null;

        // W5.5.D5: retry on null. The dispatcher (RefundLineDispatcher) commits the line's
        // StripeRefundId in a SEPARATE DI scope from the webhook handler. Stripe fires the
        // webhook within milliseconds of the dispatcher's Stripe call returning — there's a
        // tight read-after-write window where the webhook arrives before the dispatcher's
        // transaction commit is visible to other sessions. Bounded retry covers this window
        // without adding latency for the common case (first read succeeds).
        return await RetryUntilNonNullAsync(
            ct => _context.RefundRequestLineItems
                .AsNoTracking()
                .Where(li => li.StripeRefundId == stripeRefundId)
                .FirstOrDefaultAsync(ct),
            "GetWorkflowLineByStripeRefundIdAsync",
            stripeRefundId,
            cancellationToken);
    }

    /// <summary>
    /// W5.5.D5: bounded retry helper for read-after-write race against the per-line dispatcher.
    /// Returns first non-null result, or null after all retries exhaust. Total max delay 1.4s
    /// (100ms + 300ms + 1000ms) split across 4 attempts. Logs at INFO when retry recovers a
    /// null read — operator-visible signal that the race is happening, but not an error
    /// because eventual consistency works.
    /// </summary>
    private async Task<RefundRequestLineItem?> RetryUntilNonNullAsync(
        Func<CancellationToken, Task<RefundRequestLineItem?>> read,
        string operation,
        string stripeRefundId,
        CancellationToken cancellationToken)
    {
        // Attempt 0 + 3 backoff retries.
        int[] backoffMs = { 0, 100, 300, 1000 };
        for (var i = 0; i < backoffMs.Length; i++)
        {
            if (backoffMs[i] > 0)
                await Task.Delay(backoffMs[i], cancellationToken);

            try
            {
                var result = await read(cancellationToken);
                if (result != null)
                {
                    if (i > 0)
                        _logger.LogInformation(
                            "[RefundRequestRepository.W5.5.D5] {Operation} succeeded on attempt {Attempt}/{Total} after {DelayMs}ms cumulative backoff for StripeRefundId={StripeRefundId}",
                            operation, i + 1, backoffMs.Length,
                            backoffMs.Take(i + 1).Sum(), stripeRefundId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[RefundRequestRepository] {Operation} threw on attempt {Attempt}/{Total} for StripeRefundId={StripeRefundId}",
                    operation, i + 1, backoffMs.Length, stripeRefundId);
                throw;
            }
        }

        _logger.LogInformation(
            "[RefundRequestRepository.W5.5.D5] {Operation} returned null after all {Total} attempts for StripeRefundId={StripeRefundId} — caller will fall back to legacy semantics",
            operation, backoffMs.Length, stripeRefundId);
        return null;
    }
}
