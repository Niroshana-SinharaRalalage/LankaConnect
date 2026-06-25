using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for AddOnDefinition operations.
/// Includes atomic stock management methods using raw SQL.
/// </summary>
public class AddOnDefinitionRepository : Repository<AddOnDefinition>, IAddOnDefinitionRepository
{
    private readonly ILogger<AddOnDefinitionRepository> _repoLogger;

    public AddOnDefinitionRepository(
        AppDbContext context,
        ILogger<AddOnDefinitionRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnDefinition>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "AddOnDefinition"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var definitions = await _dbSet
                    .AsNoTracking()
                    .Where(d => d.EventId == eventId)
                    .OrderBy(d => d.SortOrder)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    definitions.Count,
                    stopwatch.ElapsedMilliseconds);

                return definitions;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnDefinition>> GetActiveByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveByEventId"))
        using (LogContext.PushProperty("EntityType", "AddOnDefinition"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetActiveByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var definitions = await _dbSet
                    .AsNoTracking()
                    .Where(d => d.EventId == eventId && d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetActiveByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    definitions.Count,
                    stopwatch.ElapsedMilliseconds);

                return definitions;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetActiveByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryReserveStockAsync(
        Guid definitionId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "TryReserveStock"))
        using (LogContext.PushProperty("EntityType", "AddOnDefinition"))
        using (LogContext.PushProperty("DefinitionId", definitionId))
        using (LogContext.PushProperty("Quantity", quantity))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "TryReserveStockAsync START: DefinitionId={DefinitionId}, Quantity={Quantity}",
                definitionId, quantity);

            try
            {
                var sql = @"UPDATE events.add_on_definitions
                    SET quantity_sold = quantity_sold + {0}, updated_at = NOW()
                    WHERE id = {1} AND is_active = true
                      AND (quantity_limit IS NULL OR quantity_sold + {0} <= quantity_limit)";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql, new object[] { quantity, definitionId }, cancellationToken);

                var reserved = rows > 0;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "TryReserveStockAsync COMPLETE: DefinitionId={DefinitionId}, Quantity={Quantity}, Reserved={Reserved}, Duration={ElapsedMs}ms",
                    definitionId,
                    quantity,
                    reserved,
                    stopwatch.ElapsedMilliseconds);

                return reserved;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "TryReserveStockAsync FAILED: DefinitionId={DefinitionId}, Quantity={Quantity}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    definitionId,
                    quantity,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryRestoreStockAsync(
        Guid definitionId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "TryRestoreStock"))
        using (LogContext.PushProperty("EntityType", "AddOnDefinition"))
        using (LogContext.PushProperty("DefinitionId", definitionId))
        using (LogContext.PushProperty("Quantity", quantity))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "TryRestoreStockAsync START: DefinitionId={DefinitionId}, Quantity={Quantity}",
                definitionId, quantity);

            try
            {
                var sql = @"UPDATE events.add_on_definitions
                    SET quantity_sold = GREATEST(0, quantity_sold - {0}), updated_at = NOW()
                    WHERE id = {1}";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql, new object[] { quantity, definitionId }, cancellationToken);

                var restored = rows > 0;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "TryRestoreStockAsync COMPLETE: DefinitionId={DefinitionId}, Quantity={Quantity}, Restored={Restored}, Duration={ElapsedMs}ms",
                    definitionId,
                    quantity,
                    restored,
                    stopwatch.ElapsedMilliseconds);

                return restored;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "TryRestoreStockAsync FAILED: DefinitionId={DefinitionId}, Quantity={Quantity}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    definitionId,
                    quantity,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
