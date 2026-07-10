namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

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

/// <summary>
/// Optional interface for email parameters that support List-Unsubscribe headers.
/// Marketing/subscription emails implement this; transactional emails do NOT.
/// When implemented, the email service adds RFC 2369 List-Unsubscribe and
/// RFC 8058 List-Unsubscribe-Post headers to enable one-click unsubscribe in Gmail/Yahoo.
/// </summary>
public interface IUnsubscribeableEmail
{
    /// <summary>
    /// Per-recipient unsubscribe URL containing their unique token.
    /// Example: https://api.lankaconnect.app/api/newsletter/unsubscribe?token=abc123
    /// Returns null if no unsubscribe URL is available for this recipient.
    /// </summary>
    string? UnsubscribeUrl { get; }
}
