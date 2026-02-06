namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.86: Base interface for all email parameter contracts.
/// Provides type-safe parameter passing with validation for email templates.
///
/// Design Goals:
/// - Replace Dictionary&lt;string, object&gt; with strongly-typed contracts
/// - Enable compile-time parameter verification
/// - Support backward compatibility via ToDictionary()
/// - Facilitate modularization (shared across Events/Marketplace/Forums/Business modules)
/// </summary>
public interface IEmailParameters
{
    /// <summary>
    /// The name of the email template to use (e.g., "template-event-reminder")
    /// </summary>
    string TemplateName { get; }

    /// <summary>
    /// The recipient's email address
    /// </summary>
    string RecipientEmail { get; }

    /// <summary>
    /// The recipient's display name (e.g., "John Doe")
    /// </summary>
    string RecipientName { get; }

    /// <summary>
    /// Converts the strongly-typed parameters to a Dictionary for backward compatibility
    /// with existing IEmailService.SendEmailAsync(Dictionary&lt;string, object&gt;) signature.
    ///
    /// This enables gradual migration:
    /// - New code uses typed parameters: emailService.SendEmailAsync(typedParams)
    /// - Old code continues using dictionaries: emailService.SendEmailAsync(dict)
    /// - Both call the same underlying service
    /// </summary>
    /// <returns>Dictionary with all parameters for template rendering</returns>
    Dictionary<string, object> ToDictionary();

    /// <summary>
    /// Validates that all required parameters are provided and meet business rules.
    ///
    /// Validation includes:
    /// - Required fields are not null/empty
    /// - Email addresses are valid format
    /// - Dates are in correct ranges
    /// - Template-specific business rules
    /// </summary>
    /// <param name="errors">List of validation error messages if validation fails</param>
    /// <returns>True if all parameters are valid, false otherwise</returns>
    bool Validate(out List<string> errors);
}
