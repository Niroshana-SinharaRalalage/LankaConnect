namespace LankaConnect.Application.Events.Common;

public record CollectionConfigurationDto
{
    public bool IsEnabled { get; init; }
    public decimal? GoalAmount { get; init; }
    public bool ShowProgress { get; init; }
    public List<decimal> SuggestedAmounts { get; init; } = new();
    public bool AllowCustomAmount { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? CollectionMessage { get; init; }
    public bool ShowContributorCount { get; init; }
}
