using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Queries.GetPublishedNewsletters;

/// <summary>
/// Handler for GetPublishedNewslettersQuery
/// Phase 6A.74 Parts 10 & 11: Public newsletter list page with filtering
/// Returns published (Active) newsletters with location-based sorting and filtering
/// Phase 6A.135: Enriched with email group and metro area summary data
/// </summary>
public class GetPublishedNewslettersQueryHandler : IQueryHandler<GetPublishedNewslettersQuery, IReadOnlyList<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetPublishedNewslettersQueryHandler> _logger;

    public GetPublishedNewslettersQueryHandler(
        INewsletterRepository newsletterRepository,
        IApplicationDbContext dbContext,
        ILogger<GetPublishedNewslettersQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _dbContext = dbContext;
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

                var newsletterIds = newsletters.Select(n => n.Id).ToList();
                var dbContext = _dbContext as DbContext;

                // Phase 6A.135: Query junction tables for email group and metro area mappings
                var emailGroupJunction = dbContext != null
                    ? await dbContext.Set<Dictionary<string, object>>("newsletter_email_groups")
                        .Where(j => newsletterIds.Contains((Guid)j["newsletter_id"]))
                        .Select(j => new { NewsletterId = (Guid)j["newsletter_id"], EmailGroupId = (Guid)j["email_group_id"] })
                        .ToListAsync(cancellationToken)
                    : new List<object>().Select(x => new { NewsletterId = Guid.Empty, EmailGroupId = Guid.Empty }).ToList();

                var metroAreaJunction = dbContext != null
                    ? await dbContext.Set<Dictionary<string, object>>("newsletter_metro_areas")
                        .Where(j => newsletterIds.Contains((Guid)j["newsletter_id"]))
                        .Select(j => new { NewsletterId = (Guid)j["newsletter_id"], MetroAreaId = (Guid)j["metro_area_id"] })
                        .ToListAsync(cancellationToken)
                    : new List<object>().Select(x => new { NewsletterId = Guid.Empty, MetroAreaId = Guid.Empty }).ToList();

                // Batch load email group entities
                var allEmailGroupIds = emailGroupJunction.Select(j => j.EmailGroupId).Distinct().ToList();
                var emailGroupLookup = allEmailGroupIds.Any() && dbContext != null
                    ? (await dbContext.Set<EmailGroup>()
                        .AsNoTracking()
                        .Where(eg => allEmailGroupIds.Contains(eg.Id))
                        .ToListAsync(cancellationToken))
                        .ToDictionary(eg => eg.Id)
                    : new Dictionary<Guid, EmailGroup>();

                // Batch load metro area entities
                var allMetroAreaIds = metroAreaJunction.Select(j => j.MetroAreaId).Distinct().ToList();
                var metroAreaLookup = allMetroAreaIds.Any()
                    ? (await _dbContext.MetroAreas
                        .AsNoTracking()
                        .Where(m => allMetroAreaIds.Contains(m.Id))
                        .ToListAsync(cancellationToken))
                        .ToDictionary(m => m.Id)
                    : new Dictionary<Guid, MetroArea>();

                // Build per-newsletter lookup dictionaries
                var emailGroupIdsByNewsletter = emailGroupJunction
                    .GroupBy(j => j.NewsletterId)
                    .ToDictionary(g => g.Key, g => g.Select(j => j.EmailGroupId).ToList());

                var emailGroupDtosByNewsletter = emailGroupIdsByNewsletter
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                            .Where(id => emailGroupLookup.ContainsKey(id))
                            .Select(id => new EmailGroupSummaryDto
                            {
                                Id = emailGroupLookup[id].Id,
                                Name = emailGroupLookup[id].Name,
                                IsActive = emailGroupLookup[id].IsActive
                            }).ToList());

                var metroAreaIdsByNewsletter = metroAreaJunction
                    .GroupBy(j => j.NewsletterId)
                    .ToDictionary(g => g.Key, g => g.Select(j => j.MetroAreaId).ToList());

                var metroAreaDtosByNewsletter = metroAreaIdsByNewsletter
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                            .Where(id => metroAreaLookup.ContainsKey(id))
                            .Select(id => new MetroAreaSummaryDto
                            {
                                Id = metroAreaLookup[id].Id,
                                Name = metroAreaLookup[id].Name,
                                State = metroAreaLookup[id].State
                            }).ToList());

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
                    EmailGroupIds = emailGroupIdsByNewsletter.TryGetValue(newsletter.Id, out var egIds)
                        ? egIds : newsletter.EmailGroupIds,
                    EmailGroups = emailGroupDtosByNewsletter.TryGetValue(newsletter.Id, out var egDtos)
                        ? egDtos : new List<EmailGroupSummaryDto>(),
                    MetroAreaIds = metroAreaIdsByNewsletter.TryGetValue(newsletter.Id, out var maIds)
                        ? maIds : newsletter.MetroAreaIds,
                    MetroAreas = metroAreaDtosByNewsletter.TryGetValue(newsletter.Id, out var maDtos)
                        ? maDtos : new List<MetroAreaSummaryDto>(),
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
