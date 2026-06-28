namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Aggregate DTO holding all financial data for an event.
/// Used by ExportAllFinancials to pass data to export services.
/// </summary>
public class AllFinancialsData
{
    public EventAttendeesResponse Attendees { get; init; } = null!;
    public EventDonationsResponse Donations { get; init; } = null!;
    public EventCollectionsResponse Collections { get; init; } = null!;
    public EventSponsorsResponse Sponsors { get; init; } = null!;
    public EventAddOnPurchasesResponse AddOnPurchases { get; init; } = null!;
}
