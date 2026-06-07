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

namespace LankaConnect.Application.Events.Commands.DeleteFormQuestion;

public class DeleteFormQuestionCommandHandler : ICommandHandler<DeleteFormQuestionCommand>
{
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteFormQuestionCommandHandler> _logger;

    public DeleteFormQuestionCommandHandler(
        IEventFormRepository eventFormRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteFormQuestionCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteFormQuestionCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteFormQuestion"))
        using (LogContext.PushProperty("EntityType", "FormQuestion"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("QuestionId", request.QuestionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteFormQuestion START: QuestionId={QuestionId}, FormId={FormId}, EventId={EventId}",
                request.QuestionId, request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormQuestion FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormQuestion FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                var removeResult = form.RemoveQuestion(request.QuestionId);

                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormQuestion FAILED: Domain validation failed - FormId={FormId}, QuestionId={QuestionId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, request.QuestionId, removeResult.Error, stopwatch.ElapsedMilliseconds);
                    return removeResult;
                }

                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteFormQuestion COMPLETE: QuestionId={QuestionId}, FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.QuestionId, request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteFormQuestion FAILED: Exception - FormId={FormId}, QuestionId={QuestionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, request.QuestionId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
