using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Badges.BackgroundJobs;

/// <summary>
/// Background job that runs daily to clean up expired badge assignments
/// Phase 6A.27: Original design for Badge-level expiration
/// Phase 6A.28: Redesigned for EventBadge-level expiration (duration-based)
///
/// New Responsibilities (Phase 6A.28):
/// 1. Find all events with expired EventBadge assignments (EventBadge.ExpiresAt &lt; now)
/// 2. Remove expired EventBadge assignments from events
/// 3. Badge entities themselves no longer expire - only assignments do
/// </summary>
public class ExpiredBadgeCleanupJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpiredBadgeCleanupJob> _logger;

    public ExpiredBadgeCleanupJob(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExpiredBadgeCleanupJob> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("ExpiredBadgeCleanupJob: Starting expired badge assignment cleanup at {Time}", DateTime.UtcNow);

        try
        {
            // 1. Get all events with at least one expired badge assignment
            var eventsWithExpiredBadges = await _eventRepository.GetEventsWithExpiredBadgesAsync();

            if (!eventsWithExpiredBadges.Any())
            {
                _logger.LogInformation("ExpiredBadgeCleanupJob: No expired badge assignments found. Job completed.");
                return;
            }

            _logger.LogInformation("ExpiredBadgeCleanupJob: Found {Count} events with expired badge assignments", eventsWithExpiredBadges.Count);

            var totalAssignmentsRemoved = 0;
            var totalEventsUpdated = 0;

            // 2. For each event, remove all expired badge assignments
            foreach (var @event in eventsWithExpiredBadges)
            {
                try
                {
                    var removedCount = RemoveExpiredBadgesFromEvent(@event);
                    if (removedCount > 0)
                    {
                        _eventRepository.Update(@event);
                        totalAssignmentsRemoved += removedCount;
                        totalEventsUpdated++;
                        _logger.LogInformation("ExpiredBadgeCleanupJob: Removed {Count} expired badge assignments from event {EventId}",
                            removedCount, @event.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredBadgeCleanupJob: Error processing event {EventId}", @event.Id);
                }
            }

            // 3. Commit all changes
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "ExpiredBadgeCleanupJob: Completed. Removed {AssignmentCount} expired badge assignments from {EventCount} events",
                totalAssignmentsRemoved, totalEventsUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpiredBadgeCleanupJob: Fatal error during expired badge assignment cleanup");
        }
    }

    private int RemoveExpiredBadgesFromEvent(Event @event)
    {
        var removedCount = 0;

        // Get all expired badge assignments for this event
        var expiredAssignments = @event.Badges
            .Where(eb => eb.IsExpired())
            .Select(eb => eb.BadgeId)
            .ToList();

        foreach (var badgeId in expiredAssignments)
        {
            try
            {
                var removeResult = @event.RemoveBadge(badgeId);
                if (removeResult.IsSuccess)
                {
                    removedCount++;
                }
                else
                {
                    _logger.LogWarning("ExpiredBadgeCleanupJob: Failed to remove badge {BadgeId} from event {EventId}: {Errors}",
                        badgeId, @event.Id, string.Join(", ", removeResult.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExpiredBadgeCleanupJob: Error removing badge {BadgeId} from event {EventId}",
                    badgeId, @event.Id);
            }
        }

        return removedCount;
    }
}
