using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.SubmitEventForApproval;

public class SubmitEventForApprovalCommandHandler : ICommandHandler<SubmitEventForApprovalCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitEventForApprovalCommandHandler> _logger;

    public SubmitEventForApprovalCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<SubmitEventForApprovalCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SubmitEventForApprovalCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SubmitEventForApproval"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("SubmitEventForApproval START: EventId={EventId}", request.EventId);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SubmitEventForApproval FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "SubmitEventForApproval: Event loaded - EventId={EventId}, Title={Title}, CurrentStatus={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Use domain method to submit for review
                var submitResult = @event.SubmitForReview();
                if (submitResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SubmitEventForApproval FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, submitResult.Error, stopwatch.ElapsedMilliseconds);

                    return submitResult;
                }

                _logger.LogInformation(
                    "SubmitEventForApproval: Domain method succeeded - EventId={EventId}, NewStatus={Status}",
                    @event.Id, @event.Status);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "SubmitEventForApproval COMPLETE: EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "SubmitEventForApproval FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
