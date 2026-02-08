namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.99: Interface for validating email templates against typed parameter contracts.
/// Used at application startup to detect parameter mismatches early.
/// </summary>
public interface IEmailTemplateValidator
{
    /// <summary>
    /// Validates all email templates in the database against their corresponding
    /// TypedEmailParams classes to detect parameter mismatches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result containing any mismatches found</returns>
    Task<EmailTemplateValidationResult> ValidateAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a specific email template against its expected parameters.
    /// </summary>
    /// <param name="templateName">The template name to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result for the specific template</returns>
    Task<EmailTemplateValidationResult> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of email template validation.
/// </summary>
public class EmailTemplateValidationResult
{
    /// <summary>
    /// Whether all validations passed.
    /// </summary>
    public bool IsValid => !Errors.Any() && !Warnings.Any();

    /// <summary>
    /// Whether validation passed with only warnings (no errors).
    /// </summary>
    public bool HasWarningsOnly => !Errors.Any() && Warnings.Any();

    /// <summary>
    /// Critical errors that indicate template/parameter mismatches.
    /// </summary>
    public List<EmailTemplateValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Non-critical warnings (e.g., extra parameters in code but not in template).
    /// </summary>
    public List<EmailTemplateValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Templates that were validated successfully.
    /// </summary>
    public List<string> ValidatedTemplates { get; set; } = new();

    /// <summary>
    /// Total number of templates checked.
    /// </summary>
    public int TotalTemplatesChecked { get; set; }
}

/// <summary>
/// Represents a critical validation error.
/// </summary>
public class EmailTemplateValidationError
{
    /// <summary>
    /// Template name.
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Parameters that are missing from the template but expected by the code.
    /// </summary>
    public List<string> MissingInTemplate { get; set; } = new();

    /// <summary>
    /// Parameters that are in the template but not provided by the code.
    /// </summary>
    public List<string> MissingInCode { get; set; } = new();
}

/// <summary>
/// Represents a non-critical validation warning.
/// </summary>
public class EmailTemplateValidationWarning
{
    /// <summary>
    /// Template name.
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
