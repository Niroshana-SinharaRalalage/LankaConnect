using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Badges.BackgroundJobs;

/// <summary>
/// Background job that runs daily to clean up expired badges
/// Phase 6A.27: Expired Badge Cleanup Job
///
/// Responsibilities:
/// 1. Find all expired badges (ExpiresAt < now)
/// 2. Remove expired badges from all events they're assigned to
/// 3. Optionally deactivate the expired badges
/// </summary>
public class ExpiredBadgeCleanupJob
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpiredBadgeCleanupJob> _logger;

    public ExpiredBadgeCleanupJob(
        IBadgeRepository badgeRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExpiredBadgeCleanupJob> logger)
    {
        _badgeRepository = badgeRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("ExpiredBadgeCleanupJob: Starting expired badge cleanup job execution at {Time}", DateTime.UtcNow);

        try
        {
            // 1. Get all expired badges
            var expiredBadges = await _badgeRepository.GetExpiredBadgesAsync();
            var expiredBadgesList = expiredBadges.ToList();

            if (!expiredBadgesList.Any())
            {
                _logger.LogInformation("ExpiredBadgeCleanupJob: No expired badges found. Job completed.");
                return;
            }

            _logger.LogInformation("ExpiredBadgeCleanupJob: Found {Count} expired badges to process", expiredBadgesList.Count);

            var totalEventsUpdated = 0;
            var totalBadgesDeactivated = 0;

            // 2. For each expired badge, remove it from all events
            foreach (var badge in expiredBadgesList)
            {
                try
                {
                    var eventsUpdated = await RemoveBadgeFromEventsAsync(badge);
                    totalEventsUpdated += eventsUpdated;

                    // 3. Deactivate the expired badge if it's still active
                    if (badge.IsActive)
                    {
                        badge.Deactivate();
                        _badgeRepository.Update(badge);
                        totalBadgesDeactivated++;
                        _logger.LogInformation("ExpiredBadgeCleanupJob: Deactivated expired badge {BadgeId} ({BadgeName})",
                            badge.Id, badge.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredBadgeCleanupJob: Error processing expired badge {BadgeId}", badge.Id);
                }
            }

            // 4. Commit all changes
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "ExpiredBadgeCleanupJob: Completed. Processed {BadgeCount} expired badges, updated {EventCount} events, deactivated {DeactivatedCount} badges",
                expiredBadgesList.Count, totalEventsUpdated, totalBadgesDeactivated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpiredBadgeCleanupJob: Fatal error during expired badge cleanup job execution");
        }
    }

    private async Task<int> RemoveBadgeFromEventsAsync(Badge badge)
    {
        var eventsUpdated = 0;

        try
        {
            // Get all events that have this badge assigned
            var eventsWithBadge = await _eventRepository.GetEventsWithBadgeAsync(badge.Id);

            if (!eventsWithBadge.Any())
            {
                _logger.LogDebug("ExpiredBadgeCleanupJob: No events found with badge {BadgeId}", badge.Id);
                return 0;
            }

            _logger.LogInformation("ExpiredBadgeCleanupJob: Found {Count} events with expired badge {BadgeId} ({BadgeName})",
                eventsWithBadge.Count, badge.Id, badge.Name);

            foreach (var @event in eventsWithBadge)
            {
                try
                {
                    var removeResult = @event.RemoveBadge(badge.Id);
                    if (removeResult.IsSuccess)
                    {
                        _eventRepository.Update(@event);
                        eventsUpdated++;
                        _logger.LogDebug("ExpiredBadgeCleanupJob: Removed expired badge {BadgeId} from event {EventId}",
                            badge.Id, @event.Id);
                    }
                    else
                    {
                        _logger.LogWarning("ExpiredBadgeCleanupJob: Failed to remove badge {BadgeId} from event {EventId}: {Errors}",
                            badge.Id, @event.Id, string.Join(", ", removeResult.Errors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredBadgeCleanupJob: Error removing badge {BadgeId} from event {EventId}",
                        badge.Id, @event.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpiredBadgeCleanupJob: Error getting events for badge {BadgeId}", badge.Id);
        }

        return eventsUpdated;
    }
}
