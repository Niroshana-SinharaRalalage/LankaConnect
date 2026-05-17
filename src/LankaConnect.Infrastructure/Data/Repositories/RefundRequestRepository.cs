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
            // Find the registration for this attendee on this event, then load their
            // most-recent refund request. Untracked — caller is rendering for /me.
            var registrationId = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.EventId == eventId && r.UserId == userId)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (registrationId == Guid.Empty) return null;

            return await _context.RefundRequests
                .AsNoTracking()
                .Include(r => r.LineItems)
                .Where(r => r.RegistrationId == registrationId)
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
}
