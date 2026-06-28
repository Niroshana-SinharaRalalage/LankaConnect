namespace LankaConnect.Products.LankaEvents.Application.Common;

public class EventAddOnPurchasesResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = null!;
    public List<AddOnDefinitionDto> Definitions { get; init; } = new();
    public List<AddOnPurchaseDto> Purchases { get; init; } = new();
    public AddOnPurchaseSummaryDto Summary { get; init; } = null!;
}
