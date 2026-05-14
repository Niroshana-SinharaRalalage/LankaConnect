namespace LankaConnect.Application.Events.Common;

public record SponsorConfigurationDto
{
    public bool IsEnabled { get; init; }
    public bool AcceptMoneySponsors { get; init; }
    public bool AcceptItemSponsors { get; init; }
    public decimal? MinSponsorAmount { get; init; }
    public string? SponsorMessage { get; init; }
    public bool ShowSponsorList { get; init; }
    // Phase 6A.143 — optional sponsor banner image rendered above the form on the
    // event details page. Both fields are either both set or both null.
    public string? SponsorImageUrl { get; init; }
    public string? SponsorImageBlobName { get; init; }
}
