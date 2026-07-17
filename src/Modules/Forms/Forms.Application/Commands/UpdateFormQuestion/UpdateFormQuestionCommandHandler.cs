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
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.UpdateFormQuestion;

public class UpdateFormQuestionCommandHandler : ICommandHandler<UpdateFormQuestionCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<UpdateFormQuestionCommandHandler> _logger;

    public UpdateFormQuestionCommandHandler(
        IFormRepository eventFormRepository,
        FormsDbContext formsContext,
        ILogger<UpdateFormQuestionCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateFormQuestionCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateFormQuestion"))
        using (LogContext.PushProperty("EntityType", "FormQuestion"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("QuestionId", request.QuestionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateFormQuestion START: QuestionId={QuestionId}, FormId={FormId}, EventId={EventId}",
                request.QuestionId, request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormQuestion FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormQuestion FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Form does not belong to the specified event");
                }

                // Build options if provided
                List<QuestionOption>? options = null;
                if (request.Options != null && request.Options.Count > 0)
                {
                    options = new List<QuestionOption>();
                    foreach (var optionItem in request.Options)
                    {
                        var optionResult = optionItem.Id.HasValue
                            ? QuestionOption.CreateWithId(optionItem.Id.Value, optionItem.Text, optionItem.SortOrder)
                            : QuestionOption.Create(optionItem.Text, optionItem.SortOrder);

                        if (optionResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "UpdateFormQuestion FAILED: Option creation failed - FormId={FormId}, QuestionId={QuestionId}, Error={Error}, Duration={ElapsedMs}ms",
                                request.FormId, request.QuestionId, optionResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result.Failure(optionResult.Error);
                        }
                        options.Add(optionResult.Value);
                    }
                }

                var updateResult = form.UpdateQuestion(
                    request.QuestionId,
                    request.QuestionText,
                    request.QuestionType,
                    request.IsRequired,
                    request.SortOrder,
                    options,
                    request.HelpText);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormQuestion FAILED: Domain validation failed - FormId={FormId}, QuestionId={QuestionId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, request.QuestionId, updateResult.Error, stopwatch.ElapsedMilliseconds);
                    return updateResult;
                }

                // Wave 8.5.h (D-01): direct-SaveChanges per Consult #25 Q6.
                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _formsContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateFormQuestion COMPLETE: QuestionId={QuestionId}, FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.QuestionId, request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateFormQuestion FAILED: Exception - FormId={FormId}, QuestionId={QuestionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, request.QuestionId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
