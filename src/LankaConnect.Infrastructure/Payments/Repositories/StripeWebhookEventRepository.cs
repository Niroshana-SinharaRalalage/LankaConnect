using Microsoft.EntityFrameworkCore;
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

    public StripeWebhookEventRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<StripeWebhookEvent>();
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
        var webhookEvent = await _dbSet
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

        if (webhookEvent != null)
        {
            webhookEvent.MarkAsProcessed();
            await _context.SaveChangesAsync(cancellationToken);
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
