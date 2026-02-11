using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateEventForm;

public class UpdateEventFormCommandHandler : ICommandHandler<UpdateEventFormCommand>
{
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEventFormCommandHandler> _logger;

    public UpdateEventFormCommandHandler(
        IEventFormRepository eventFormRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEventFormCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateEventFormCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEventForm"))
        using (LogContext.PushProperty("EntityType", "EventForm"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEventForm START: FormId={FormId}, EventId={EventId}, Title={Title}",
                request.FormId, request.EventId, request.Title);

            try
            {
                var form = await _eventFormRepository.GetByIdAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEventForm FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEventForm FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, FormEventId={FormEventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, form.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                var updateResult = form.UpdateDetails(
                    request.Title,
                    request.Description,
                    request.AllowMultipleResponses,
                    request.ResponseDeadline,
                    request.MaxResponses);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEventForm FAILED: Domain validation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, updateResult.Error, stopwatch.ElapsedMilliseconds);
                    return updateResult;
                }

                _eventFormRepository.Update(form);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateEventForm COMPLETE: FormId={FormId}, EventId={EventId}, Title={Title}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, request.Title, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateEventForm FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
