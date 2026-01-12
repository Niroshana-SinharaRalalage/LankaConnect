using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByCreator;

/// <summary>
/// Handler for GetNewslettersByCreatorQuery
/// Phase 6A.74: Newsletter listing by creator
/// </summary>
public class GetNewslettersByCreatorQueryHandler : IQueryHandler<GetNewslettersByCreatorQuery, List<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetNewslettersByCreatorQueryHandler> _logger;

    public GetNewslettersByCreatorQueryHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        ILogger<GetNewslettersByCreatorQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
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

            var dtoList = newsletters.Select(newsletter => new NewsletterDto
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
                MetroAreas = new List<MetroAreaSummaryDto>()
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
