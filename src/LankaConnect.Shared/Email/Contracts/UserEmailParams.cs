namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87: Base parameter contract for user-related email fields.
///
/// Used by all email templates that need recipient/user information.
/// Provides strongly-typed access to user data with validation.
///
/// Common parameters:
/// - UserId: Unique identifier for the user
/// - UserName: Display name (e.g., "John Doe")
/// - UserEmail: Email address for the recipient
///
/// Templates using these parameters:
/// - All templates (every email has a recipient)
/// - Registration confirmations
/// - Ticket emails
/// - Reminder emails
/// </summary>
public class UserEmailParams
{
    /// <summary>
    /// Unique identifier for the user receiving the email.
    /// Used for tracking, logging, and database correlation.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's display name (e.g., "John Doe").
    /// Used in email greetings and personalization.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// This is the recipient email for the email send operation.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Converts to dictionary for backward compatibility with existing email system.
    /// </summary>
    /// <returns>Dictionary with all user parameters</returns>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "UserId", UserId.ToString() },
            { "UserName", UserName },
            { "UserEmail", UserEmail }
        };
    }

    /// <summary>
    /// Validates that all required user parameters are provided.
    /// </summary>
    /// <param name="errors">List of validation errors if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (UserId == Guid.Empty)
            errors.Add("UserId is required");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        return errors.Count == 0;
    }
}
