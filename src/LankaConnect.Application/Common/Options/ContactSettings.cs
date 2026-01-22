namespace LankaConnect.Application.Common.Options;

/// <summary>
/// Configuration settings for the Contact Us feature.
/// Recipient email is stored securely in configuration and never exposed to frontend clients.
/// Phase 6A.76: Contact Us Feature
/// </summary>
public class ContactSettings
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json
    /// </summary>
    public const string SectionName = "ContactSettings";

    /// <summary>
    /// Email address to receive contact form submissions.
    /// Stored in server configuration, never exposed to clients.
    /// </summary>
    public string RecipientEmail { get; set; } = "niroshanaks@gmail.com";

    /// <summary>
    /// Display name for the recipient in email headers.
    /// </summary>
    public string RecipientName { get; set; } = "LankaConnect Support";

    /// <summary>
    /// Prefix added to email subjects for easy filtering and identification.
    /// </summary>
    public string EmailSubjectPrefix { get; set; } = "[LankaConnect Contact]";

    /// <summary>
    /// Maximum allowed message length in characters.
    /// </summary>
    public int MaxMessageLength { get; set; } = 5000;

    /// <summary>
    /// Rate limit: maximum submissions per IP address per hour.
    /// </summary>
    public int MaxSubmissionsPerHour { get; set; } = 5;
}
