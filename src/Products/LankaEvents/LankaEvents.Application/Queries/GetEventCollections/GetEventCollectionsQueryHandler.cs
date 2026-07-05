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
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventCollections;

/// <summary>
/// Handles retrieving all collections for an event with summary statistics.
/// </summary>
public class GetEventCollectionsQueryHandler : IQueryHandler<GetEventCollectionsQuery, EventCollectionsResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<GetEventCollectionsQueryHandler> _logger;

    public GetEventCollectionsQueryHandler(
        IEventRepository eventRepository,
        ICollectionRepository collectionRepository,
        ILogger<GetEventCollectionsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _collectionRepository = collectionRepository;
        _logger = logger;
    }

    public async Task<Result<EventCollectionsResponse>> Handle(
        GetEventCollectionsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventCollections"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventCollections START: EventId={EventId}",
                request.EventId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<EventCollectionsResponse>.Failure("Event not found");

                var collections = await _collectionRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                var collectionDtos = collections.Select(c => new CollectionDto
                {
                    Id = c.Id,
                    EventId = c.EventId,
                    ContributorUserId = c.ContributorUserId,
                    ContributorName = c.ContributorName,
                    ContributorEmail = c.ContributorEmail,
                    ContributorPhone = c.ContributorPhone,
                    ContributorNotes = c.ContributorNotes,
                    Amount = c.Amount.Amount,
                    Currency = c.Amount.Currency.ToString(),
                    Status = c.Status.ToString(),
                    StripeFeeAmount = c.StripeFeeAmount?.Amount,
                    PlatformCommissionAmount = c.PlatformCommissionAmount?.Amount,
                    OrganizerPayoutAmount = c.OrganizerPayoutAmount?.Amount,
                    CreatedAt = c.CreatedAt,
                    PaymentCompletedAt = c.PaymentCompletedAt
                }).ToList();

                var completedCollections = collections.Where(c => c.Status == CollectionStatus.Completed).ToList();
                var totalAmount = completedCollections.Sum(c => c.Amount.Amount);
                var currency = completedCollections.FirstOrDefault()?.Amount.Currency.ToString() ?? "USD";

                var contributorCount = await _collectionRepository.GetContributorCountForEventAsync(
                    request.EventId, cancellationToken);

                // Get goal information from event's collection configuration
                var goalAmount = @event.CollectionConfig?.GoalAmount;
                decimal? goalProgressPercent = null;
                if (goalAmount.HasValue && goalAmount.Value > 0)
                {
                    goalProgressPercent = Math.Round((totalAmount / goalAmount.Value) * 100, 2);
                }

                var summary = new CollectionSummaryDto
                {
                    TotalCollections = collections.Count,
                    CompletedCollections = completedCollections.Count,
                    TotalAmount = totalAmount,
                    AverageCollection = completedCollections.Count > 0 ? totalAmount / completedCollections.Count : 0,
                    Currency = currency,
                    GoalAmount = goalAmount,
                    GoalProgressPercent = goalProgressPercent,
                    ContributorCount = contributorCount,
                    TotalStripeFees = completedCollections.Sum(c => c.StripeFeeAmount?.Amount ?? 0),
                    TotalPlatformCommission = completedCollections.Sum(c => c.PlatformCommissionAmount?.Amount ?? 0),
                    TotalOrganizerPayout = completedCollections.Sum(c => c.OrganizerPayoutAmount?.Amount ?? 0)
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventCollections COMPLETE: EventId={EventId}, TotalCollections={Total}, CompletedCollections={Completed}, TotalAmount={Amount}, ContributorCount={Contributors}, Duration={ElapsedMs}ms",
                    request.EventId, collections.Count, completedCollections.Count, totalAmount, contributorCount, stopwatch.ElapsedMilliseconds);

                return Result<EventCollectionsResponse>.Success(new EventCollectionsResponse
                {
                    EventId = request.EventId,
                    EventTitle = @event.Title.Value,
                    Collections = collectionDtos,
                    Summary = summary
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventCollections FAILED: EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);

                return Result<EventCollectionsResponse>.Failure($"Failed to retrieve collections: {ex.Message}");
            }
        }
    }
}
