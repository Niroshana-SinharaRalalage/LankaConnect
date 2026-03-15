using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CreateSponsor;

/// <summary>
/// Item-based sponsorship command — no Stripe payment needed.
/// Returns the sponsor ID (not a checkout URL) since the entity is immediately recorded.
/// </summary>
public record CreateItemSponsorCommand(
    Guid EventId,
    string SponsorName,
    string SponsorEmail,
    string? SponsorPhone,
    string? SponsorOrganization,
    string? SponsorNotes,
    string ItemName,
    string? ItemDescription,
    decimal? EstimatedValue,
    // Null for anonymous sponsors
    Guid? UserId = null
) : ICommand<Guid>;  // Returns sponsor ID
