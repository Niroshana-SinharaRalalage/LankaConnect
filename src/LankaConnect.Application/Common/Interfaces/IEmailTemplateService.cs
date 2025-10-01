using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for managing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders an email template with provided parameters
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="parameters">Template parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing rendered email content</returns>
    Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(string templateName, 
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available email templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of available templates</returns>
    Task<Result<List<EmailTemplateInfo>>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets template metadata and configuration
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing template information</returns>
    Task<Result<EmailTemplateInfo>> GetTemplateInfoAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates template syntax and required parameters
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="parameters">Parameters to validate against template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating validation status</returns>
    Task<Result> ValidateTemplateParametersAsync(string templateName, 
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rendered email template content
/// </summary>
public class RenderedEmailTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
}

/// <summary>
/// Email template information and metadata
/// </summary>
public class EmailTemplateInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredParameters { get; set; } = new();
    public List<string> OptionalParameters { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastModified { get; set; }
}