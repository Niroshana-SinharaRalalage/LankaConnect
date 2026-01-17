using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Tax;
using LankaConnect.Domain.Tax.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.X: Repository implementation for StateTaxRate entity with comprehensive logging
/// </summary>
public class StateTaxRateRepository : Repository<StateTaxRate>, IStateTaxRateRepository
{
    public StateTaxRateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StateTaxRate?> GetActiveByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveByStateCode"))
        using (LogContext.PushProperty("EntityType", "StateTaxRate"))
        using (LogContext.PushProperty("StateCode", stateCode))
        {
            var stopwatch = Stopwatch.StartNew();
            var normalizedCode = stateCode.ToUpperInvariant();

            _logger.Debug(
                "GetActiveByStateCodeAsync START: StateCode={StateCode}, Normalized={NormalizedCode}",
                stateCode,
                normalizedCode);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.StateCode == normalizedCode && r.IsActive)
                    .OrderByDescending(r => r.EffectiveDate)
                    .FirstOrDefaultAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetActiveByStateCodeAsync COMPLETE: StateCode={StateCode}, Found={Found}, TaxRate={TaxRate}, Duration={ElapsedMs}ms",
                    normalizedCode,
                    result != null,
                    result?.TaxRate,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetActiveByStateCodeAsync FAILED: StateCode={StateCode}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    normalizedCode,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<StateTaxRate>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllActive"))
        using (LogContext.PushProperty("EntityType", "StateTaxRate"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug("GetAllActiveAsync START");

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.StateCode)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetAllActiveAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetAllActiveAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<StateTaxRate>> GetHistoryByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetHistoryByStateCode"))
        using (LogContext.PushProperty("EntityType", "StateTaxRate"))
        using (LogContext.PushProperty("StateCode", stateCode))
        {
            var stopwatch = Stopwatch.StartNew();
            var normalizedCode = stateCode.ToUpperInvariant();

            _logger.Debug(
                "GetHistoryByStateCodeAsync START: StateCode={StateCode}, Normalized={NormalizedCode}",
                stateCode,
                normalizedCode);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.StateCode == normalizedCode)
                    .OrderByDescending(r => r.EffectiveDate)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetHistoryByStateCodeAsync COMPLETE: StateCode={StateCode}, Count={Count}, Duration={ElapsedMs}ms",
                    normalizedCode,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetHistoryByStateCodeAsync FAILED: StateCode={StateCode}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    normalizedCode,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
