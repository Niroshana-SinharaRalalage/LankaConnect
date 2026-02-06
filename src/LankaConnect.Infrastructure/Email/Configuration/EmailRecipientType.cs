namespace LankaConnect.Infrastructure.Email.Configuration;

/// <summary>
/// Defines recipient types for email distribution.
/// Used primarily in Phase 6A.50 - Organizer Manual Email Sending.
/// Determines which groups of users receive emails.
/// Phase 0 - Email System Configuration Infrastructure
/// </summary>
public enum EmailRecipientType
{
    /// <summary>
    /// All attendees (both free and paid registrations).
    /// Includes users who have confirmed registration.
    /// </summary>
    AllAttendees = 1,

    /// <summary>
    /// Only free event registrations.
    /// Users who registered without payment.
    /// </summary>
    FreeAttendees = 2,

    /// <summary>
    /// Only paid event registrations.
    /// Users who completed payment for registration.
    /// </summary>
    PaidAttendees = 3,

    /// <summary>
    /// All signups (commitment-based attendance).
    /// Users who clicked "I Will Attend" for signup items.
    /// </summary>
    AllSignups = 4,

    /// <summary>
    /// Only signups with confirmed commitment.
    /// Users who confirmed "I Will Attend" (IsAttending = true).
    /// </summary>
    ConfirmedSignups = 5,

    /// <summary>
    /// Only signups with pending/tentative commitment.
    /// Users who have not yet confirmed commitment.
    /// </summary>
    PendingSignups = 6,

    /// <summary>
    /// Both attendees and signups combined.
    /// Maximum reach for event communication.
    /// </summary>
    AttendeesAndSignups = 7,

    /// <summary>
    /// Custom email list (future feature).
    /// Manually selected recipients from email groups.
    /// Phase 6A.25 - Email Groups Management integration.
    /// </summary>
    CustomList = 8
}

/// <summary>
/// Extension methods for EmailRecipientType.
/// Provides display names and descriptions for UI.
/// </summary>
public static class EmailRecipientTypeExtensions
{
    /// <summary>
    /// Gets human-readable display name for recipient type.
    /// Used in UI dropdowns and selection lists.
    /// </summary>
    public static string GetDisplayName(this EmailRecipientType recipientType)
    {
        return recipientType switch
        {
            EmailRecipientType.AllAttendees => "All Attendees",
            EmailRecipientType.FreeAttendees => "Free Event Attendees",
            EmailRecipientType.PaidAttendees => "Paid Event Attendees",
            EmailRecipientType.AllSignups => "All Signups",
            EmailRecipientType.ConfirmedSignups => "Confirmed Signups (I Will Attend)",
            EmailRecipientType.PendingSignups => "Pending Signups",
            EmailRecipientType.AttendeesAndSignups => "All Attendees & Signups",
            EmailRecipientType.CustomList => "Custom Email List",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets description explaining what this recipient type includes.
    /// Used for tooltips and help text in UI.
    /// </summary>
    public static string GetDescription(this EmailRecipientType recipientType)
    {
        return recipientType switch
        {
            EmailRecipientType.AllAttendees =>
                "Send to all users who registered (both free and paid)",
            EmailRecipientType.FreeAttendees =>
                "Send only to users with free registrations",
            EmailRecipientType.PaidAttendees =>
                "Send only to users who paid for registration",
            EmailRecipientType.AllSignups =>
                "Send to all users who signed up (commitment-based)",
            EmailRecipientType.ConfirmedSignups =>
                "Send only to users who confirmed 'I Will Attend'",
            EmailRecipientType.PendingSignups =>
                "Send only to users with pending commitment",
            EmailRecipientType.AttendeesAndSignups =>
                "Send to everyone (registrations and signups)",
            EmailRecipientType.CustomList =>
                "Send to manually selected email list",
            _ => "Unknown recipient type"
        };
    }

    /// <summary>
    /// Checks if recipient type includes attendees (registrations).
    /// </summary>
    public static bool IncludesAttendees(this EmailRecipientType recipientType)
    {
        return recipientType switch
        {
            EmailRecipientType.AllAttendees => true,
            EmailRecipientType.FreeAttendees => true,
            EmailRecipientType.PaidAttendees => true,
            EmailRecipientType.AttendeesAndSignups => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if recipient type includes signups.
    /// </summary>
    public static bool IncludesSignups(this EmailRecipientType recipientType)
    {
        return recipientType switch
        {
            EmailRecipientType.AllSignups => true,
            EmailRecipientType.ConfirmedSignups => true,
            EmailRecipientType.PendingSignups => true,
            EmailRecipientType.AttendeesAndSignups => true,
            _ => false
        };
    }
}
