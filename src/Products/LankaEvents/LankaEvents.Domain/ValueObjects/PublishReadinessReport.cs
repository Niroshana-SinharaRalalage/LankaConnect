namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Slice S4 — enumerated publish-readiness state for a venue layout.
/// <para>
/// Distinct from <see cref="LankaConnect.Domain.Events.Entities.VenueLayout.ValidateForEvent"/>:
/// that method short-circuits on the FIRST issue and is the authoritative publish gate
/// (called from <see cref="LankaConnect.Domain.Events.Event.CheckLayoutPublishReadiness"/>
/// inside <c>PublishEventCommandHandler</c>). The report below is a *non-gating* snapshot
/// surfaced through <c>GET /api/venue-layouts/{id}/publish-readiness</c> so the canvas
/// editor and seating-section UI can show every issue at once.
/// </para>
/// <para>
/// Layout is publish-ready when <see cref="Blockers"/> is empty.
/// </para>
/// </summary>
public sealed record PublishReadinessReport(
    IReadOnlyList<PublishReadinessIssue> Blockers,
    IReadOnlyList<PublishReadinessIssue> Warnings,
    IReadOnlyList<TierMappingSummary> TierSummary)
{
    public bool IsPublishReady => Blockers.Count == 0;
}

/// <summary>Single readiness issue (either blocker or warning).</summary>
public sealed record PublishReadinessIssue(
    PublishReadinessCode Code,
    string Message,
    Guid? ShapeId = null,
    string? ShapeName = null,
    Guid? TierId = null,
    string? TierName = null);

/// <summary>
/// Tier-by-tier mapping snapshot for the UI summary. Surfaces every active tier
/// with the zones + tables it currently covers and the total enabled seats.
/// </summary>
public sealed record TierMappingSummary(
    Guid TierId,
    string TierName,
    int TierCapacity,
    IReadOnlyList<MappedShapeRef> MappedZones,
    IReadOnlyList<MappedShapeRef> MappedTables,
    int TotalEnabledSeats);

/// <summary>Lightweight reference to a zone or table inside a tier summary entry.</summary>
public sealed record MappedShapeRef(Guid Id, string Name, int EnabledSeatCount);

/// <summary>Stable codes for surfacing localised messages or analytics tags.</summary>
public enum PublishReadinessCode
{
    /// <summary>Layout has no zones AND no tables.</summary>
    LayoutEmpty,
    /// <summary>Zone has at least one enabled seat but no tier_assignment.</summary>
    ZoneUnmapped,
    /// <summary>Zone exists with no seats and no tier mapping (warning, not blocker).</summary>
    ZoneEmptyAndUnmapped,
    /// <summary>Zone enabled-seat count exceeds the linked tier's capacity.</summary>
    ZoneOverCapacity,
    /// <summary>Table has at least one enabled seat but no tier_assignment.</summary>
    TableUnmapped,
    /// <summary>Table exists with no seats and no tier mapping (warning).</summary>
    TableEmptyAndUnmapped,
    /// <summary>Table enabled-seat count exceeds the linked tier's capacity.</summary>
    TableOverCapacity,
    /// <summary>Active tier has zero zone/table mappings (warning — buyers in this
    /// tier won't be able to choose a seat).</summary>
    TierWithoutMapping,
    /// <summary>Sum of all mapped seats for a tier exceeds the tier's capacity
    /// (multi-zone over-allocation; the per-zone check would miss this).</summary>
    TierTotalOverCapacity,
}
