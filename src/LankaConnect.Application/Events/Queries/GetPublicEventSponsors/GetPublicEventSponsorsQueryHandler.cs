using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetPublicEventSponsors;

/// <summary>
/// Phase 6A.150 — handler for the public, PII-free sponsors read path.
///
/// Filter contract (matches the existing front-end public components'
/// client-side filter — see SponsorsPreviewStrip.tsx:35-43 and
/// SponsorSection.tsx:68-83):
///   - Sponsor must have an ImageUrl (the public preview is logo-driven).
///   - Money sponsors must be SponsorStatus.Completed.
///   - Item sponsors must be SponsorStatus.RecordedItem.
///
/// Sort: contribution magnitude descending — Money.Amount ?? Item.EstimatedValue ?? 0,
/// then CreatedAt descending. These are computed server-side; the public DTO
/// itself does NOT carry Amount, EstimatedValue, Status, or CreatedAt.
///
/// PII / financial fields stripped at projection time:
///   SponsorEmail, SponsorPhone, SponsorNotes, SponsorUserId,
///   Amount, EstimatedValue, Currency,
///   StripeFeeAmount, PlatformCommissionAmount, OrganizerPayoutAmount,
///   ImageBlobName, Status, PaymentCompletedAt, CreatedAt, ItemDescription.
/// Compile-time guarantee: PublicSponsorDto physically lacks these fields
/// (reflection-asserted in SponsorsControllerPublicEndpointTests).
/// </summary>
public class GetPublicEventSponsorsQueryHandler
    : IQueryHandler<GetPublicEventSponsorsQuery, PublicEventSponsorsResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ILogger<GetPublicEventSponsorsQueryHandler> _logger;

    public GetPublicEventSponsorsQueryHandler(
        IEventRepository eventRepository,
        ISponsorRepository sponsorRepository,
        ILogger<GetPublicEventSponsorsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _sponsorRepository = sponsorRepository;
        _logger = logger;
    }

    public async Task<Result<PublicEventSponsorsResponse>> Handle(
        GetPublicEventSponsorsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetPublicEventSponsors"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "GetPublicEventSponsors START: EventId={EventId}", request.EventId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "GetPublicEventSponsors DENIED (404): event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<PublicEventSponsorsResponse>.NotFound("Event not found");
                }

                var sponsors = await _sponsorRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                // Phase 6A.148.W5.D10 — operator UAT feedback: ALL sponsorships should appear
                // in public surfaces uniformly regardless of creation path (organizer-added,
                // standalone via event sponsor section, or bundled at registration-checkout
                // time). Previously the filter required ImageUrl so bundled-at-registration
                // sponsorships (which often skip the optional image upload) were silently
                // hidden — operator's $120 sponsor 110ffdef sat refunded with no public
                // visibility because no image was uploaded at checkout.
                //
                // Filter retained: only confirmed sponsors (Money/Completed, Item/RecordedItem)
                // are exposed publicly. Pending/Abandoned/Refunded/Failed stay hidden
                // (those are mid-flight or terminal states that should never be advertised).
                //
                // FE (SponsorsPreviewStrip.tsx) handles the missing-ImageUrl case with a
                // placeholder card that shows the sponsor name + initials so the design
                // doesn't break on text-only entries.
                var eligible = sponsors
                    .Where(s =>
                        (s.Type == SponsorType.Money && s.Status == SponsorStatus.Completed) ||
                        (s.Type == SponsorType.Item && s.Status == SponsorStatus.RecordedItem))
                    .OrderByDescending(s => s.Amount?.Amount ?? s.EstimatedValue ?? 0)
                    .ThenByDescending(s => s.CreatedAt)
                    .ToList();

                // Project to PII-free DTO. The magnitude / status / timing fields used
                // for sorting are intentionally NOT carried into the output.
                var publicSponsors = eligible.Select(s => new PublicSponsorDto
                {
                    Id = s.Id,
                    SponsorOrganization = s.SponsorOrganization,
                    SponsorName = s.SponsorName,
                    ItemName = s.ItemName,
                    ImageUrl = s.ImageUrl,
                    SponsorType = s.Type.ToString(),
                }).ToList();

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetPublicEventSponsors COMPLETE: EventId={EventId}, EligibleCount={EligibleCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    request.EventId, publicSponsors.Count, sponsors.Count, stopwatch.ElapsedMilliseconds);

                return Result<PublicEventSponsorsResponse>.Success(new PublicEventSponsorsResponse
                {
                    EventId = request.EventId,
                    Sponsors = publicSponsors,
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetPublicEventSponsors FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
