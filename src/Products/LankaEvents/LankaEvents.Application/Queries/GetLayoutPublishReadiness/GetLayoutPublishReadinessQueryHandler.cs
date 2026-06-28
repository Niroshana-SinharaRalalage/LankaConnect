using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetLayoutPublishReadiness;

/// <summary>
/// Slice S4 — loads the layout (with zones/tables/seats) and the bound event's
/// ticket tiers (with polymorphic <c>tier_assignments</c>), runs the domain's
/// <see cref="VenueLayout.BuildPublishReadinessReport"/> enumeration, and projects
/// the result to a flat DTO. Template layouts (EventId == null) have no event
/// context — return a "template" report (empty blockers/warnings; an empty tier
/// summary) so the UI can show a banner explaining "templates are validated when
/// applied to an event."
/// </summary>
public class GetLayoutPublishReadinessQueryHandler
    : IQueryHandler<GetLayoutPublishReadinessQuery, PublishReadinessReportDto>
{
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetLayoutPublishReadinessQueryHandler> _logger;

    public GetLayoutPublishReadinessQueryHandler(
        IVenueLayoutRepository layoutRepository,
        IEventRepository eventRepository,
        ILogger<GetLayoutPublishReadinessQueryHandler> logger)
    {
        _layoutRepository = layoutRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<PublishReadinessReportDto>> Handle(
        GetLayoutPublishReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var layout = await _layoutRepository.GetWithZonesAndSeatsAsync(
            request.LayoutId, cancellationToken);
        if (layout is null)
        {
            return Result<PublishReadinessReportDto>.Failure(
                "Venue layout not found", ErrorKind.NotFound);
        }

        IReadOnlyList<TicketTier> tiers;
        if (layout.EventId.HasValue)
        {
            var @event = await _eventRepository.GetByIdAsync(
                layout.EventId.Value, trackChanges: false, cancellationToken);
            tiers = @event?.TicketTiers ?? new List<TicketTier>().AsReadOnly();
        }
        else
        {
            // Template layouts have no event-tier context. The UI surfaces this
            // as "validated on apply"; return an empty-but-valid report.
            tiers = new List<TicketTier>().AsReadOnly();
        }

        var report = layout.BuildPublishReadinessReport(tiers);

        _logger.LogInformation(
            "PublishReadiness: layout={LayoutId} blockers={Blockers} warnings={Warnings} tiers={Tiers}",
            request.LayoutId, report.Blockers.Count, report.Warnings.Count, report.TierSummary.Count);

        return Result<PublishReadinessReportDto>.Success(MapToDto(report));
    }

    private static PublishReadinessReportDto MapToDto(PublishReadinessReport report)
    {
        return new PublishReadinessReportDto(
            IsPublishReady: report.IsPublishReady,
            Blockers: report.Blockers.Select(MapIssue).ToList(),
            Warnings: report.Warnings.Select(MapIssue).ToList(),
            TierSummary: report.TierSummary.Select(MapTier).ToList());
    }

    private static PublishReadinessIssueDto MapIssue(PublishReadinessIssue issue) =>
        new(
            Code: issue.Code.ToString(),
            Message: issue.Message,
            ShapeId: issue.ShapeId,
            ShapeName: issue.ShapeName,
            TierId: issue.TierId,
            TierName: issue.TierName);

    private static TierMappingSummaryDto MapTier(TierMappingSummary tier) =>
        new(
            TierId: tier.TierId,
            TierName: tier.TierName,
            TierCapacity: tier.TierCapacity,
            MappedZones: tier.MappedZones
                .Select(s => new MappedShapeRefDto(s.Id, s.Name, s.EnabledSeatCount))
                .ToList(),
            MappedTables: tier.MappedTables
                .Select(s => new MappedShapeRefDto(s.Id, s.Name, s.EnabledSeatCount))
                .ToList(),
            TotalEnabledSeats: tier.TotalEnabledSeats);
}
