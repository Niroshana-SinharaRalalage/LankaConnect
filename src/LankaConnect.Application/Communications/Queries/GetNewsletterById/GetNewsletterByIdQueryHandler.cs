using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Queries.GetNewsletterById;

/// <summary>
/// Handler for GetNewsletterByIdQuery
/// Phase 6A.74: Newsletter retrieval
/// </summary>
public class GetNewsletterByIdQueryHandler : IQueryHandler<GetNewsletterByIdQuery, NewsletterDto>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetNewsletterByIdQueryHandler> _logger;

    public GetNewsletterByIdQueryHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        ILogger<GetNewsletterByIdQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<NewsletterDto>> Handle(GetNewsletterByIdQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetNewsletterById"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetNewsletterById START: NewsletterId={NewsletterId}, UserId={UserId}",
                request.Id, _currentUserService.UserId);

            try
            {
                // Validate request
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNewsletterById FAILED: Invalid NewsletterId - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result<NewsletterDto>.Failure("Newsletter ID is required");
                }

                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);

                if (newsletter == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNewsletterById FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result<NewsletterDto>.Failure("Newsletter not found");
                }

                // Authorization: Only creator or admin can view
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNewsletterById FAILED: Access denied - NewsletterId={NewsletterId}, RequestingUserId={UserId}, CreatorId={CreatorId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id, _currentUserService.UserId, newsletter.CreatedByUserId, _currentUserService.IsAdmin, stopwatch.ElapsedMilliseconds);

                    return Result<NewsletterDto>.Failure("You do not have permission to view this newsletter");
                }

                // Phase 6A.74 Part 13 Issue #1 FIX: Get recipient counts from NewsletterEmailHistory
                var dbContext = _dbContext as DbContext;
                NewsletterEmailHistory? history = null;

                if (dbContext != null)
                {
                    history = await dbContext.Set<NewsletterEmailHistory>()
                        .FirstOrDefaultAsync(h => h.NewsletterId == newsletter.Id, cancellationToken);
                }

                var dto = new NewsletterDto
                {
                    Id = newsletter.Id,
                    Title = newsletter.Title.Value,
                    Description = newsletter.Description.Value,
                    CreatedByUserId = newsletter.CreatedByUserId,
                    CreatedByUserName = string.Empty, // Populated by controller if needed
                    EventId = newsletter.EventId,
                    EventTitle = null, // Populated by controller if needed
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
                    EmailGroups = new List<EmailGroupSummaryDto>(), // Populated by controller if needed
                    MetroAreaIds = newsletter.MetroAreaIds,
                    MetroAreas = new List<MetroAreaSummaryDto>(), // Populated by controller if needed
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

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetNewsletterById COMPLETE: NewsletterId={NewsletterId}, Title={Title}, Status={Status}, TotalRecipients={RecipientCount}, HasHistory={HasHistory}, Duration={ElapsedMs}ms",
                    request.Id, newsletter.Title.Value, newsletter.Status, history?.TotalRecipientCount ?? 0, history != null, stopwatch.ElapsedMilliseconds);

                return Result<NewsletterDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetNewsletterById FAILED: Exception occurred - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
