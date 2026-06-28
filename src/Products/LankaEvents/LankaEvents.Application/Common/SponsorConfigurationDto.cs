namespace LankaConnect.Products.LankaEvents.Application.Common;

public record SponsorConfigurationDto
{
    public bool IsEnabled { get; init; }
    public bool AcceptMoneySponsors { get; init; }
    public bool AcceptItemSponsors { get; init; }
    public decimal? MinSponsorAmount { get; init; }
    public string? SponsorMessage { get; init; }
    public bool ShowSponsorList { get; init; }

    /// <summary>
    /// Phase 6A.156 — whether organizer-defined sponsorship packages
    /// (Gold/Silver/Bronze) are exposed on the public event page. When false,
    /// packages can be drafted by the organizer but the public package grid is
    /// hidden. Existing rows missing this field deserialize to false (default),
    /// preserving pre-6A.156 behaviour for all existing events.
    /// </summary>
    public bool EnablePackages { get; init; }
}
