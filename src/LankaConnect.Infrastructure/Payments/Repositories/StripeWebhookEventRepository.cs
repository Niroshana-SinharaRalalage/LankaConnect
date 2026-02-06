using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Payments.Entities;

namespace LankaConnect.Infrastructure.Payments.Repositories;

/// <summary>
/// Repository implementation for StripeWebhookEvent infrastructure entity
/// Phase 6A.4: Stripe Payment Integration - Webhook Idempotency
/// </summary>
public class StripeWebhookEventRepository : IStripeWebhookEventRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<StripeWebhookEvent> _dbSet;
    private readonly ILogger<StripeWebhookEventRepository> _logger;

    public StripeWebhookEventRepository(AppDbContext context, ILogger<StripeWebhookEventRepository> logger)
    {
        _context = context;
        _dbSet = context.Set<StripeWebhookEvent>();
        _logger = logger;
    }

    /// <summary>
    /// Phase 6A.X FIX: Check if event has been fully PROCESSED (not just recorded).
    /// Returns true only if the event exists AND processed=true.
    /// This allows reprocessing of events that failed (processed=false).
    /// </summary>
    public async Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.EventId == eventId && e.Processed, cancellationToken);
    }

    /// <summary>
    /// Check if an event record exists (regardless of processed status).
    /// Used to prevent duplicate INSERT attempts on retries.
    /// </summary>
    public async Task<bool> EventExistsAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<Guid> RecordEventAsync(
        string eventId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var webhookEvent = StripeWebhookEvent.Create(eventId, eventType);
        await _dbSet.AddAsync(webhookEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return webhookEvent.Id;
    }

    public async Task MarkEventAsProcessedAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Phase 6A.X] [WebhookRepo-1] MarkEventAsProcessedAsync called - EventId: {EventId}",
            eventId);

        var webhookEvent = await _dbSet
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

        if (webhookEvent != null)
        {
            _logger.LogInformation(
                "[Phase 6A.X] [WebhookRepo-2] Found webhook event - EventId: {EventId}, CurrentProcessed: {Processed}",
                eventId, webhookEvent.Processed);

            // Get entity state BEFORE modification
            var stateBefore = _context.Entry(webhookEvent).State;
            _logger.LogInformation(
                "[Phase 6A.X] [WebhookRepo-2a] Entity state BEFORE MarkAsProcessed - EventId: {EventId}, State: {State}",
                eventId, stateBefore);

            webhookEvent.MarkAsProcessed();

            // Phase 6A.X FIX: Explicitly mark entity as Modified to ensure EF Core persists changes
            // After nested CommitAsync in domain event handlers, EF Core's change tracking snapshot
            // may not detect property changes. Explicitly setting Modified state forces the save.
            _context.Entry(webhookEvent).State = EntityState.Modified;

            var stateAfter = _context.Entry(webhookEvent).State;
            _logger.LogInformation(
                "[Phase 6A.X] [WebhookRepo-2b] Entity state AFTER explicit Modified - EventId: {EventId}, State: {State}",
                eventId, stateAfter);

            _logger.LogInformation(
                "[Phase 6A.X] [WebhookRepo-3] About to SaveChangesAsync - EventId: {EventId}, TrackedEntities: {TrackedCount}",
                eventId, _context.ChangeTracker.Entries().Count());

            // Phase 6A.92 FIX: Detach all entities EXCEPT the webhook event to avoid concurrency conflicts
            // The shared DbContext accumulates tracked entities from domain event handlers (email sending, etc.)
            // which can cause "expected to affect 1 row, but affected 0" errors when saving alongside webhook event
            var entriesToDetach = _context.ChangeTracker.Entries()
                .Where(e => !ReferenceEquals(e.Entity, webhookEvent))
                .ToList();

            _logger.LogInformation(
                "[Phase 6A.92] [WebhookRepo-3a] Detaching {Count} non-webhook entities to avoid concurrency conflicts",
                entriesToDetach.Count);

            foreach (var entry in entriesToDetach)
            {
                entry.State = EntityState.Detached;
            }

            var savedCount = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.X] [WebhookRepo-4] SaveChangesAsync completed - EventId: {EventId}, SavedCount: {SavedCount}",
                eventId, savedCount);
        }
        else
        {
            _logger.LogWarning(
                "[Phase 6A.X] [WebhookRepo-WARN] Webhook event not found - EventId: {EventId}",
                eventId);
        }
    }

    public async Task RecordAttemptAsync(
        string eventId,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var webhookEvent = await _dbSet
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

        if (webhookEvent != null)
        {
            webhookEvent.RecordAttempt(errorMessage);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
