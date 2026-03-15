namespace LankaConnect.Application.Events.Common;

public class EventSponsorsResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = null!;
    public List<SponsorDto> Sponsors { get; init; } = new();
    public SponsorSummaryDto Summary { get; init; } = null!;
}
