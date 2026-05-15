namespace LankaConnect.Application.Events.Common;

public record SponsorConfigurationDto
{
    public bool IsEnabled { get; init; }
    public bool AcceptMoneySponsors { get; init; }
    public bool AcceptItemSponsors { get; init; }
    public decimal? MinSponsorAmount { get; init; }
    public string? SponsorMessage { get; init; }
    public bool ShowSponsorList { get; init; }
    // Phase 6A.145 — opt-in threshold for per-sponsor image uploads. Null = feature OFF.
    // When set, sponsors whose money amount (or item EstimatedValue) meets this threshold
    // can attach an image displayed on the event details page.
    public decimal? MinAmountForSponsorImage { get; init; }
}
