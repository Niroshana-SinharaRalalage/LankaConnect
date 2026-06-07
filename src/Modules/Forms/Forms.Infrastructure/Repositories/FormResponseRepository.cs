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
/// extraction — injects <see cref="FormsDbContext"/>; mutations are self-saving.
/// </summary>
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
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(FormResponse entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(FormResponse entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return _context.SaveChangesAsync(cancellationToken);
    }
}
