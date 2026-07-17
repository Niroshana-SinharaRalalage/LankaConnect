using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Infrastructure.Repositories;

/// <summary>
/// Hand-rolled <see cref="IFormResponseRepository"/> per ADR-010. W4.3 capability
/// extraction — injects <see cref="FormsDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Wave 6.5.d (2026-07-03): retired the W4.0b self-saving pattern. AddAsync +
/// UpdateAsync + DeleteAsync stage on the <see cref="FormsDbContext"/> ChangeTracker;
/// callers commit at the handler edge.
/// </para>
/// <para>
/// Wave 8.5.h (2026-07-17, Tech Lead D-01): callers now use direct
/// <c>_formsContext.SaveChangesAsync(ct)</c> per Consult #25 Q6. Wave 8.5.f
/// <c>DomainEventSaveChangesInterceptor</c> dispatches domain events post-save.
/// The F30a class of production data-loss cannot recur through this repository.
/// </para>
/// </remarks>
public class FormResponseRepository : IFormResponseRepository
{
    private readonly FormsDbContext _context;
    private readonly DbSet<FormResponse> _dbSet;
    private readonly ILogger<FormResponseRepository> _repoLogger;

    public FormResponseRepository(FormsDbContext context, ILogger<FormResponseRepository> logger)
    {
        _context = context;
        _dbSet = context.Set<FormResponse>();
        _repoLogger = logger;
    }

    public Task<FormResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbSet.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<FormResponse?> GetByIdWithAnswersAsync(Guid responseId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIdWithAnswers"))
        using (LogContext.PushProperty("ResponseId", responseId))
        {
            return await _dbSet
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == responseId, cancellationToken);
        }
    }

    public async Task<FormResponse?> GetByAccessTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByAccessTokenHash"))
        {
            return await _dbSet
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.AccessTokenHash == tokenHash, cancellationToken);
        }
    }

    public async Task<FormResponse?> GetByFormAndUserAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByFormAndUser"))
        using (LogContext.PushProperty("FormId", formId))
        using (LogContext.PushProperty("UserId", userId))
        {
            return await _dbSet
                .AsNoTracking()
                .Include(r => r.Answers)
                .Where(r => r.EventFormId == formId && r.RespondentUserId == userId)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    public async Task<FormResponse?> GetByFormAndEmailAsync(Guid formId, string email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByFormAndEmail"))
        using (LogContext.PushProperty("FormId", formId))
        {
            return await _dbSet
                .AsNoTracking()
                .Include(r => r.Answers)
                .Where(r => r.EventFormId == formId && r.RespondentEmail == email)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    public Task<int> GetCountByFormIdAsync(Guid formId, CancellationToken cancellationToken = default) =>
        _dbSet.CountAsync(r => r.EventFormId == formId, cancellationToken);

    public async Task<(IReadOnlyList<FormResponse> Responses, int TotalCount)> GetPaginatedAsync(
        Guid formId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPaginated"))
        using (LogContext.PushProperty("FormId", formId))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(r => r.Answers)
                .Where(r => r.EventFormId == formId);

            var totalCount = await query.CountAsync(cancellationToken);

            var responses = await query
                .OrderByDescending(r => r.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (responses, totalCount);
        }
    }

    public async Task<IReadOnlyList<FormResponse>> GetByEventAndUserAsync(
        Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventAndUser"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("UserId", userId))
        {
            return await _dbSet
                .Include(r => r.Answers)
                .Where(r => r.EventId == eventId && r.RespondentUserId == userId)
                .ToListAsync(cancellationToken);
        }
    }

    public async Task AddAsync(FormResponse entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.d: SaveChangesAsync deleted at the repo boundary. Wave 8.5.h:
        // caller MUST invoke direct _formsContext.SaveChangesAsync(ct) per
        // Tech Lead D-01 (retire of IMultiContextUnitOfWork).
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(FormResponse entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.d: SaveChangesAsync deleted. Same caller contract as AddAsync.
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FormResponse entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.d: SaveChangesAsync deleted. Same caller contract as AddAsync.
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
