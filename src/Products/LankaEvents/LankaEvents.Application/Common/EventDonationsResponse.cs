namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Response containing donation list and summary for an event.
/// </summary>
public class EventDonationsResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = null!;
    public List<DonationDto> Donations { get; init; } = new();
    public DonationSummaryDto Summary { get; init; } = null!;
}
