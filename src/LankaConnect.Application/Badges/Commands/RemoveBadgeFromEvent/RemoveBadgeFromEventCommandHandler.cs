using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Badges.Commands.RemoveBadgeFromEvent;

/// <summary>
/// Handler for RemoveBadgeFromEventCommand
/// Phase 6A.25: Removes a badge from an event
/// </summary>
public class RemoveBadgeFromEventCommandHandler : IRequestHandler<RemoveBadgeFromEventCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveBadgeFromEventCommandHandler> _logger;

    public RemoveBadgeFromEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveBadgeFromEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveBadgeFromEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveBadgeFromEvent"))
        using (LogContext.PushProperty("EntityType", "EventBadge"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("BadgeId", request.BadgeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemoveBadgeFromEvent START: EventId={EventId}, BadgeId={BadgeId}",
                request.EventId, request.BadgeId);

            try
            {
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveBadgeFromEvent FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event ID is required");
                }

                if (request.BadgeId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveBadgeFromEvent FAILED: Invalid BadgeId - EventId={EventId}, BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.EventId, request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Badge ID is required");
                }

                // 1. Get event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveBadgeFromEvent FAILED: Event not found - EventId={EventId}, BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                        request.EventId, request.BadgeId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                // 2. Remove badge from event
                var removeResult = @event.RemoveBadge(request.BadgeId);
                if (!removeResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveBadgeFromEvent FAILED: Removal failed - EventId={EventId}, BadgeId={BadgeId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.EventId, request.BadgeId, string.Join(", ", removeResult.Errors), stopwatch.ElapsedMilliseconds);

                    return removeResult;
                }

                // 3. Save changes
                _eventRepository.Update(@event);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RemoveBadgeFromEvent COMPLETE: EventId={EventId}, BadgeId={BadgeId}, Duration={ElapsedMs}ms",
                    request.EventId, request.BadgeId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RemoveBadgeFromEvent FAILED: Exception occurred - EventId={EventId}, BadgeId={BadgeId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.BadgeId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
