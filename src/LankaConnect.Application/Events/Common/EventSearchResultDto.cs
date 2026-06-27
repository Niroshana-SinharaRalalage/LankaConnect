using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// DTO for event search results with relevance ranking
/// Includes all EventDto properties plus search-specific metadata
/// </summary>
public class EventSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EventLocation? Location { get; set; }
    public EventCategory Category { get; set; }
    public EventStatus Status { get; set; }
    public Guid OrganizerId { get; set; }
    public string? OrganizerName { get; set; }
    public int? Capacity { get; set; }
    public int CurrentRegistrations { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Images and Videos
    public List<EventImageDto> Images { get; set; } = new();
    public List<EventVideoDto> Videos { get; set; } = new();

    /// <summary>
    /// Search relevance score (0.0 to 1.0)
    /// Higher scores indicate better matches to the search term
    /// Calculated using PostgreSQL ts_rank function
    /// </summary>
    public decimal SearchRelevance { get; set; }
}
