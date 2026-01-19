using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.AdminApproval;

public class RejectEventCommandHandler : ICommandHandler<RejectEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectEventCommandHandler> _logger;

    public RejectEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RejectEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RejectEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RejectEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("AdminId", request.RejectedByAdminId))
        using (LogContext.PushProperty("RejectionReason", request.Reason ?? "Not specified"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RejectEvent START: EventId={EventId}, AdminId={AdminId}, Reason={Reason}",
                request.EventId, request.RejectedByAdminId, request.Reason ?? "Not specified");

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RejectEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "RejectEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentStatus={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Use domain method to reject event
                var rejectResult = @event.Reject(request.RejectedByAdminId, request.Reason ?? "No reason provided");
                if (rejectResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RejectEvent FAILED: Domain validation failed - EventId={EventId}, AdminId={AdminId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.RejectedByAdminId, rejectResult.Error, stopwatch.ElapsedMilliseconds);

                    return rejectResult;
                }

                _logger.LogInformation(
                    "RejectEvent: Domain method succeeded - EventId={EventId}, NewStatus={Status}",
                    @event.Id, @event.Status);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RejectEvent COMPLETE: EventId={EventId}, AdminId={AdminId}, Reason={Reason}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, request.RejectedByAdminId, request.Reason ?? "Not specified", @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RejectEvent FAILED: Exception occurred - EventId={EventId}, AdminId={AdminId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.RejectedByAdminId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
