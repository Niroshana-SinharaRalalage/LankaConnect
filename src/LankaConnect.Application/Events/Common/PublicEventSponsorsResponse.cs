namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.150 — top-level wrapper for the <c>[AllowAnonymous] GET /api/events/{id}/sponsors/public</c>
/// endpoint. Unlike the organizer-only <see cref="EventSponsorsResponse"/>, this
/// record does NOT carry a <c>Summary</c> field (which contains financial totals
/// and per-status counts — organizer-only data).
/// </summary>
public class PublicEventSponsorsResponse
{
    public Guid EventId { get; init; }
    public List<PublicSponsorDto> Sponsors { get; init; } = new();
}
