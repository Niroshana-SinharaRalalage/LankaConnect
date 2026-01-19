using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RemoveSignUpListFromEvent;

public class RemoveSignUpListFromEventCommandHandler : ICommandHandler<RemoveSignUpListFromEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveSignUpListFromEventCommandHandler> _logger;

    public RemoveSignUpListFromEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveSignUpListFromEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveSignUpListFromEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveSignUpListFromEvent"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "RemoveSignUpListFromEvent START: EventId={EventId}, SignUpListId={SignUpListId}",
                request.EventId, request.SignUpListId);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpListFromEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "RemoveSignUpListFromEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentSignUpListCount={SignUpListCount}",
                    @event.Id, @event.Title.Value, @event.SignUpLists.Count);

                // Remove sign-up list
                var removeResult = @event.RemoveSignUpList(request.SignUpListId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpListFromEvent FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, removeResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(removeResult.Error);
                }

                _logger.LogInformation(
                    "RemoveSignUpListFromEvent: Domain method succeeded - EventId={EventId}, SignUpListId={SignUpListId}, NewSignUpListCount={SignUpListCount}",
                    @event.Id, request.SignUpListId, @event.SignUpLists.Count);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RemoveSignUpListFromEvent COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RemoveSignUpListFromEvent FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
