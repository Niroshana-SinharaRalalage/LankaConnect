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
    /// Phase 6A.24 FIX: Check if event has been RECORDED (not just processed).
    ///
    /// Previous bug: Only checked for Processed=true, which caused 500 errors on Stripe retries.
    /// If webhook was recorded but not yet marked processed, retry would pass this check
    /// but fail on INSERT due to unique constraint on EventId.
    ///
    /// Fix: Check if ANY record exists with this EventId, regardless of processed status.
    /// This prevents duplicate INSERT attempts on webhook retries.
    /// </summary>
    public async Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default)
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

            webhookEvent.MarkAsProcessed();

            _logger.LogInformation(
                "[Phase 6A.X] [WebhookRepo-3] About to SaveChangesAsync - EventId: {EventId}, TrackedEntities: {TrackedCount}",
                eventId, _context.ChangeTracker.Entries().Count());

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
