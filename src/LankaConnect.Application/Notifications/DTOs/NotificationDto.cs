namespace LankaConnect.Application.Notifications.DTOs;

/// <summary>
/// DTO for notification
/// Phase 6A.6: Notification System
/// </summary>
public record NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
}
