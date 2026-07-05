using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Enums;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.SPLIT_PER_ENTITY.Repositories;

/// <summary>
/// Phase 7A: Repository implementation for WhatsAppTemplate entities.
/// Follows existing codebase patterns with structured logging and observability.
/// </summary>
public class WhatsAppTemplateRepository : Repository<WhatsAppTemplate>, IWhatsAppTemplateRepository
{
    private readonly ILogger<WhatsAppTemplateRepository> _repoLogger;

    public WhatsAppTemplateRepository(
        AppDbContext context,
        ILogger<WhatsAppTemplateRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<WhatsAppTemplate?> GetByNameAsync(string templateName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            _repoLogger.LogWarning("GetByNameAsync called with null/empty templateName");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByName"))
        using (LogContext.PushProperty("EntityType", "WhatsAppTemplate"))
        using (LogContext.PushProperty("TemplateName", templateName))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetByNameAsync START: TemplateName={TemplateName}", templateName);

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(t => t.TemplateName == templateName, ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetByNameAsync COMPLETE: TemplateName={TemplateName}, Found={Found}, Duration={ElapsedMs}ms",
                    templateName, result != null, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetByNameAsync FAILED: TemplateName={TemplateName}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    templateName, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<WhatsAppTemplate>> GetApprovedTemplatesAsync(CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetApprovedTemplates"))
        using (LogContext.PushProperty("EntityType", "WhatsAppTemplate"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetApprovedTemplatesAsync START");

            try
            {
                var result = await _dbSet
                    .Where(t => t.Status == WhatsAppTemplateStatus.Approved)
                    .OrderBy(t => t.TemplateName)
                    .AsNoTracking()
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetApprovedTemplatesAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetApprovedTemplatesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<WhatsAppTemplate>> GetAllTemplatesAsync(CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllTemplates"))
        using (LogContext.PushProperty("EntityType", "WhatsAppTemplate"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetAllTemplatesAsync START");

            try
            {
                var result = await _dbSet
                    .OrderBy(t => t.TemplateName)
                    .AsNoTracking()
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetAllTemplatesAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetAllTemplatesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }
}
