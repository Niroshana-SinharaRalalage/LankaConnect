using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PromoteFromWaitingList;

/// <summary>
/// Handler for PromoteFromWaitingListCommand
/// Promotes user from waiting list to confirmed registration when capacity is available
/// </summary>
public class PromoteFromWaitingListCommandHandler : ICommandHandler<PromoteFromWaitingListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PromoteFromWaitingListCommandHandler> _logger;

    public PromoteFromWaitingListCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<PromoteFromWaitingListCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(PromoteFromWaitingListCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "PromoteFromWaitingList"))
        using (LogContext.PushProperty("EntityType", "WaitingListEntry"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PromoteFromWaitingList START: EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "PromoteFromWaitingList FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "PromoteFromWaitingList: Event loaded - EventId={EventId}, Title={Title}, Capacity={Capacity}, WaitingListCount={WaitingListCount}",
                    @event.Id, @event.Title.Value, @event.Capacity, @event.WaitingList.Count);

                // Use domain method to promote user from waiting list
                var promoteResult = @event.PromoteFromWaitingList(request.UserId);
                if (promoteResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "PromoteFromWaitingList FAILED: Domain validation failed - EventId={EventId}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, promoteResult.Error, stopwatch.ElapsedMilliseconds);

                    return promoteResult;
                }

                _logger.LogInformation(
                    "PromoteFromWaitingList: Domain method succeeded - EventId={EventId}, UserId={UserId}, NewWaitingListCount={WaitingListCount}",
                    @event.Id, request.UserId, @event.WaitingList.Count);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PromoteFromWaitingList COMPLETE: EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "PromoteFromWaitingList FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
