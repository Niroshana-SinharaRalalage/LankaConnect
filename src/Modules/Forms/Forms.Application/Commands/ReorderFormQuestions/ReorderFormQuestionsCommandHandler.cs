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

namespace LankaConnect.Modules.Forms.Application.Commands.ReorderFormQuestions;

public class ReorderFormQuestionsCommandHandler : ICommandHandler<ReorderFormQuestionsCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<ReorderFormQuestionsCommandHandler> _logger;

    public ReorderFormQuestionsCommandHandler(
        IFormRepository eventFormRepository,
        IMultiContextUnitOfWork unitOfWork,
        FormsDbContext formsContext,
        ILogger<ReorderFormQuestionsCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ReorderFormQuestionsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ReorderFormQuestions"))
        using (LogContext.PushProperty("EntityType", "FormQuestion"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ReorderFormQuestions START: FormId={FormId}, EventId={EventId}, QuestionCount={QuestionCount}",
                request.FormId, request.EventId, request.QuestionIdsInOrder.Count);

            try
            {
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReorderFormQuestions FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReorderFormQuestions FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                var reorderResult = form.ReorderQuestions(request.QuestionIdsInOrder);

                if (reorderResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReorderFormQuestions FAILED: Domain validation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, reorderResult.Error, stopwatch.ElapsedMilliseconds);
                    return reorderResult;
                }

                // Wave 6.5.d: multi-context commit (AppDbContext + FormsDbContext).
                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _unitOfWork.CommitAsync(new DbContext[] { _formsContext }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "ReorderFormQuestions COMPLETE: FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ReorderFormQuestions FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
