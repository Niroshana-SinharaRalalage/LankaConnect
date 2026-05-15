using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateSponsorConfig;

/// <summary>
/// Updates the sponsor configuration for an event.
/// Organizer-facing command to enable/disable and configure sponsorship settings.
/// Phase 6A.145: <see cref="MinAmountForSponsorImage"/> added (opt-in threshold for
/// per-sponsor image upload). Null = feature off.
/// </summary>
public record UpdateSponsorConfigCommand(
    Guid EventId,
    bool IsEnabled,
    bool AcceptMoneySponsors,
    bool AcceptItemSponsors,
    decimal? MinSponsorAmount,
    string? SponsorMessage,
    bool ShowSponsorList,
    decimal? MinAmountForSponsorImage = null
) : ICommand;
