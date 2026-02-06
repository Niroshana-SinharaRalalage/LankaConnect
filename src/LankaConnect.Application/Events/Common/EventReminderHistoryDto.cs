namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.76: DTO for event reminder history display in Communication tab
/// Aggregates reminder sends by type and date for display purposes
/// </summary>
public class EventReminderHistoryDto
{
    /// <summary>
    /// Type of reminder (1day, 2day, 7day, custom)
    /// </summary>
    public string ReminderType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the reminder type
    /// </summary>
    public string ReminderTypeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Date when reminders of this type were sent (grouped by date)
    /// </summary>
    public DateTime SentDate { get; set; }

    /// <summary>
    /// Number of recipients who received this reminder type on this date
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Helper to get user-friendly reminder type label
    /// </summary>
    public static string GetReminderTypeLabel(string reminderType)
    {
        return reminderType switch
        {
            "1day" => "Tomorrow (1 day)",
            "2day" => "In 2 days",
            "7day" => "In 1 week",
            "custom" => "Custom reminder",
            _ => reminderType
        };
    }
}
