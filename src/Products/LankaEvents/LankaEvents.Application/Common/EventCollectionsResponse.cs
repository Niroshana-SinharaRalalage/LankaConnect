namespace LankaConnect.Products.LankaEvents.Application.Common;

public class EventCollectionsResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = null!;
    public List<CollectionDto> Collections { get; init; } = new();
    public CollectionSummaryDto Summary { get; init; } = null!;
}
