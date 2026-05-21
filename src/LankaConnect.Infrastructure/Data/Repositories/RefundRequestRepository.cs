using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.148 — EF Core implementation of <see cref="IRefundRequestRepository"/>.
///
/// Read operations involving the organizer queue use AsNoTracking projections per the
/// architect's review (read-side repository pattern). Command operations (Approve /
/// Reject / Withdraw) load tracked entities via <see cref="GetByIdAsync"/> so the
/// xmin concurrency token participates in SaveChanges.
/// </summary>
public class RefundRequestRepository : IRefundRequestRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<RefundRequestRepository> _logger;

    public RefundRequestRepository(AppDbContext context, ILogger<RefundRequestRepository> logger)
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

        try
        {
            return await _context.RefundRequestLineItems
                .AsNoTracking()
                .AnyAsync(li =>
                    li.Type == RefundLineItemType.Sponsor &&
                    li.ReferenceId == sponsorId &&
                    li.StripeRefundId == stripeRefundId,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RefundRequestRepository] ExistsWorkflowLineItemForSponsorAsync failed for SponsorId={SponsorId} StripeRefundId={StripeRefundId}",
                sponsorId, stripeRefundId);
            throw;
        }
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
}
