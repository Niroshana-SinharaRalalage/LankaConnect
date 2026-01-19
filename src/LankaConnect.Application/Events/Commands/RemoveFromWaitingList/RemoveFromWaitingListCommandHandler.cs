using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RemoveFromWaitingList;

/// <summary>
/// Handler for RemoveFromWaitingListCommand
/// Removes user from event waiting list and resequences positions
/// </summary>
public class RemoveFromWaitingListCommandHandler : ICommandHandler<RemoveFromWaitingListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveFromWaitingListCommandHandler> _logger;

    public RemoveFromWaitingListCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveFromWaitingListCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveFromWaitingListCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveFromWaitingList"))
        using (LogContext.PushProperty("EntityType", "WaitingListEntry"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemoveFromWaitingList START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveFromWaitingList FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "RemoveFromWaitingList: Event loaded - EventId={EventId}, Title={Title}, WaitingListCount={WaitingListCount}",
                    @event.Id, @event.Title.Value, @event.WaitingList.Count);

                // Use domain method to remove user from waiting list
                var removeResult = @event.RemoveFromWaitingList(request.UserId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveFromWaitingList FAILED: Domain validation failed - EventId={EventId}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, removeResult.Error, stopwatch.ElapsedMilliseconds);

                    return removeResult;
                }

                _logger.LogInformation(
                    "RemoveFromWaitingList: Domain method succeeded - EventId={EventId}, UserId={UserId}, NewWaitingListCount={WaitingListCount}",
                    @event.Id, request.UserId, @event.WaitingList.Count);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RemoveFromWaitingList COMPLETE: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RemoveFromWaitingList FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
