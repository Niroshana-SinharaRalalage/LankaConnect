using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.ValueObjects;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailTemplate entities with specialized template operations
/// Follows TDD principles and integrates Result pattern for error handling
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    private readonly ILogger<EmailTemplateRepository> _repoLogger;

    public EmailTemplateRepository(
        AppDbContext context,
        ILogger<EmailTemplateRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<List<EmailTemplate>> GetTemplatesAsync(
        EmailTemplateCategory? category = null,
        EmailType? emailType = null,
        bool? isActive = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTemplates"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("Category", category))
        using (LogContext.PushProperty("EmailType", emailType))
        using (LogContext.PushProperty("IsActive", isActive))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        using (LogContext.PushProperty("PageNumber", pageNumber))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTemplatesAsync START: Category={Category}, EmailType={EmailType}, IsActive={IsActive}, SearchTerm={SearchTerm}, Page={PageNumber}, PageSize={PageSize}",
                category, emailType, isActive, searchTerm, pageNumber, pageSize);

            try
            {
                var query = _dbSet.AsNoTracking();

                // Apply filters
                if (category != null)
                    query = query.Where(t => t.Category.Value == category.Value);

                if (emailType.HasValue)
                    query = query.Where(t => t.Type == emailType.Value);

                if (isActive.HasValue)
                    query = query.Where(t => t.IsActive == isActive.Value);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(t => t.Name.Contains(searchTerm) ||
                                           t.Description.Contains(searchTerm));
                }

                // Apply pagination
                var result = await query
                    .OrderBy(t => t.Category.Value)
                    .ThenBy(t => t.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTemplatesAsync COMPLETE: Category={Category}, EmailType={EmailType}, IsActive={IsActive}, Count={Count}, Duration={ElapsedMs}ms",
                    category,
                    emailType,
                    isActive,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTemplatesAsync FAILED: Category={Category}, EmailType={EmailType}, Page={PageNumber}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category,
                    emailType,
                    pageNumber,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetTemplatesCountAsync(
        EmailTemplateCategory? category = null,
        EmailType? emailType = null,
        bool? isActive = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTemplatesCount"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("Category", category))
        using (LogContext.PushProperty("EmailType", emailType))
        using (LogContext.PushProperty("IsActive", isActive))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTemplatesCountAsync START: Category={Category}, EmailType={EmailType}, IsActive={IsActive}, SearchTerm={SearchTerm}",
                category, emailType, isActive, searchTerm);

            try
            {
                var query = _dbSet.AsNoTracking();

                // Apply same filters as GetTemplatesAsync
                if (category != null)
                    query = query.Where(t => t.Category.Value == category.Value);

                if (emailType.HasValue)
                    query = query.Where(t => t.Type == emailType.Value);

                if (isActive.HasValue)
                    query = query.Where(t => t.IsActive == isActive.Value);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(t => t.Name.Contains(searchTerm) ||
                                           t.Description.Contains(searchTerm));
                }

                var count = await query.CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTemplatesCountAsync COMPLETE: Category={Category}, EmailType={EmailType}, Count={Count}, Duration={ElapsedMs}ms",
                    category,
                    emailType,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTemplatesCountAsync FAILED: Category={Category}, EmailType={EmailType}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category,
                    emailType,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Dictionary<EmailTemplateCategory, int>> GetCategoryCountsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCategoryCounts"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("IsActive", isActive))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetCategoryCountsAsync START: IsActive={IsActive}", isActive);

            try
            {
                var query = _dbSet.AsNoTracking();

                if (isActive.HasValue)
                    query = query.Where(t => t.IsActive == isActive.Value);

                var result = await query
                    .GroupBy(t => t.Category.Value)
                    .Select(g => new { CategoryValue = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                // Convert to proper EmailTemplateCategory objects
                var categoryDict = new Dictionary<EmailTemplateCategory, int>();
                foreach (var item in result)
                {
                    var categoryResult = EmailTemplateCategory.FromValue(item.CategoryValue);
                    if (categoryResult.IsSuccess)
                    {
                        categoryDict[categoryResult.Value] = item.Count;
                    }
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetCategoryCountsAsync COMPLETE: IsActive={IsActive}, CategoryCount={CategoryCount}, Duration={ElapsedMs}ms",
                    isActive,
                    categoryDict.Count,
                    stopwatch.ElapsedMilliseconds);

                return categoryDict;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetCategoryCountsAsync FAILED: IsActive={IsActive}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    isActive,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _repoLogger.LogWarning("GetByNameAsync called with null/empty name");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByName"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("TemplateName", name))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByNameAsync START: TemplateName={TemplateName}", name);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

                stopwatch.Stop();

                if (result != null)
                {
                    _repoLogger.LogInformation(
                        "GetByNameAsync COMPLETE: TemplateName={TemplateName}, TemplateId={TemplateId}, IsActive={IsActive}, Category={Category}, Duration={ElapsedMs}ms",
                        name,
                        result.Id,
                        result.IsActive,
                        result.Category.Value,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _repoLogger.LogInformation(
                        "GetByNameAsync COMPLETE: TemplateName={TemplateName}, Found=false, Duration={ElapsedMs}ms",
                        name,
                        stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByNameAsync FAILED: TemplateName={TemplateName}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    name,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<List<EmailTemplate>> GetByEmailTypeAsync(
        EmailType emailType,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEmailType"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("EmailType", emailType))
        using (LogContext.PushProperty("IsActive", isActive))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEmailTypeAsync START: EmailType={EmailType}, IsActive={IsActive}", emailType, isActive);

            try
            {
                var query = _dbSet.AsNoTracking().Where(t => t.Type == emailType);

                if (isActive.HasValue)
                    query = query.Where(t => t.IsActive == isActive.Value);

                var result = await query
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEmailTypeAsync COMPLETE: EmailType={EmailType}, IsActive={IsActive}, Count={Count}, Duration={ElapsedMs}ms",
                    emailType,
                    isActive,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEmailTypeAsync FAILED: EmailType={EmailType}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    emailType,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("TemplateId", template.Id))
        using (LogContext.PushProperty("TemplateName", template.Name))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("UpdateAsync START: TemplateId={TemplateId}, TemplateName={TemplateName}", template.Id, template.Name);

            try
            {
                _dbSet.Update(template);
                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "UpdateAsync COMPLETE: TemplateId={TemplateId}, TemplateName={TemplateName}, IsActive={IsActive}, Duration={ElapsedMs}ms",
                    template.Id,
                    template.Name,
                    template.IsActive,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "UpdateAsync FAILED: TemplateId={TemplateId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    template.Id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
