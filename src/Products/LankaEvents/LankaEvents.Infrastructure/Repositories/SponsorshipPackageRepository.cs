using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

using LankaConnect.BuildingBlocks.Abstractions;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Phase 6A.156 — concrete repository for <see cref="SponsorshipPackage"/>.
/// Atomic stock methods lifted byte-for-byte from
/// <see cref="AddOnDefinitionRepository"/>: a single SQL UPDATE with a WHERE
/// clause that re-checks the available quantity so two concurrent buyers
/// cannot oversell a 1-cap Gold slot.
///
/// All read/write paths wrap structured-logging context (<see cref="LogContext"/>)
/// + Stopwatch + try/catch per CLAUDE.md Section 4 observability rule.
/// </summary>
[Wave6_5TransitionalException("Inherits Repository<T> + uses AppDbContext via LankaConnect.Infrastructure.Data; cleared in Wave 6.5 LankaEventsDbContext extraction")]
public class SponsorshipPackageRepository : Repository<SponsorshipPackage>, ISponsorshipPackageRepository
{
    private readonly ILogger<SponsorshipPackageRepository> _repoLogger;

    public SponsorshipPackageRepository(
        AppDbContext context,
        ILogger<SponsorshipPackageRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SponsorshipPackage>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "SponsorshipPackage"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "SponsorshipPackage.GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var packages = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.EventId == eventId)
                    .OrderBy(p => p.SortOrder)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SponsorshipPackage.GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    packages.Count,
                    stopwatch.ElapsedMilliseconds);

                return packages;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SponsorshipPackage.GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SponsorshipPackage>> GetActiveByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveByEventId"))
        using (LogContext.PushProperty("EntityType", "SponsorshipPackage"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "SponsorshipPackage.GetActiveByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var packages = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.EventId == eventId && p.IsActive)
                    .OrderBy(p => p.SortOrder)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SponsorshipPackage.GetActiveByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    packages.Count,
                    stopwatch.ElapsedMilliseconds);

                return packages;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SponsorshipPackage.GetActiveByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryReserveStockAsync(
        Guid packageId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "TryReserveStock"))
        using (LogContext.PushProperty("EntityType", "SponsorshipPackage"))
        using (LogContext.PushProperty("PackageId", packageId))
        using (LogContext.PushProperty("Quantity", quantity))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "SponsorshipPackage.TryReserveStockAsync START: PackageId={PackageId}, Quantity={Quantity}",
                packageId, quantity);

            try
            {
                // Single UPDATE with re-check inside the WHERE clause. Postgres
                // makes this row-locked + atomic — two concurrent reservations
                // for the last slot are linearized; the loser sees rows=0.
                var sql = @"UPDATE events.sponsorship_packages
                    SET quantity_sold = quantity_sold + {0}, updated_at = NOW()
                    WHERE id = {1} AND is_active = true
                      AND (quantity_limit IS NULL OR quantity_sold + {0} <= quantity_limit)";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql, new object[] { quantity, packageId }, cancellationToken);

                var reserved = rows > 0;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SponsorshipPackage.TryReserveStockAsync COMPLETE: PackageId={PackageId}, Quantity={Quantity}, Reserved={Reserved}, Duration={ElapsedMs}ms",
                    packageId,
                    quantity,
                    reserved,
                    stopwatch.ElapsedMilliseconds);

                return reserved;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SponsorshipPackage.TryReserveStockAsync FAILED: PackageId={PackageId}, Quantity={Quantity}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    packageId,
                    quantity,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryRestoreStockAsync(
        Guid packageId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "TryRestoreStock"))
        using (LogContext.PushProperty("EntityType", "SponsorshipPackage"))
        using (LogContext.PushProperty("PackageId", packageId))
        using (LogContext.PushProperty("Quantity", quantity))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "SponsorshipPackage.TryRestoreStockAsync START: PackageId={PackageId}, Quantity={Quantity}",
                packageId, quantity);

            try
            {
                // GREATEST guards against underflow if a restore arrives twice
                // (idempotent boundary — same protection AddOnDefinition uses).
                var sql = @"UPDATE events.sponsorship_packages
                    SET quantity_sold = GREATEST(0, quantity_sold - {0}), updated_at = NOW()
                    WHERE id = {1}";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql, new object[] { quantity, packageId }, cancellationToken);

                var restored = rows > 0;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SponsorshipPackage.TryRestoreStockAsync COMPLETE: PackageId={PackageId}, Quantity={Quantity}, Restored={Restored}, Duration={ElapsedMs}ms",
                    packageId,
                    quantity,
                    restored,
                    stopwatch.ElapsedMilliseconds);

                return restored;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SponsorshipPackage.TryRestoreStockAsync FAILED: PackageId={PackageId}, Quantity={Quantity}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    packageId,
                    quantity,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
