using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.AdminApproval;

public class ApproveEventCommandHandler : ICommandHandler<ApproveEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveEventCommandHandler> _logger;

    public ApproveEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ApproveEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ApproveEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("AdminId", request.ApprovedByAdminId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ApproveEvent START: EventId={EventId}, AdminId={AdminId}",
                request.EventId, request.ApprovedByAdminId);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ApproveEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "ApproveEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentStatus={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Use domain method to approve event
                var approveResult = @event.Approve(request.ApprovedByAdminId);
                if (approveResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "ApproveEvent FAILED: Domain validation failed - EventId={EventId}, AdminId={AdminId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.ApprovedByAdminId, approveResult.Error, stopwatch.ElapsedMilliseconds);

                    return approveResult;
                }

                _logger.LogInformation(
                    "ApproveEvent: Domain method succeeded - EventId={EventId}, NewStatus={Status}",
                    @event.Id, @event.Status);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ApproveEvent COMPLETE: EventId={EventId}, AdminId={AdminId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, request.ApprovedByAdminId, @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "ApproveEvent FAILED: Exception occurred - EventId={EventId}, AdminId={AdminId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.ApprovedByAdminId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
