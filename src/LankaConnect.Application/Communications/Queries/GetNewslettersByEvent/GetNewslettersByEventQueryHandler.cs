using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByEvent;

/// <summary>
/// Phase 6A.74 Part 3D: Query handler to get newsletters linked to an event
/// Phase 6A.61+ Fix: Changed from AutoMapper to manual mapping (consistent with other handlers)
/// </summary>
public class GetNewslettersByEventQueryHandler : IQueryHandler<GetNewslettersByEventQuery, IReadOnlyList<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetNewslettersByEventQueryHandler> _logger;

    public GetNewslettersByEventQueryHandler(
        INewsletterRepository newsletterRepository,
        IApplicationDbContext dbContext,
        ILogger<GetNewslettersByEventQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<NewsletterDto>>> Handle(GetNewslettersByEventQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetNewslettersByEvent"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetNewslettersByEvent START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNewslettersByEvent FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<NewsletterDto>>.Failure("Event ID is required");
                }

                var newsletters = await _newsletterRepository.GetByEventAsync(request.EventId, cancellationToken);

                // Phase 6A.74 Part 13 Issue #1: Get recipient counts from NewsletterEmailHistory
                var newsletterIds = newsletters.Select(n => n.Id).ToList();
                var dbContext = _dbContext as DbContext;

                // Get history records for all newsletters in a single query
                // Phase 6A.74 Part 14 Fix: Group by NewsletterId and take most recent (newsletters can now be sent multiple times)
                var historyRecords = dbContext != null
                    ? await dbContext.Set<NewsletterEmailHistory>()
                        .Where(h => newsletterIds.Contains(h.NewsletterId))
                        .GroupBy(h => h.NewsletterId)
                        .Select(g => g.OrderByDescending(h => h.CreatedAt).First())
                        .ToDictionaryAsync(h => h.NewsletterId, cancellationToken)
                    : new Dictionary<Guid, NewsletterEmailHistory>();

                // Phase 6A.61+ Fix: Manual mapping to match GetNewslettersByCreatorQueryHandler
                var result = newsletters.Select(newsletter =>
                {
                    // Get history record if exists
                    historyRecords.TryGetValue(newsletter.Id, out var history);

                    return new NewsletterDto
                    {
                        Id = newsletter.Id,
                        Title = newsletter.Title.Value,
                        Description = newsletter.Description.Value,
                        CreatedByUserId = newsletter.CreatedByUserId,
                        CreatedByUserName = string.Empty,
                        EventId = newsletter.EventId,
                        EventTitle = null,
                        Status = newsletter.Status,
                        PublishedAt = newsletter.PublishedAt,
                        SentAt = newsletter.SentAt,
                        ExpiresAt = newsletter.ExpiresAt,
                        IncludeNewsletterSubscribers = newsletter.IncludeNewsletterSubscribers,
                        TargetAllLocations = newsletter.TargetAllLocations,
                        CreatedAt = newsletter.CreatedAt,
                        UpdatedAt = newsletter.UpdatedAt,
                        EmailGroupIds = newsletter.EmailGroupIds,
                        EmailGroups = new List<EmailGroupSummaryDto>(),
                        MetroAreaIds = newsletter.MetroAreaIds,
                        MetroAreas = new List<MetroAreaSummaryDto>(),
                        // Phase 6A.74 Part 14: Announcement-only flag
                        IsAnnouncementOnly = newsletter.IsAnnouncementOnly,
                        // Phase 6A.74 Part 13+: Populate all recipient breakdown fields from history
                        TotalRecipientCount = history?.TotalRecipientCount,
                        NewsletterEmailGroupCount = history?.NewsletterEmailGroupCount,
                        EventEmailGroupCount = history?.EventEmailGroupCount,
                        SubscriberCount = history?.SubscriberCount,
                        EventRegistrationCount = history?.EventRegistrationCount,
                        SuccessfulSends = history?.SuccessfulSends,
                        FailedSends = history?.FailedSends,
                        // Legacy fields for backwards compatibility
                        EmailGroupRecipientCount = history?.EmailGroupRecipientCount,
                        SubscriberRecipientCount = history?.SubscriberRecipientCount
                    };
                }).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetNewslettersByEvent COMPLETE: EventId={EventId}, NewsletterCount={Count}, WithHistoryCount={HistoryCount}, Duration={ElapsedMs}ms",
                    request.EventId, result.Count, historyRecords.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<NewsletterDto>>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetNewslettersByEvent FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
