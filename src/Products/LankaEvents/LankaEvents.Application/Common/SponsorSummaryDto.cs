namespace LankaConnect.Products.LankaEvents.Application.Common;

public record SponsorSummaryDto
{
    public int TotalSponsors { get; init; }
    public int CompletedMoneySponsors { get; init; }
    public int RecordedItemSponsors { get; init; }
    public decimal TotalMoneyAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal TotalStripeFees { get; init; }
    public decimal TotalPlatformCommission { get; init; }
    public decimal TotalOrganizerPayout { get; init; }
    public int ItemSponsorCount { get; init; }
}
