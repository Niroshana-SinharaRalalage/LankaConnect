using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Tax;
using LankaConnect.Domain.Tax.Repositories;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.X: Repository implementation for StateTaxRate entity
/// </summary>
public class StateTaxRateRepository : Repository<StateTaxRate>, IStateTaxRateRepository
{
    public StateTaxRateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StateTaxRate?> GetActiveByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = stateCode.ToUpperInvariant();

        return await _dbSet
            .AsNoTracking()
            .Where(r => r.StateCode == normalizedCode && r.IsActive)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StateTaxRate>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.StateCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StateTaxRate>> GetHistoryByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = stateCode.ToUpperInvariant();

        return await _dbSet
            .AsNoTracking()
            .Where(r => r.StateCode == normalizedCode)
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync(cancellationToken);
    }
}
