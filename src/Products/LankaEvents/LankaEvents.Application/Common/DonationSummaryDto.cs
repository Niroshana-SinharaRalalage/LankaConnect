namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Summary statistics for donations on an event.
/// </summary>
public record DonationSummaryDto
{
    public int TotalDonations { get; init; }
    public int CompletedDonations { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AverageDonation { get; init; }
    public string Currency { get; init; } = "USD";

    // Revenue breakdown totals
    public decimal TotalStripeFees { get; init; }
    public decimal TotalPlatformCommission { get; init; }
    public decimal TotalOrganizerPayout { get; init; }
}
