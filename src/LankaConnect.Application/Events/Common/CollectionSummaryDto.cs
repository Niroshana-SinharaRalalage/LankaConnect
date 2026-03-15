namespace LankaConnect.Application.Events.Common;

public record CollectionSummaryDto
{
    public int TotalCollections { get; init; }
    public int CompletedCollections { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AverageCollection { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal TotalStripeFees { get; init; }
    public decimal TotalPlatformCommission { get; init; }
    public decimal TotalOrganizerPayout { get; init; }
    public decimal? GoalAmount { get; init; }
    public decimal? GoalProgressPercent { get; init; }
    public int ContributorCount { get; init; }
}
