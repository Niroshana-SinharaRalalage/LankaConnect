using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByCreator;

/// <summary>
/// Handler for GetNewslettersByCreatorQuery
/// Phase 6A.74: Newsletter listing by creator
/// </summary>
public class GetNewslettersByCreatorQueryHandler : IQueryHandler<GetNewslettersByCreatorQuery, List<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetNewslettersByCreatorQueryHandler> _logger;

    public GetNewslettersByCreatorQueryHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        ILogger<GetNewslettersByCreatorQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<List<NewsletterDto>>> Handle(GetNewslettersByCreatorQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74] GetNewslettersByCreatorQuery STARTED - User {UserId}",
            _currentUserService.UserId);

        try
        {
            var newsletters = await _newsletterRepository.GetByCreatorAsync(_currentUserService.UserId, cancellationToken);

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

            var dtoList = newsletters.Select(newsletter =>
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
                    IsAnnouncementOnly = newsletter.IsAnnouncementOnly,
                    CreatedAt = newsletter.CreatedAt,
                    UpdatedAt = newsletter.UpdatedAt,
                    EmailGroupIds = newsletter.EmailGroupIds,
                    EmailGroups = new List<EmailGroupSummaryDto>(),
                    MetroAreaIds = newsletter.MetroAreaIds,
                    MetroAreas = new List<MetroAreaSummaryDto>(),
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

            _logger.LogInformation(
                "[Phase 6A.74] Retrieved {Count} newsletters for user {UserId}",
                dtoList.Count, _currentUserService.UserId);

            return Result<List<NewsletterDto>>.Success(dtoList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR retrieving newsletters for user {UserId}",
                _currentUserService.UserId);
            throw;
        }
    }
}
