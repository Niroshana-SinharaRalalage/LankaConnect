using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Payments.Entities;

namespace LankaConnect.Infrastructure.Payments.Repositories;

/// <summary>
/// Repository implementation for StripeCustomer infrastructure entity
/// Phase 6A.4: Stripe Payment Integration - MVP
/// </summary>
public class StripeCustomerRepository : IStripeCustomerRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<StripeCustomer> _dbSet;

    public StripeCustomerRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<StripeCustomer>();
    }

    public async Task<string?> GetStripeCustomerIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.UserId == userId, cancellationToken);

        return customer?.StripeCustomerId;
    }

    public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(sc => sc.UserId == userId, cancellationToken);
    }

    public async Task SaveStripeCustomerAsync(
        Guid userId,
        string stripeCustomerId,
        string email,
        string name,
        DateTime stripeCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var existingCustomer = await _dbSet
            .FirstOrDefaultAsync(sc => sc.UserId == userId, cancellationToken);

        if (existingCustomer != null)
        {
            // Update existing customer (email/name might have changed)
            _context.Entry(existingCustomer).Property("Email").CurrentValue = email;
            _context.Entry(existingCustomer).Property("Name").CurrentValue = name;
            _context.Entry(existingCustomer).Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
        else
        {
            // Create new customer
            var customer = StripeCustomer.Create(userId, stripeCustomerId, email, name, stripeCreatedAt);
            await _dbSet.AddAsync(customer, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
