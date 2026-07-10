using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Contracts; // Wave 5.4.d.3: IEmailGroupQueries replaces dbContext.Set<EmailGroup>
using ContractsEmailGroupSummaryDto = LankaConnect.Modules.Communications.Contracts.EmailGroupSummaryDto;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using LankaConnect.Modules.Communications.Infrastructure.Data; // Wave 6.5.f mirror (2026-07-09 Day 4): CommunicationsDbContext
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // 4C.h Day 5: MetroAreas cross-module
namespace LankaConnect.Modules.Communications.Application.Queries.GetNewslettersByEvent;

/// <summary>
/// Phase 6A.74 Part 3D: Query handler to get newsletters linked to an event
/// Phase 6A.61+ Fix: Changed from AutoMapper to manual mapping (consistent with other handlers)
/// </summary>
public class GetNewslettersByEventQueryHandler : IQueryHandler<GetNewslettersByEventQuery, IReadOnlyList<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly CommunicationsDbContext _dbContext;
    private readonly LankaEventsDbContext _eventsContext;
    private readonly IEmailGroupQueries _emailGroupQueries; // Wave 5.4.d.3
    private readonly ILogger<GetNewslettersByEventQueryHandler> _logger;

    public GetNewslettersByEventQueryHandler(
        INewsletterRepository newsletterRepository,
        CommunicationsDbContext dbContext,
        LankaEventsDbContext eventsContext,
        IEmailGroupQueries emailGroupQueries, // Wave 5.4.d.3
        ILogger<GetNewslettersByEventQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _dbContext = dbContext;
        _eventsContext = eventsContext;
        _emailGroupQueries = emailGroupQueries;
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

                // Phase 6A.135: Query junction tables for email group and metro area mappings.
                // Wave 5.4.d.1b (2026-06-22) — newsletter_email_groups is now the CLR-typed
                // NewsletterEmailGroupLink junction; the shared-type Dictionary access throws
                // "Cannot create a DbSet for 'Dictionary<string, object>'" at runtime.
                var emailGroupJunction = dbContext != null
                    ? (await dbContext.Set<NewsletterEmailGroupLink>()
                        .Where(j => newsletterIds.Contains(j.NewsletterId))
                        .Select(j => new { j.NewsletterId, j.EmailGroupId })
                        .ToListAsync(cancellationToken))
                    : new List<object>().Select(x => new { NewsletterId = Guid.Empty, EmailGroupId = Guid.Empty }).ToList();

                var metroAreaJunction = dbContext != null
                    ? await dbContext.Set<Dictionary<string, object>>("newsletter_metro_areas")
                        .Where(j => newsletterIds.Contains((Guid)j["newsletter_id"]))
                        .Select(j => new { NewsletterId = (Guid)j["newsletter_id"], MetroAreaId = (Guid)j["metro_area_id"] })
                        .ToListAsync(cancellationToken)
                    : new List<object>().Select(x => new { NewsletterId = Guid.Empty, MetroAreaId = Guid.Empty }).ToList();

                // Wave 5.4.d.3 (2026-06-22): batch fetch via IEmailGroupQueries (cross-module Contracts).
                // Replaces the previous dbContext.Set<EmailGroup>().Where().ToList() pull.
                var allEmailGroupIds = emailGroupJunction.Select(j => j.EmailGroupId).Distinct().ToList();
                var emailGroupLookup = allEmailGroupIds.Any()
                    ? (await _emailGroupQueries.GetByIdsAsync(allEmailGroupIds, cancellationToken))
                        .ToDictionary(eg => eg.Id)
                    : new Dictionary<Guid, ContractsEmailGroupSummaryDto>();

                // Batch load metro area entities
                var allMetroAreaIds = metroAreaJunction.Select(j => j.MetroAreaId).Distinct().ToList();
                var metroAreaLookup = allMetroAreaIds.Any()
                    ? (await _eventsContext.MetroAreas
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
                            .Select(id => new LankaConnect.Modules.Communications.Contracts.EmailGroupSummaryDto(
                                emailGroupLookup[id].Id, emailGroupLookup[id].Name, null,
                                Guid.Empty, 0, emailGroupLookup[id].IsActive, DateTime.UtcNow, null))
                            .ToList());

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
                        EmailGroupIds = emailGroupIdsByNewsletter.TryGetValue(newsletter.Id, out var egIds)
                            ? egIds : newsletter.EmailGroupIds,
                        EmailGroups = emailGroupDtosByNewsletter.TryGetValue(newsletter.Id, out var egDtos)
                            ? egDtos : new List<LankaConnect.Modules.Communications.Contracts.EmailGroupSummaryDto>(),
                        MetroAreaIds = metroAreaIdsByNewsletter.TryGetValue(newsletter.Id, out var maIds)
                            ? maIds : newsletter.MetroAreaIds,
                        MetroAreas = metroAreaDtosByNewsletter.TryGetValue(newsletter.Id, out var maDtos)
                            ? maDtos : new List<MetroAreaSummaryDto>(),
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
