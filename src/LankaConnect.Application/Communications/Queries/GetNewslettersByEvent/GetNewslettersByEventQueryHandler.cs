using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByEvent;

/// <summary>
/// Phase 6A.74 Part 3D: Query handler to get newsletters linked to an event
/// Phase 6A.61+ Fix: Changed from AutoMapper to manual mapping (consistent with other handlers)
/// </summary>
public class GetNewslettersByEventQueryHandler : IQueryHandler<GetNewslettersByEventQuery, IReadOnlyList<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ILogger<GetNewslettersByEventQueryHandler> _logger;

    public GetNewslettersByEventQueryHandler(
        INewsletterRepository newsletterRepository,
        ILogger<GetNewslettersByEventQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<NewsletterDto>>> Handle(GetNewslettersByEventQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Phase 6A.74] GetNewslettersByEventQuery - Getting newsletters for event {EventId}", request.EventId);

        try
        {
            var newsletters = await _newsletterRepository.GetByEventAsync(request.EventId, cancellationToken);

            // Phase 6A.61+ Fix: Manual mapping to match GetNewslettersByCreatorQueryHandler
            var result = newsletters.Select(newsletter => new NewsletterDto
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

            _logger.LogInformation("[Phase 6A.74] Found {Count} newsletters for event {EventId}", result.Count, request.EventId);

            return Result<IReadOnlyList<NewsletterDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.74] ERROR getting newsletters for event {EventId}", request.EventId);
            throw;
        }
    }
}
