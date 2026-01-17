using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.X: Repository implementation template with comprehensive logging
///
/// TEMPLATE USAGE:
/// 1. Replace {Entity} with your entity name (e.g., Event, Registration, User)
/// 2. Replace {PrimaryKey} with primary key type (usually Guid)
/// 3. Add all repository methods following the pattern below
/// 4. Each method MUST have:
///    - LogContext.PushProperty for Operation, EntityType, and relevant context
///    - Stopwatch for performance timing
///    - Debug log at START with input parameters
///    - Try-catch with Error logging including SqlState
///    - Information log at COMPLETE with results and duration
///
/// REFERENCE IMPLEMENTATION: StateTaxRateRepository.cs
/// </summary>
public class {Entity}Repository : Repository<{Entity}>, I{Entity}Repository
{
    public {Entity}Repository(AppDbContext context) : base(context)
    {
    }

    // ============================================================================
    // TEMPLATE METHOD 1: GetByIdAsync
    // ============================================================================
    public async Task<{Entity}?> GetByIdAsync({PrimaryKey} id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "GetByIdAsync START: EntityId={EntityId}",
                id);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetByIdAsync COMPLETE: EntityId={EntityId}, Found={Found}, Duration={ElapsedMs}ms",
                    id,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetByIdAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // ============================================================================
    // TEMPLATE METHOD 2: GetAllAsync (with pagination)
    // ============================================================================
    public async Task<IReadOnlyList<{Entity}>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAll"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        using (LogContext.PushProperty("PageNumber", pageNumber))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "GetAllAsync START: PageNumber={PageNumber}, PageSize={PageSize}",
                pageNumber,
                pageSize);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .OrderBy(e => e.Id) // Replace with appropriate ordering
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetAllAsync COMPLETE: Count={Count}, PageNumber={PageNumber}, PageSize={PageSize}, Duration={ElapsedMs}ms",
                    result.Count,
                    pageNumber,
                    pageSize,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetAllAsync FAILED: PageNumber={PageNumber}, PageSize={PageSize}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    pageNumber,
                    pageSize,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    // ============================================================================
    // TEMPLATE METHOD 3: AddAsync
    // ============================================================================
    public async Task<{Entity}> AddAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "AddAsync START: EntityId={EntityId}",
                entity.Id);

            try
            {
                await _dbSet.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "AddAsync COMPLETE: EntityId={EntityId}, Duration={ElapsedMs}ms",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds);

                return entity;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "AddAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // ============================================================================
    // TEMPLATE METHOD 4: UpdateAsync
    // ============================================================================
    public async Task UpdateAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        using (LogContext.PushProperty("EntityId", entity.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "UpdateAsync START: EntityId={EntityId}",
                entity.Id);

            try
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "UpdateAsync COMPLETE: EntityId={EntityId}, Duration={ElapsedMs}ms",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "UpdateAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    entity.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // ============================================================================
    // TEMPLATE METHOD 5: DeleteAsync
    // ============================================================================
    public async Task DeleteAsync({PrimaryKey} id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Delete"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "DeleteAsync START: EntityId={EntityId}",
                id);

            try
            {
                var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
                if (entity == null)
                {
                    _logger.Warning(
                        "DeleteAsync: Entity not found for deletion, EntityId={EntityId}",
                        id);
                    return;
                }

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "DeleteAsync COMPLETE: EntityId={EntityId}, Duration={ElapsedMs}ms",
                    id,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "DeleteAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // ============================================================================
    // TEMPLATE METHOD 6: Custom Query Method
    // ============================================================================
    public async Task<IReadOnlyList<{Entity}>> GetByCustomCriteriaAsync(
        string criteriaValue,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCustomCriteria"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        using (LogContext.PushProperty("CriteriaValue", criteriaValue))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "GetByCustomCriteriaAsync START: CriteriaValue={CriteriaValue}",
                criteriaValue);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .Where(e => e.SomeProperty == criteriaValue) // Replace with actual criteria
                    .OrderBy(e => e.Id) // Replace with appropriate ordering
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetByCustomCriteriaAsync COMPLETE: CriteriaValue={CriteriaValue}, Count={Count}, Duration={ElapsedMs}ms",
                    criteriaValue,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetByCustomCriteriaAsync FAILED: CriteriaValue={CriteriaValue}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    criteriaValue,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // ============================================================================
    // PERFORMANCE METRICS (Optional - Phase 2 Step 6)
    // ============================================================================
    // Add after implementing PerformanceMetrics.cs:
    //
    // stopwatch.Stop();
    // PerformanceMetrics.RepositoryQueryDuration.Record(
    //     stopwatch.ElapsedMilliseconds,
    //     new KeyValuePair<string, object?>("entity", "{Entity}"),
    //     new KeyValuePair<string, object?>("operation", "GetById"));
    // PerformanceMetrics.RepositoryQueryCount.Add(1);
}
