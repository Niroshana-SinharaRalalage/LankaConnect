using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class RegistrationRepository : Repository<Registration>, IRegistrationRepository
{
    private readonly ILogger<RegistrationRepository> _repoLogger;

    public RegistrationRepository(
        AppDbContext context,
        ILogger<RegistrationRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Override GetByIdAsync to enable tracking for scenarios where the entity will be modified
    /// and needs domain event dispatch (e.g., payment completion via Stripe webhook).
    /// Phase 6A.49: Fix for paid event email - ensures domain events are collected from ChangeTracker.
    /// Uses tracking (NOT AsNoTracking) so that when CompletePayment() raises PaymentCompletedEvent,
    /// the event is dispatched via AppDbContext.CommitAsync() → ChangeTracker.Entries&lt;BaseEntity&gt;().
    /// Phase 6A.X: Added comprehensive logging with LogContext, Stopwatch, and PostgreSQL SqlState extraction
    /// </summary>
    public override async Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByIdAsync START: EntityId={EntityId}",
                id);

            try
            {
                var result = await _dbSet
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    // Phase 6A.148: include refund requests + their line items so the approval-
                    // workflow handlers can mutate them in the same tracked load without a
                    // second round-trip. Typical refund_requests per registration: 0-1.
                    .Include(r => r.RefundRequests)
                        .ThenInclude(rr => rr.LineItems)
                    .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdAsync COMPLETE: EntityId={EntityId}, Found={Found}, Status={Status}, Duration={ElapsedMs}ms",
                    id,
                    result != null,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.93: Added trackChanges parameter for write operations.
    /// When trackChanges is true, entities are tracked by EF Core, enabling:
    /// - State change detection on SaveChanges
    /// - Domain event dispatch via CommitAsync
    /// Use trackChanges: true for auto-refund processing in EventCancellationEmailJob.
    /// </summary>
    public async Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default, bool trackChanges = false)
    {
        using (LogContext.PushProperty("Operation", "GetByEvent"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("TrackChanges", trackChanges))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventAsync START: EventId={EventId}, TrackChanges={TrackChanges}", eventId, trackChanges);

            try
            {
                // Phase 6A.93: Conditionally apply AsNoTracking based on trackChanges parameter
                // When trackChanges is true, entities are tracked for write operations (e.g., refund processing)
                IQueryable<Registration> query = _dbSet.Where(r => r.EventId == eventId);

                if (!trackChanges)
                {
                    query = query.AsNoTracking();
                }

                var result = await query
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventAsync COMPLETE: EventId={EventId}, Count={Count}, TrackChanges={TrackChanges}, Duration={ElapsedMs}ms",
                    eventId,
                    result.Count,
                    trackChanges,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventAsync FAILED: EventId={EventId}, TrackChanges={TrackChanges}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    trackChanges,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUser"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByUserAsync START: UserId={UserId}", userId);

            try
            {
                // Phase 6A.81 Part 4 FIX: Include Preliminary to show Payment Pending UI
                // IMPORTANT: Preliminary must be included so frontend can detect existing unpaid registrations
                // and show the Payment Pending UI with countdown timer instead of registration form.
                // Phase 6A.93 FIX: Include RefundRequested to show "Refund in Progress" UI
                // Excluded: Abandoned (expired), Cancelled, Refunded, Pending (deprecated)
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.UserId == userId &&
                               (r.Status == RegistrationStatus.Preliminary ||  // Phase 6A.81: Payment pending
                                r.Status == RegistrationStatus.Confirmed ||
                                r.Status == RegistrationStatus.RefundRequested ||  // Phase 6A.93: Refund in progress
                                r.Status == RegistrationStatus.Waitlisted ||
                                r.Status == RegistrationStatus.CheckedIn ||
                                r.Status == RegistrationStatus.Attended))
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUserAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventAndUser"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventAndUserAsync START: EventId={EventId}, UserId={UserId}",
                eventId,
                userId);

            try
            {
                // Only return active registrations (exclude cancelled and refunded)
                // This fixes the multi-attendee re-registration issue (Session 30)
                // Phase 6A.41: Fixed to return NEWEST registration (OrderByDescending)
                // and include Attendees collection to prevent stale data issues
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    .Where(r => r.EventId == eventId &&
                               r.UserId == userId &&
                               r.Status != RegistrationStatus.Cancelled &&
                               r.Status != RegistrationStatus.Refunded)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventAndUserAsync COMPLETE: EventId={EventId}, UserId={UserId}, Found={Found}, Status={Status}, Duration={ElapsedMs}ms",
                    eventId,
                    userId,
                    result != null,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventAndUserAsync FAILED: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByStatus"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByStatusAsync START: Status={Status}", status);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.Status == status)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByStatusAsync FAILED: Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 7G — returns up to <paramref name="take"/> registrations stuck in
    /// <see cref="RegistrationStatus.RefundRequested"/> whose <c>RefundRequestedAt</c>
    /// is older than <paramref name="requestedBefore"/>. Tracked load — caller
    /// (<c>RefundReconciliationService</c>) needs to mutate state via
    /// <c>Registration.CompleteRefund</c> and persist via the existing
    /// <c>IUnitOfWork</c>. Ordered oldest-first so the most painfully-stuck rows
    /// are reconciled first when a backlog has accumulated.
    /// </summary>
    public async Task<IReadOnlyList<Registration>> GetStuckRefundsAsync(
        DateTime requestedBefore,
        int take,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetStuckRefunds"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetStuckRefundsAsync START: RequestedBefore={RequestedBefore:o}, Take={Take}",
                requestedBefore, take);

            try
            {
                var result = await _dbSet
                    .Where(r => r.Status == RegistrationStatus.RefundRequested
                                && r.RefundRequestedAt != null
                                && r.RefundRequestedAt < requestedBefore)
                    .OrderBy(r => r.RefundRequestedAt)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetStuckRefundsAsync COMPLETE: Count={Count}, RequestedBefore={RequestedBefore:o}, Take={Take}, Duration={ElapsedMs}ms",
                    result.Count, requestedBefore, take, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetStuckRefundsAsync FAILED: RequestedBefore={RequestedBefore:o}, Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    requestedBefore, take, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Registration Registration, string StripeRefundId)>>
        GetStuckCancelledWithRefundedTicketAsync(
            DateTime processedBefore,
            int take,
            CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetStuckCancelledWithRefundedTicket"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetStuckCancelledWithRefundedTicketAsync START: ProcessedBefore={ProcessedBefore:o}, Take={Take}",
                processedBefore, take);

            try
            {
                // Find registration IDs that match the stuck pattern:
                //   r.Status='Cancelled' AND r.RefundCompletedAt IS NULL AND r.StripeRefundId IS NULL
                //   AND some workflow ticket line is already Refunded with stripe_refund_id NOT NULL
                //   AND that line's processed_at is older than threshold (gives the W5.D5 webhook
                //   handler a chance to settle the row normally).
                //
                // Projection returns (RegistrationId, StripeRefundId) pairs; we then load the
                // tracked Registration aggregates by Id for the reconciler to mutate.
                var stuckPairs = await (
                    from r in _dbSet.AsNoTracking()
                    where r.Status == RegistrationStatus.Cancelled
                          && r.RefundCompletedAt == null
                          && r.StripeRefundId == null
                    let ticketLine = (
                        from rr in _context.Set<RefundRequest>().AsNoTracking()
                        join li in _context.Set<RefundRequestLineItem>().AsNoTracking()
                            on rr.Id equals li.RefundRequestId
                        where rr.RegistrationId == r.Id
                              && li.Type == RefundLineItemType.Ticket
                              && li.Status == RefundLineItemStatus.Refunded
                              && li.StripeRefundId != null
                              && li.ProcessedAt != null
                              && li.ProcessedAt < processedBefore
                        orderby li.ProcessedAt
                        select li.StripeRefundId
                    ).FirstOrDefault()
                    where ticketLine != null
                    select new { RegistrationId = r.Id, StripeRefundId = ticketLine })
                    .Take(take)
                    .ToListAsync(cancellationToken);

                if (stuckPairs.Count == 0)
                {
                    stopwatch.Stop();
                    _repoLogger.LogDebug(
                        "GetStuckCancelledWithRefundedTicketAsync COMPLETE: Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<(Registration, string)>();
                }

                // Load tracked Registrations by Id so the caller can call
                // CompleteRefundFromCancelled + IUnitOfWork.CommitAsync.
                var ids = stuckPairs.Select(p => p.RegistrationId).ToList();
                var tracked = await _dbSet
                    .Where(r => ids.Contains(r.Id))
                    .ToListAsync(cancellationToken);
                var trackedById = tracked.ToDictionary(r => r.Id);

                var result = stuckPairs
                    .Where(p => trackedById.ContainsKey(p.RegistrationId))
                    .Select(p => (trackedById[p.RegistrationId], p.StripeRefundId!))
                    .ToList();

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetStuckCancelledWithRefundedTicketAsync COMPLETE: Count={Count}, ProcessedBefore={ProcessedBefore:o}, Take={Take}, Duration={ElapsedMs}ms",
                    result.Count, processedBefore, take, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetStuckCancelledWithRefundedTicketAsync FAILED: ProcessedBefore={ProcessedBefore:o}, Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    processedBefore, take, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalQuantity"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetTotalQuantityForEventAsync START: EventId={EventId}", eventId);

            try
            {
                var result = await _dbSet
                    .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
                    .SumAsync(r => r.Quantity, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalQuantityForEventAsync COMPLETE: EventId={EventId}, TotalQuantity={TotalQuantity}, Duration={ElapsedMs}ms",
                    eventId,
                    result,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalQuantityForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.24: Gets an anonymous registration by event ID and contact email
    /// Used to fetch registration details for anonymous users' confirmation emails
    /// Phase 6A.41: Fixed to return NEWEST registration with includes
    /// Phase 6A.X: Added comprehensive logging with LogContext, Stopwatch, and PostgreSQL SqlState extraction
    /// </summary>
    public async Task<Registration?> GetAnonymousByEventAndEmailAsync(Guid eventId, string email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAnonymousByEventAndEmail"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("Email", email))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetAnonymousByEventAndEmailAsync START: EventId={EventId}, Email={Email}",
                eventId,
                email);

            try
            {
                // Look for anonymous registrations (UserId is null) with matching contact email
                // Return newest registration with all related data loaded
                var result = await _dbSet
                    .AsNoTracking()
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    .Where(r => r.EventId == eventId &&
                               r.UserId == null &&
                               r.Contact != null &&
                               r.Contact.Email == email &&
                               r.Status != RegistrationStatus.Cancelled &&
                               r.Status != RegistrationStatus.Refunded)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAnonymousByEventAndEmailAsync COMPLETE: EventId={EventId}, Email={Email}, Found={Found}, Status={Status}, Duration={ElapsedMs}ms",
                    eventId,
                    email,
                    result != null,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAnonymousByEventAndEmailAsync FAILED: EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId,
                    email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.X: Gets a registration by Stripe PaymentIntentId.
    /// Used by charge.refunded webhook handler as fallback when refund metadata is missing.
    /// Returns the registration WITH tracking enabled so CompleteRefund() can raise domain events.
    /// </summary>
    public async Task<Registration?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByPaymentIntentId"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("PaymentIntentId", paymentIntentId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByPaymentIntentIdAsync START: PaymentIntentId={PaymentIntentId}",
                paymentIntentId);

            try
            {
                // Use tracking (NOT AsNoTracking) so domain events are dispatched via CommitAsync
                var result = await _dbSet
                    .Include(r => r.Attendees)
                    .Include(r => r.Contact)
                    .FirstOrDefaultAsync(r => r.StripePaymentIntentId == paymentIntentId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByPaymentIntentIdAsync COMPLETE: PaymentIntentId={PaymentIntentId}, Found={Found}, RegId={RegId}, Status={Status}, Duration={ElapsedMs}ms",
                    paymentIntentId,
                    result != null,
                    result?.Id,
                    result?.Status,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByPaymentIntentIdAsync FAILED: PaymentIntentId={PaymentIntentId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    paymentIntentId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}