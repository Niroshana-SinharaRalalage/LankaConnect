using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class RegistrationRepository : Repository<Registration>, IRegistrationRepository
{
    public RegistrationRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Override GetByIdAsync to enable tracking for scenarios where the entity will be modified
    /// and needs domain event dispatch (e.g., payment completion via Stripe webhook).
    /// Phase 6A.49: Fix for paid event email - ensures domain events are collected from ChangeTracker.
    /// Uses tracking (NOT AsNoTracking) so that when CompletePayment() raises PaymentCompletedEvent,
    /// the event is dispatched via AppDbContext.CommitAsync() → ChangeTracker.Entries&lt;BaseEntity&gt;().
    /// </summary>
    public override async Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Attendees)
            .Include(r => r.Contact)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Registration>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Registration>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId &&
                       r.Status != RegistrationStatus.Cancelled &&
                       r.Status != RegistrationStatus.Refunded)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Only return active registrations (exclude cancelled and refunded)
        // This fixes the multi-attendee re-registration issue (Session 30)
        // Phase 6A.41: Fixed to return NEWEST registration (OrderByDescending)
        // and include Attendees collection to prevent stale data issues
        return await _dbSet
            .AsNoTracking()
            .Include(r => r.Attendees)
            .Include(r => r.Contact)
            .Where(r => r.EventId == eventId &&
                       r.UserId == userId &&
                       r.Status != RegistrationStatus.Cancelled &&
                       r.Status != RegistrationStatus.Refunded)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Registration>> GetByStatusAsync(RegistrationStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalQuantityForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
            .SumAsync(r => r.Quantity, cancellationToken);
    }

    /// <summary>
    /// Phase 6A.24: Gets an anonymous registration by event ID and contact email
    /// Used to fetch registration details for anonymous users' confirmation emails
    /// Phase 6A.41: Fixed to return NEWEST registration with includes
    /// </summary>
    public async Task<Registration?> GetAnonymousByEventAndEmailAsync(Guid eventId, string email, CancellationToken cancellationToken = default)
    {
        // Look for anonymous registrations (UserId is null) with matching contact email
        // Return newest registration with all related data loaded
        return await _dbSet
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
    }
}