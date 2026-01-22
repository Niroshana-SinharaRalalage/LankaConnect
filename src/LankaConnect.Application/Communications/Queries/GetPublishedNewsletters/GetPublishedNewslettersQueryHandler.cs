using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "GetPublishedNewsletters"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetPublishedNewsletters START: UserId={UserId}, SearchTerm={SearchTerm}, MetroAreaCount={MetroCount}, PublishedFrom={PublishedFrom}, PublishedTo={PublishedTo}, State={State}",
                request.UserId,
                request.SearchTerm,
                request.MetroAreaIds?.Count ?? 0,
                request.PublishedFrom,
                request.PublishedTo,
                request.State);

            try
            {
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
                    MetroAreas = new List<MetroAreaSummaryDto>(), // Can be populated by frontend if needed
                    // Phase 6A.74 Part 14: Announcement-only flag (always false for public page)
                    IsAnnouncementOnly = newsletter.IsAnnouncementOnly
                }).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetPublishedNewsletters COMPLETE: ReturnedCount={Count}, SearchTerm={SearchTerm}, State={State}, HasLocationFilter={HasLocation}, Duration={ElapsedMs}ms",
                    result.Count, request.SearchTerm, request.State, request.Latitude.HasValue && request.Longitude.HasValue, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<NewsletterDto>>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetPublishedNewsletters FAILED: Exception occurred - SearchTerm={SearchTerm}, State={State}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.SearchTerm, request.State, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
