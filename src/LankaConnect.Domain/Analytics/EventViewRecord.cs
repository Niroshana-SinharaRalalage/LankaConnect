namespace LankaConnect.Domain.Analytics;

/// <summary>
/// EventViewRecord entity for tracking individual event views
/// Used for detailed tracking and unique viewer calculation
/// </summary>
public class EventViewRecord
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ViewedAt { get; set; }
}
