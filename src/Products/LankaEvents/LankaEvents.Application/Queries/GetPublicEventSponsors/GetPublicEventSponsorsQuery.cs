using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetPublicEventSponsors;

/// <summary>
/// Phase 6A.150 — public, [AllowAnonymous] query returning a PII-redacted list
/// of sponsors-with-images for an event. Mirrors the data the public
/// SponsorsPreviewStrip + SponsorSection components actually render: only
/// confirmed Money sponsors (Completed) and confirmed Item sponsors
/// (RecordedItem) who have attached an image are included, sorted by
/// contribution magnitude (server-side; the dollar amounts themselves are
/// NOT exposed).
///
/// Replaces the previous flow where the public components hit the organizer-
/// only <c>GetEventSponsorsQuery</c>, got 401, and triggered the
/// AuthProvider login-redirect chain.
/// </summary>
public record GetPublicEventSponsorsQuery(Guid EventId)
    : IQuery<PublicEventSponsorsResponse>;
