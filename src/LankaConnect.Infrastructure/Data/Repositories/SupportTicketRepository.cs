using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Support.Enums;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.89: Repository implementation for SupportTicket aggregate.
/// </summary>
public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<SupportTicket> _dbSet;
    private readonly ILogger<SupportTicketRepository> _logger;

    public SupportTicketRepository(AppDbContext context, ILogger<SupportTicketRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<SupportTicket>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetByIdAsync START: TicketId={TicketId}", id);

            try
            {
                // Include replies and notes for full ticket details
                var ticket = await _dbSet
                    .AsSplitQuery()
                    .Include(t => t.Replies)
                    .Include(t => t.Notes)
                    .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByIdAsync COMPLETE: TicketId={TicketId}, Found={Found}, ReferenceId={ReferenceId}, Duration={ElapsedMs}ms",
                    id,
                    ticket != null,
                    ticket?.ReferenceId,
                    stopwatch.ElapsedMilliseconds);

                return ticket;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByIdAsync FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<SupportTicket?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByReferenceId"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("ReferenceId", referenceId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetByReferenceIdAsync START: ReferenceId={ReferenceId}", referenceId);

            try
            {
                var ticket = await _dbSet
                    .AsSplitQuery()
                    .Include(t => t.Replies)
                    .Include(t => t.Notes)
                    .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByReferenceIdAsync COMPLETE: ReferenceId={ReferenceId}, Found={Found}, TicketId={TicketId}, Duration={ElapsedMs}ms",
                    referenceId,
                    ticket != null,
                    ticket?.Id,
                    stopwatch.ElapsedMilliseconds);

                return ticket;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByReferenceIdAsync FAILED: ReferenceId={ReferenceId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    referenceId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        SupportTicketStatus? statusFilter = null,
        SupportTicketPriority? priorityFilter = null,
        Guid? assignedToFilter = null,
        bool? unassignedOnly = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPaged"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "GetPagedAsync START: Page={Page}, PageSize={PageSize}, StatusFilter={StatusFilter}, PriorityFilter={PriorityFilter}, UnassignedOnly={UnassignedOnly}",
                page, pageSize, statusFilter, priorityFilter, unassignedOnly);

            try
            {
                IQueryable<SupportTicket> query = _dbSet.AsNoTracking();

                // Apply filters
                if (statusFilter.HasValue)
                    query = query.Where(t => t.Status == statusFilter.Value);

                if (priorityFilter.HasValue)
                    query = query.Where(t => t.Priority == priorityFilter.Value);

                if (assignedToFilter.HasValue)
                    query = query.Where(t => t.AssignedToUserId == assignedToFilter.Value);

                if (unassignedOnly == true)
                    query = query.Where(t => t.AssignedToUserId == null);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLower();
                    query = query.Where(t =>
                        t.Name.ToLower().Contains(term) ||
                        t.Subject.ToLower().Contains(term) ||
                        t.ReferenceId.ToLower().Contains(term) ||
                        t.Email.Value.ToLower().Contains(term));
                }

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Get paged items with replies (for reply count) ordered by created date descending (newest first)
                var items = await query
                    .AsSplitQuery()
                    .Include(t => t.Replies)
                    .OrderByDescending(t => t.CreatedAt)
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

    public async Task<Dictionary<SupportTicketStatus, int>> GetCountsByStatusAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCountsByStatus"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetCountsByStatusAsync START");

            try
            {
                var counts = await _dbSet
                    .AsNoTracking()
                    .GroupBy(t => t.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetCountsByStatusAsync COMPLETE: StatusCount={StatusCount}, Duration={ElapsedMs}ms",
                    counts.Count, stopwatch.ElapsedMilliseconds);

                return counts;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetCountsByStatusAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    public async Task<Dictionary<SupportTicketPriority, int>> GetCountsByPriorityAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCountsByPriority"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetCountsByPriorityAsync START");

            try
            {
                var counts = await _dbSet
                    .AsNoTracking()
                    .GroupBy(t => t.Priority)
                    .Select(g => new { Priority = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Priority, x => x.Count, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetCountsByPriorityAsync COMPLETE: PriorityCount={PriorityCount}, Duration={ElapsedMs}ms",
                    counts.Count, stopwatch.ElapsedMilliseconds);

                return counts;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetCountsByPriorityAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    public async Task<int> GetUnassignedCountAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUnassignedCount"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetUnassignedCountAsync START");

            try
            {
                var count = await _dbSet
                    .AsNoTracking()
                    .CountAsync(t => t.AssignedToUserId == null, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUnassignedCountAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    count, stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUnassignedCountAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("EntityId", ticket.Id))
        {
            _logger.LogInformation(
                "Adding SupportTicket: TicketId={TicketId}, ReferenceId={ReferenceId}, Subject={Subject}",
                ticket.Id, ticket.ReferenceId, ticket.Subject);

            await _dbSet.AddAsync(ticket, cancellationToken);
        }
    }

    public void Update(SupportTicket ticket)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("EntityId", ticket.Id))
        {
            _logger.LogInformation(
                "Updating SupportTicket: TicketId={TicketId}, ReferenceId={ReferenceId}, Status={Status}",
                ticket.Id, ticket.ReferenceId, ticket.Status);

            _dbSet.Update(ticket);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.CommitAsync(cancellationToken);
    }
}
