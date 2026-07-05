using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateSponsorConfig;

/// <summary>
/// Updates the sponsor configuration for an event.
/// Organizer-facing command to enable/disable and configure sponsorship settings.
/// Phase 6A.145 Commit 6: dropped MinAmountForSponsorImage threshold per UAT —
/// any sponsor can attach an image regardless of amount.
/// Phase 6A.156: added <see cref="EnablePackages"/> — gates whether the
/// organizer-defined sponsorship-package grid is exposed on the public event
/// page (default false; backward-compatible for all existing events).
/// </summary>
public record UpdateSponsorConfigCommand(
    Guid EventId,
    bool IsEnabled,
    bool AcceptMoneySponsors,
    bool AcceptItemSponsors,
    decimal? MinSponsorAmount,
    string? SponsorMessage,
    bool ShowSponsorList,
    bool EnablePackages = false
) : ICommand;
