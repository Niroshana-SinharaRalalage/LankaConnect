namespace LankaConnect.Application.Common.Configuration;

/// <summary>
/// Settings for automatic email notifications.
/// Phase 6A.82 - Disable Automatic Event Publication Emails
/// Maps to EmailSettings:AutomaticNotifications in appsettings.json
/// </summary>
public sealed class EmailNotificationSettings
{
    public const string SectionName = "EmailSettings:AutomaticNotifications";

    /// <summary>
    /// Whether to send automatic notification emails when an event is published (default: false)
    /// When false, organizers must use the manual "Send Notification" button.
    /// When true, emails are sent automatically when event status changes from Draft to Published.
    /// </summary>
    public bool SendOnEventPublish { get; set; } = false;
}
