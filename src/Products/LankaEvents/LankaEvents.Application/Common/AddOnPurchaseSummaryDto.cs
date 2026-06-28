namespace LankaConnect.Products.LankaEvents.Application.Common;

public record AddOnPurchaseSummaryDto
{
    public int TotalPurchases { get; init; }
    public int CompletedPurchases { get; init; }
    public decimal TotalRevenue { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal TotalStripeFees { get; init; }
    public decimal TotalPlatformCommission { get; init; }
    public decimal TotalOrganizerPayout { get; init; }
    public int TotalItemsSold { get; init; }
}
