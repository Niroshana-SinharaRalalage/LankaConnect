using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Support;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.89: Repository implementation for AdminAuditLog.
/// </summary>
public class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<AdminAuditLog> _dbSet;
    private readonly ILogger<AdminAuditLogRepository> _logger;

    public AdminAuditLogRepository(AppDbContext context, ILogger<AdminAuditLogRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<AdminAuditLog>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddAsync(AdminAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "AdminAuditLog"))
        using (LogContext.PushProperty("EntityId", auditLog.Id))
        {
            _logger.LogInformation(
                "Adding AdminAuditLog: LogId={LogId}, AdminUserId={AdminUserId}, Action={Action}, TargetUserId={TargetUserId}",
                auditLog.Id, auditLog.AdminUserId, auditLog.Action, auditLog.TargetUserId);

            await _dbSet.AddAsync(auditLog, cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<AdminAuditLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? adminUserFilter = null,
        string? actionFilter = null,
        Guid? targetUserFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPaged"))
        using (LogContext.PushProperty("EntityType", "AdminAuditLog"))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "GetPagedAsync START: Page={Page}, PageSize={PageSize}, AdminFilter={AdminFilter}, ActionFilter={ActionFilter}",
                page, pageSize, adminUserFilter, actionFilter);

            try
            {
                var query = _dbSet.AsNoTracking();

                // Apply filters
                if (adminUserFilter.HasValue)
                    query = query.Where(a => a.AdminUserId == adminUserFilter.Value);

                if (!string.IsNullOrWhiteSpace(actionFilter))
                    query = query.Where(a => a.Action == actionFilter.ToUpperInvariant());

                if (targetUserFilter.HasValue)
                    query = query.Where(a => a.TargetUserId == targetUserFilter.Value);

                if (fromDate.HasValue)
                    query = query.Where(a => a.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(a => a.CreatedAt <= toDate.Value);

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Get paged items ordered by created date descending (newest first)
                var items = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetPagedAsync COMPLETE: Page={Page}, ItemCount={ItemCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    page, items.Count, totalCount, stopwatch.ElapsedMilliseconds);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetPagedAsync FAILED: Page={Page}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    page, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<AdminAuditLog>> GetByTargetUserAsync(
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByTargetUser"))
        using (LogContext.PushProperty("EntityType", "AdminAuditLog"))
        using (LogContext.PushProperty("TargetUserId", targetUserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetByTargetUserAsync START: TargetUserId={TargetUserId}", targetUserId);

            try
            {
                var items = await _dbSet
                    .AsNoTracking()
                    .Where(a => a.TargetUserId == targetUserId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByTargetUserAsync COMPLETE: TargetUserId={TargetUserId}, ItemCount={ItemCount}, Duration={ElapsedMs}ms",
                    targetUserId, items.Count, stopwatch.ElapsedMilliseconds);

                return items;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByTargetUserAsync FAILED: TargetUserId={TargetUserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    targetUserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.CommitAsync(cancellationToken);
    }
}
