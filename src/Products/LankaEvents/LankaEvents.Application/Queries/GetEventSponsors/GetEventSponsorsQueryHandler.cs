using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventSponsors;

/// <summary>
/// Handles retrieving all sponsors for an event with summary statistics.
/// </summary>
public class GetEventSponsorsQueryHandler : IQueryHandler<GetEventSponsorsQuery, EventSponsorsResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ILogger<GetEventSponsorsQueryHandler> _logger;

    public GetEventSponsorsQueryHandler(
        IEventRepository eventRepository,
        ISponsorRepository sponsorRepository,
        ILogger<GetEventSponsorsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _sponsorRepository = sponsorRepository;
        _logger = logger;
    }

    public async Task<Result<EventSponsorsResponse>> Handle(
        GetEventSponsorsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventSponsors"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventSponsors START: EventId={EventId}",
                request.EventId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<EventSponsorsResponse>.Failure("Event not found");

                var sponsors = await _sponsorRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                var sponsorDtos = sponsors.Select(s => new SponsorDto
                {
                    Id = s.Id,
                    EventId = s.EventId,
                    SponsorUserId = s.SponsorUserId,
                    SponsorName = s.SponsorName,
                    SponsorEmail = s.SponsorEmail,
                    SponsorPhone = s.SponsorPhone,
                    SponsorOrganization = s.SponsorOrganization,
                    SponsorNotes = s.SponsorNotes,
                    SponsorType = s.Type.ToString(),
                    Amount = s.Amount?.Amount,
                    Currency = s.Amount?.Currency.ToString(),
                    Status = s.Status.ToString(),
                    ItemName = s.ItemName,
                    ItemDescription = s.ItemDescription,
                    EstimatedValue = s.EstimatedValue,
                    ImageUrl = s.ImageUrl,
                    ImageBlobName = s.ImageBlobName,
                    // Phase 6A.162 — brochure slot (sibling to logo)
                    BrochureUrl = s.BrochureUrl,
                    BrochureBlobName = s.BrochureBlobName,
                    StripeFeeAmount = s.StripeFeeAmount?.Amount,
                    PlatformCommissionAmount = s.PlatformCommissionAmount?.Amount,
                    OrganizerPayoutAmount = s.OrganizerPayoutAmount?.Amount,
                    CreatedAt = s.CreatedAt,
                    PaymentCompletedAt = s.PaymentCompletedAt
                }).ToList();

                var completedMoneySponsors = sponsors
                    .Where(s => s.Type == SponsorType.Money && s.Status == SponsorStatus.Completed)
                    .ToList();
                var recordedItemSponsors = sponsors
                    .Where(s => s.Type == SponsorType.Item && s.Status == SponsorStatus.RecordedItem)
                    .ToList();

                var totalMoneyAmount = completedMoneySponsors.Sum(s => s.Amount?.Amount ?? 0);
                var currency = completedMoneySponsors.FirstOrDefault()?.Amount?.Currency.ToString() ?? "USD";

                var summary = new SponsorSummaryDto
                {
                    TotalSponsors = sponsors.Count,
                    CompletedMoneySponsors = completedMoneySponsors.Count,
                    RecordedItemSponsors = recordedItemSponsors.Count,
                    TotalMoneyAmount = totalMoneyAmount,
                    Currency = currency,
                    ItemSponsorCount = recordedItemSponsors.Count,
                    TotalStripeFees = completedMoneySponsors.Sum(s => s.StripeFeeAmount?.Amount ?? 0),
                    TotalPlatformCommission = completedMoneySponsors.Sum(s => s.PlatformCommissionAmount?.Amount ?? 0),
                    TotalOrganizerPayout = completedMoneySponsors.Sum(s => s.OrganizerPayoutAmount?.Amount ?? 0)
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventSponsors COMPLETE: EventId={EventId}, TotalSponsors={Total}, CompletedMoney={CompletedMoney}, RecordedItems={RecordedItems}, TotalMoneyAmount={Amount}, Duration={ElapsedMs}ms",
                    request.EventId, sponsors.Count, completedMoneySponsors.Count, recordedItemSponsors.Count, totalMoneyAmount, stopwatch.ElapsedMilliseconds);

                return Result<EventSponsorsResponse>.Success(new EventSponsorsResponse
                {
                    EventId = request.EventId,
                    EventTitle = @event.Title.Value,
                    Sponsors = sponsorDtos,
                    Summary = summary
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventSponsors FAILED: EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);

                return Result<EventSponsorsResponse>.Failure($"Failed to retrieve sponsors: {ex.Message}");
            }
        }
    }
}
