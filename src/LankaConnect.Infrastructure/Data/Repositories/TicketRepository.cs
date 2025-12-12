using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.24: Repository implementation for ticket operations
/// </summary>
public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetByTicketCodeAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.RegistrationId == registrationId && t.IsValid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.IsValid)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TicketCodeExistsAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(t => t.TicketCode == ticketCode, cancellationToken);
    }
}
