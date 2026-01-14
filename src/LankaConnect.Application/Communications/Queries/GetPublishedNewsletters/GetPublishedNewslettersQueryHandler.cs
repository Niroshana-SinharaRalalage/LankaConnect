using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Queries.GetPublishedNewsletters;

/// <summary>
/// Handler for GetPublishedNewslettersQuery
/// Phase 6A.74 Parts 10 & 11: Public newsletter list page with filtering
/// Returns published (Active) newsletters with location-based sorting and filtering
/// </summary>
public class GetPublishedNewslettersQueryHandler : IQueryHandler<GetPublishedNewslettersQuery, IReadOnlyList<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ILogger<GetPublishedNewslettersQueryHandler> _logger;

    public GetPublishedNewslettersQueryHandler(
        INewsletterRepository newsletterRepository,
        ILogger<GetPublishedNewslettersQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<NewsletterDto>>> Handle(
        GetPublishedNewslettersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.74 Parts 10/11] GetPublishedNewslettersQuery STARTED - UserId: {UserId}, SearchTerm: {SearchTerm}, MetroAreaCount: {MetroCount}",
                request.UserId,
                request.SearchTerm,
                request.MetroAreaIds?.Count ?? 0);

            // Get published newsletters with filtering
            var newsletters = await _newsletterRepository.GetPublishedWithFiltersAsync(
                publishedFrom: request.PublishedFrom,
                publishedTo: request.PublishedTo,
                state: request.State,
                metroAreaIds: request.MetroAreaIds,
                searchTerm: request.SearchTerm,
                userId: request.UserId,
                latitude: request.Latitude,
                longitude: request.Longitude,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74 Parts 10/11] GetPublishedNewslettersQuery COMPLETED - Found {Count} newsletters",
                newsletters.Count);

            // Manual mapping to DTOs (following pattern from GetNewsletterByIdQueryHandler)
            var result = newsletters.Select(newsletter => new NewsletterDto
            {
                Id = newsletter.Id,
                Title = newsletter.Title.Value,
                Description = newsletter.Description.Value,
                CreatedByUserId = newsletter.CreatedByUserId,
                CreatedByUserName = string.Empty, // Public endpoint, no user details
                EventId = newsletter.EventId,
                EventTitle = null, // Can be populated by frontend if needed
                Status = newsletter.Status,
                PublishedAt = newsletter.PublishedAt,
                SentAt = newsletter.SentAt,
                ExpiresAt = newsletter.ExpiresAt,
                IncludeNewsletterSubscribers = newsletter.IncludeNewsletterSubscribers,
                TargetAllLocations = newsletter.TargetAllLocations,
                CreatedAt = newsletter.CreatedAt,
                UpdatedAt = newsletter.UpdatedAt,
                EmailGroupIds = newsletter.EmailGroupIds,
                EmailGroups = new List<EmailGroupSummaryDto>(), // Public endpoint, no group details
                MetroAreaIds = newsletter.MetroAreaIds,
                MetroAreas = new List<MetroAreaSummaryDto>() // Can be populated by frontend if needed
            }).ToList();

            return Result<IReadOnlyList<NewsletterDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74 Parts 10/11] GetPublishedNewslettersQuery FAILED - Error: {Message}",
                ex.Message);

            throw;
        }
    }
}
