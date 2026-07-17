using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Infrastructure.Repositories;

/// <summary>
/// Hand-rolled <see cref="IFormRepository"/> per ADR-010. W4.3 capability
/// extraction — injects <see cref="FormsDbContext"/> directly.
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
public class FormRepository : IFormRepository
{
    private readonly FormsDbContext _context;
    private readonly DbSet<Form> _dbSet;
    private readonly ILogger<FormRepository> _repoLogger;

    public FormRepository(FormsDbContext context, ILogger<FormRepository> logger)
    {
        _context = context;
        _dbSet = context.Set<Form>();
        _repoLogger = logger;
    }

    public Task<Form?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbSet.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Form?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIdWithQuestions"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("FormId", formId))
        {
            return await _dbSet
                .Include(f => f.Questions.OrderBy(q => q.SortOrder))
                .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken);
        }
    }

    public async Task<List<Form>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            return await _dbSet
                .AsNoTracking()
                .Include(f => f.Questions.OrderBy(q => q.SortOrder))
                .Where(f => f.EventId == eventId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }

    public async Task AddAsync(Form entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.d: SaveChangesAsync deleted at the repo boundary. Wave 8.5.h:
        // caller MUST invoke direct _formsContext.SaveChangesAsync(ct) per
        // Tech Lead D-01 (retire of IMultiContextUnitOfWork).
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(Form entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.d: SaveChangesAsync deleted. Same caller contract as AddAsync.
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Form entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.d: SaveChangesAsync deleted. Same caller contract as AddAsync.
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
