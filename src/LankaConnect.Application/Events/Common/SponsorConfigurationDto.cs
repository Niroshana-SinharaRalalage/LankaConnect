namespace LankaConnect.Application.Events.Common;

public record SponsorConfigurationDto
{
    public bool IsEnabled { get; init; }
    public bool AcceptMoneySponsors { get; init; }
    public bool AcceptItemSponsors { get; init; }
    public decimal? MinSponsorAmount { get; init; }
    public string? SponsorMessage { get; init; }
    public bool ShowSponsorList { get; init; }
}
