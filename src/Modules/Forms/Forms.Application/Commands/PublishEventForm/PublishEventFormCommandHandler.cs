using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.PublishEventForm;

public class PublishEventFormCommandHandler : ICommandHandler<PublishEventFormCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<PublishEventFormCommandHandler> _logger;

    public PublishEventFormCommandHandler(
        IFormRepository eventFormRepository,
        IMultiContextUnitOfWork unitOfWork,
        FormsDbContext formsContext,
        ILogger<PublishEventFormCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result> Handle(PublishEventFormCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "PublishEventForm"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PublishEventForm START: FormId={FormId}, EventId={EventId}",
                request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishEventForm FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishEventForm FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                var publishResult = form.Publish();

                if (publishResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "PublishEventForm FAILED: Domain validation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, publishResult.Error, stopwatch.ElapsedMilliseconds);
                    return publishResult;
                }

                // Wave 6.5.d: multi-context commit (AppDbContext + FormsDbContext).
                // TODO Wave 6.5.d-followup: emit FormPublishedIntegrationEventV1 via the
                // outbox (Forms.Contracts wire ABI). Deferred from this slice so 6.5.d
                // ships as a pure self-save retirement + multi-context UoW migration;
                // integration-event contracts land in a follow-up alongside their
                // downstream consumer.
                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _unitOfWork.CommitAsync(new DbContext[] { _formsContext }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "PublishEventForm COMPLETE: FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "PublishEventForm FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
