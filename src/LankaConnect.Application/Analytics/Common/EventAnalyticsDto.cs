namespace LankaConnect.Application.Analytics.Common;

/// <summary>
/// DTO for EventAnalytics data
/// </summary>
public class EventAnalyticsDto
{
    public Guid EventId { get; init; }
    public int TotalViews { get; init; }
    public int UniqueViewers { get; init; }
    public int RegistrationCount { get; init; }
    public decimal ConversionRate { get; init; }
    public DateTime? LastViewedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for Organizer Dashboard data
/// </summary>
public class OrganizerDashboardDto
{
    public Guid OrganizerId { get; init; }
    public int TotalEvents { get; init; }
    public int TotalViews { get; init; }
    public int TotalUniqueViewers { get; init; }
    public int TotalRegistrations { get; init; }
    public decimal AverageConversionRate { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public List<EventAnalyticsSummaryDto> TopEvents { get; init; } = new();
    public List<EventAnalyticsSummaryDto> UpcomingEvents { get; init; } = new();
}

/// <summary>
/// Summary data for individual events in dashboard
/// </summary>
public class EventAnalyticsSummaryDto
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public int Views { get; init; }
    public int Registrations { get; init; }
    public decimal ConversionRate { get; init; }
}
