namespace LankaConnect.BuildingBlocks.Application.Common.Options;

/// <summary>
/// Configuration settings for event management.
/// Phase 6A.133: Multi-organizer feature — configurable co-organizer limits.
/// </summary>
public class EventSettings
{
    public const string SectionName = "EventSettings";

    /// <summary>
    /// Maximum number of co-organizers that can be linked to an event.
    /// Default: 10. Primary organizer is NOT counted toward this limit.
    /// </summary>
    public int MaxCoOrganizersPerEvent { get; init; } = 10;

    /// <summary>
    /// Validates the event settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when settings are invalid.</exception>
    public void Validate()
    {
        if (MaxCoOrganizersPerEvent < 1 || MaxCoOrganizersPerEvent > 50)
        {
            throw new InvalidOperationException(
                $"MaxCoOrganizersPerEvent must be between 1 and 50 (got {MaxCoOrganizersPerEvent})");
        }
    }
}
