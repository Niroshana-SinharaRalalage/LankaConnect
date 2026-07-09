namespace LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // 4C.h prereq: promoted from LankaEvents.Application (2026-07-09 Day 4).

/// <summary>
/// Phase 6A.61: DTO for event notification history display in Communication tab
/// </summary>
public class EventNotificationHistoryDto
{
    public Guid Id { get; set; }
    public DateTime SentAt { get; set; }
    public string SentByUserName { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
}
