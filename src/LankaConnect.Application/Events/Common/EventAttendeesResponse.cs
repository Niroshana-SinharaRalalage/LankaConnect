namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Response containing all attendees for an event with summary statistics.
/// </summary>
public class EventAttendeesResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public List<EventAttendeeDto> Attendees { get; init; } = new();
    public int TotalRegistrations { get; init; }
    public int TotalAttendees { get; init; }
    public decimal? TotalRevenue { get; init; }
}
