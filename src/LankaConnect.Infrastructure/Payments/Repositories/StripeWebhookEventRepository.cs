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

    public async Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.EventId == eventId && e.Processed, cancellationToken);
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
