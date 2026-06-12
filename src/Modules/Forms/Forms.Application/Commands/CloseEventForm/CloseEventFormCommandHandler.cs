using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.CloseEventForm;

public class CloseEventFormCommandHandler : ICommandHandler<CloseEventFormCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CloseEventFormCommandHandler> _logger;

    public CloseEventFormCommandHandler(
        IFormRepository eventFormRepository,
        IUnitOfWork unitOfWork,
        ILogger<CloseEventFormCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CloseEventFormCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CloseEventForm"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CloseEventForm START: FormId={FormId}, EventId={EventId}",
                request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CloseEventForm FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CloseEventForm FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                var closeResult = form.Close();

                if (closeResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CloseEventForm FAILED: Domain validation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, closeResult.Error, stopwatch.ElapsedMilliseconds);
                    return closeResult;
                }

                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CloseEventForm COMPLETE: FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CloseEventForm FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
