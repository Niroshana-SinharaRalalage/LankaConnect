namespace LankaConnect.Application.Events.Common;

/// <summary>
/// DTO for donation configuration on an event.
/// Maps from DonationConfiguration value object.
/// </summary>
public record DonationConfigurationDto
{
    public bool IsEnabled { get; init; }
    public List<decimal> SuggestedAmounts { get; init; } = new();
    public bool AllowCustomAmount { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? DonationMessage { get; init; }
    public bool ShowDonationSummary { get; init; }
}
