using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.ValueObjects;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailTemplate entities with specialized template operations
/// Follows TDD principles and integrates Result pattern for error handling
/// </summary>
public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(AppDbContext context) : base(context)
    {
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
        using (LogContext.PushProperty("Category", category))
        using (LogContext.PushProperty("EmailType", emailType))
        using (LogContext.PushProperty("IsActive", isActive))
        using (LogContext.PushProperty("PageNumber", pageNumber))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            _logger.Debug("Getting email templates with filters - Category: {Category}, EmailType: {EmailType}, Active: {IsActive}, Search: {SearchTerm}",
                category, emailType, isActive, searchTerm);

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

            _logger.Debug("Retrieved {Count} email templates", result.Count);
            return result;
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
            
            _logger.Debug("Templates count: {Count}", count);
            return count;
        }
    }

    public async Task<Dictionary<EmailTemplateCategory, int>> GetCategoryCountsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCategoryCounts"))
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

            _logger.Debug("Category counts: {@CategoryCounts}", categoryDict);
            return categoryDict;
        }
    }

    public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.Warning("[TEMPLATE-LOAD] GetByNameAsync called with null/empty name");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByName"))
        using (LogContext.PushProperty("TemplateName", name))
        {
            _logger.Information("[TEMPLATE-LOAD] Getting template by name: {TemplateName}", name);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

                if (result != null)
                {
                    _logger.Information("[TEMPLATE-LOAD] ✅ Found template {TemplateId} with name {TemplateName}, IsActive: {IsActive}, Category: {Category}",
                        result.Id, name, result.IsActive, result.Category.Value);
                }
                else
                {
                    _logger.Warning("[TEMPLATE-LOAD] ❌ No template found with name {TemplateName}", name);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TEMPLATE-LOAD] ❌ Exception loading template {TemplateName}: {Message}", name, ex.Message);
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
        using (LogContext.PushProperty("EmailType", emailType))
        {
            _logger.Debug("Getting templates by email type: {EmailType}", emailType);

            var query = _dbSet.AsNoTracking().Where(t => t.Type == emailType);

            if (isActive.HasValue)
                query = query.Where(t => t.IsActive == isActive.Value);

            var result = await query
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            _logger.Debug("Found {Count} templates for email type {EmailType}", result.Count, emailType);
            return result;
        }
    }

    public async Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "UpdateTemplate"))
        using (LogContext.PushProperty("TemplateId", template.Id))
        {
            _logger.Debug("Updating email template {TemplateId}", template.Id);

            _dbSet.Update(template);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.Debug("Updated email template {TemplateId}", template.Id);
        }
    }
}