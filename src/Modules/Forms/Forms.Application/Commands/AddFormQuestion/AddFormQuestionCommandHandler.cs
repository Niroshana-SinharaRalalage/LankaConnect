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

namespace LankaConnect.Modules.Forms.Application.Commands.AddFormQuestion;

public class AddFormQuestionCommandHandler : ICommandHandler<AddFormQuestionCommand, Guid>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<AddFormQuestionCommandHandler> _logger;

    public AddFormQuestionCommandHandler(
        IFormRepository eventFormRepository,
        FormsDbContext formsContext,
        ILogger<AddFormQuestionCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddFormQuestionCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddFormQuestion"))
        using (LogContext.PushProperty("EntityType", "FormQuestion"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddFormQuestion START: FormId={FormId}, EventId={EventId}, QuestionType={QuestionType}",
                request.FormId, request.EventId, request.QuestionType);

            try
            {
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AddFormQuestion FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AddFormQuestion FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure("Form does not belong to the specified event");
                }

                // Build options if provided
                List<QuestionOption>? options = null;
                if (request.Options != null && request.Options.Count > 0)
                {
                    options = new List<QuestionOption>();
                    foreach (var optionItem in request.Options)
                    {
                        var optionResult = QuestionOption.Create(optionItem.Text, optionItem.SortOrder);
                        if (optionResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "AddFormQuestion FAILED: Option creation failed - FormId={FormId}, OptionText={OptionText}, Error={Error}, Duration={ElapsedMs}ms",
                                request.FormId, optionItem.Text, optionResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result<Guid>.Failure(optionResult.Error);
                        }
                        options.Add(optionResult.Value);
                    }
                }

                var addResult = form.AddQuestion(
                    request.QuestionText,
                    request.QuestionType,
                    request.IsRequired,
                    request.SortOrder,
                    options,
                    request.HelpText);

                if (addResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "AddFormQuestion FAILED: Domain validation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, addResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(addResult.Error);
                }

                // Wave 8.5.h (D-01): direct-SaveChanges per Consult #25 Q6.
                await _eventFormRepository.UpdateAsync(form, cancellationToken);
                await _formsContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddFormQuestion COMPLETE: QuestionId={QuestionId}, FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    addResult.Value.Id, request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(addResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "AddFormQuestion FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
