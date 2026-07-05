using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetActiveSponsorshipPackages;

/// <summary>
/// Phase 6A.157 — public/anonymous query returning the active, purchasable
/// sponsorship packages for an event. Backs
/// <c>GET /api/events/{eventId}/sponsorship-packages/active</c>.
///
/// Server-side filtering:
///   - Event must be Published
///   - Event's <c>SponsorConfig.IsEnabled == true</c>
///   - Event's <c>SponsorConfig.EnablePackages == true</c>
///   - Each package: <c>IsActive == true</c> AND
///     (<c>QuantityLimit IS NULL</c> OR <c>QuantitySold &lt; QuantityLimit</c>)
///
/// Any gate failure returns an empty list (NOT an error) — keeps the public
/// FE quiet on events that haven't opted into packages.
/// </summary>
public record GetActiveSponsorshipPackagesQuery(Guid EventId)
    : IQuery<IReadOnlyList<SponsorshipPackagePublicDto>>;
