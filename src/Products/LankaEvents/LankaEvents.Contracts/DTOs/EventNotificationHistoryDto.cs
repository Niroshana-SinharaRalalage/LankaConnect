namespace LankaConnect.Products.LankaEvents.Contracts.DTOs; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt.

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
