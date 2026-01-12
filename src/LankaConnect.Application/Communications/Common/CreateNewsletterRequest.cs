namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Request model for creating a newsletter
/// </summary>
public class CreateNewsletterRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> EmailGroupIds { get; set; } = new();
    public bool IncludeNewsletterSubscribers { get; set; }
    public Guid? EventId { get; set; }

    // Phase 6A.74 Enhancement 1: Location Targeting for Non-Event Newsletters
    public List<Guid>? MetroAreaIds { get; set; }
    public bool TargetAllLocations { get; set; }
}
