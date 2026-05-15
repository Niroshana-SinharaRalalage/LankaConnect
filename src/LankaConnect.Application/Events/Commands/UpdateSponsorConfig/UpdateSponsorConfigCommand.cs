using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateSponsorConfig;

/// <summary>
/// Updates the sponsor configuration for an event.
/// Organizer-facing command to enable/disable and configure sponsorship settings.
/// Phase 6A.145 Commit 6: dropped MinAmountForSponsorImage threshold per UAT —
/// any sponsor can attach an image regardless of amount.
/// </summary>
public record UpdateSponsorConfigCommand(
    Guid EventId,
    bool IsEnabled,
    bool AcceptMoneySponsors,
    bool AcceptItemSponsors,
    decimal? MinSponsorAmount,
    string? SponsorMessage,
    bool ShowSponsorList
) : ICommand;
