using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetLayoutPublishReadiness;

/// <summary>
/// Slice S4 — non-gating publish-readiness snapshot for the canvas-editor / seating
/// section UI. Returns every blocker + warning + per-tier mapping summary at once,
/// so the organiser can see the entire fix list before attempting to publish.
/// The strict publish gate is still <c>PublishEventCommandHandler</c> via the
/// existing <c>Event.CheckLayoutPublishReadiness</c> path; this query is read-only.
/// </summary>
public record GetLayoutPublishReadinessQuery(Guid LayoutId)
    : IQuery<PublishReadinessReportDto>;

public record PublishReadinessReportDto(
    bool IsPublishReady,
    IReadOnlyList<PublishReadinessIssueDto> Blockers,
    IReadOnlyList<PublishReadinessIssueDto> Warnings,
    IReadOnlyList<TierMappingSummaryDto> TierSummary);

public record PublishReadinessIssueDto(
    string Code,
    string Message,
    Guid? ShapeId,
    string? ShapeName,
    Guid? TierId,
    string? TierName);

public record TierMappingSummaryDto(
    Guid TierId,
    string TierName,
    int TierCapacity,
    IReadOnlyList<MappedShapeRefDto> MappedZones,
    IReadOnlyList<MappedShapeRefDto> MappedTables,
    int TotalEnabledSeats);

public record MappedShapeRefDto(Guid Id, string Name, int EnabledSeatCount);
