using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Repository interface for managing email templates
/// Follows Clean Architecture principles by extending base IRepository
/// </summary>
public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    /// <summary>
    /// Gets email templates with filtering and pagination
    /// </summary>
    /// <param name="category">Optional category filter using domain value object</param>
    /// <param name="emailType">Optional email type filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="searchTerm">Optional search term for name or description</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of email templates</returns>
    Task<List<EmailTemplate>> GetTemplatesAsync(
        EmailTemplateCategory? category = null,
        EmailType? emailType = null,
        bool? isActive = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of email templates matching the filters
    /// </summary>
    /// <param name="category">Optional category filter using domain value object</param>
    /// <param name="emailType">Optional email type filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of matching templates</returns>
    Task<int> GetTemplatesCountAsync(
        EmailTemplateCategory? category = null,
        EmailType? emailType = null,
        bool? isActive = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category counts for active templates using domain value objects
    /// </summary>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of category counts using domain value objects</returns>
    Task<Dictionary<EmailTemplateCategory, int>> GetCategoryCountsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by email type
    /// </summary>
    /// <param name="emailType">The email type to filter by</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of templates for the specified email type</returns>
    Task<List<EmailTemplate>> GetByEmailTypeAsync(
        EmailType emailType,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets email template by name
    /// </summary>
    /// <param name="name">Template name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email template or null if not found</returns>
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    // GetByIdAsync, AddAsync, Update, Remove, DeleteAsync methods are inherited from IRepository<EmailTemplate>
}