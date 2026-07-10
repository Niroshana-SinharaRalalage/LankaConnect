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

namespace LankaConnect.Modules.Forms.Application.Commands.ReopenEventForm;

public class ReopenEventFormCommandHandler : ICommandHandler<ReopenEventFormCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<ReopenEventFormCommandHandler> _logger;

    public ReopenEventFormCommandHandler(
        IFormRepository eventFormRepository,
        IMultiContextUnitOfWork unitOfWork,
        FormsDbContext formsContext,
        ILogger<ReopenEventFormCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ReopenEventFormCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ReopenEventForm"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ReopenEventForm START: FormId={FormId}, EventId={EventId}",
                request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReopenEventForm FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReopenEventForm FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                var reopenResult = form.Reopen();

                if (reopenResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReopenEventForm FAILED: Domain validation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, reopenResult.Error, stopwatch.ElapsedMilliseconds);
                    return reopenResult;
                }

                // Wave 6.5.d: multi-context commit (AppDbContext + FormsDbContext).
                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _unitOfWork.CommitAsync(new DbContext[] { _formsContext }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ReopenEventForm COMPLETE: FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ReopenEventForm FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
