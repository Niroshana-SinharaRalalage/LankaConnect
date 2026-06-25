using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace LankaConnect.Modules.Communications.Application.Queries.GetNewslettersByCreator;

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
        using (LogContext.PushProperty("Operation", "GetNewslettersByCreator"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("CreatorUserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetNewslettersByCreator START: CreatorUserId={UserId}",
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
                        EmailGroupIds = emailGroupIdsByNewsletter.TryGetValue(newsletter.Id, out var egIds)
                            ? egIds : newsletter.EmailGroupIds,
                        EmailGroups = emailGroupDtosByNewsletter.TryGetValue(newsletter.Id, out var egDtos)
                            ? egDtos : new List<EmailGroupSummaryDto>(),
                        MetroAreaIds = metroAreaIdsByNewsletter.TryGetValue(newsletter.Id, out var maIds)
                            ? maIds : newsletter.MetroAreaIds,
                        MetroAreas = metroAreaDtosByNewsletter.TryGetValue(newsletter.Id, out var maDtos)
                            ? maDtos : new List<MetroAreaSummaryDto>(),
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
                    "GetNewslettersByCreator COMPLETE: CreatorUserId={UserId}, NewsletterCount={Count}, WithHistoryCount={HistoryCount}, Duration={ElapsedMs}ms",
                    _currentUserService.UserId, dtoList.Count, historyRecords.Count, stopwatch.ElapsedMilliseconds);

                return Result<List<NewsletterDto>>.Success(dtoList);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetNewslettersByCreator FAILED: Exception occurred - CreatorUserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    _currentUserService.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
