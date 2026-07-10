using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.DeleteEventForm;

public class DeleteEventFormCommandHandler : ICommandHandler<DeleteEventFormCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<DeleteEventFormCommandHandler> _logger;

    public DeleteEventFormCommandHandler(
        IFormRepository eventFormRepository,
        IMultiContextUnitOfWork unitOfWork,
        FormsDbContext formsContext,
        ILogger<DeleteEventFormCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteEventFormCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteEventForm"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteEventForm START: FormId={FormId}, EventId={EventId}",
                request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteEventForm FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteEventForm FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                if (form.HasResponses)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteEventForm FAILED: Form has responses - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Cannot delete a form that has responses. Close the form instead.");
                }

                // Wave 6.5.d: multi-context commit (AppDbContext + FormsDbContext).
                await _eventFormRepository.DeleteAsync(form, cancellationToken);
                await _unitOfWork.CommitAsync(new DbContext[] { _formsContext }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteEventForm COMPLETE: FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteEventForm FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
