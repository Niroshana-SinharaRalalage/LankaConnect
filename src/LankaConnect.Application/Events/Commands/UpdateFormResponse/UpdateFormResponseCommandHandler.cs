using System.Diagnostics;
using System.Security.Cryptography;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateFormResponse;

public class UpdateFormResponseCommandHandler : ICommandHandler<UpdateFormResponseCommand>
{
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateFormResponseCommandHandler> _logger;

    public UpdateFormResponseCommandHandler(
        IEventFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateFormResponseCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateFormResponseCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateFormResponse"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("ResponseId", request.ResponseId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateFormResponse START: ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}",
                request.ResponseId, request.FormId, request.EventId);

            try
            {
                // Authenticate via access token
                var tokenHash = ComputeSha256Hash(request.AccessToken);
                var response = await _formResponseRepository.GetByIdWithAnswersAsync(request.ResponseId, cancellationToken);

                if (response == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Response not found - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        request.ResponseId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Response with ID {request.ResponseId} not found");
                }

                if (response.AccessTokenHash != tokenHash)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Invalid access token - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        request.ResponseId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Invalid access token");
                }

                if (response.EventFormId != request.FormId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Response does not belong to form - ResponseId={ResponseId}, FormId={FormId}, Duration={ElapsedMs}ms",
                        request.ResponseId, request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Response does not belong to the specified form");
                }

                // Load form to check deadline
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);
                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Form with ID {request.FormId} not found");
                }

                // Check edit deadline
                if (!response.CanEdit(form.ResponseDeadline))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Edit deadline passed - ResponseId={ResponseId}, Deadline={Deadline}, Duration={ElapsedMs}ms",
                        request.ResponseId, form.ResponseDeadline, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("The response deadline has passed. You can no longer edit your response.");
                }

                // Update answers
                var questionMap = form.Questions.ToDictionary(q => q.Id);

                foreach (var answerItem in request.Answers)
                {
                    if (!questionMap.TryGetValue(answerItem.QuestionId, out var question))
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "UpdateFormResponse FAILED: Question not found - ResponseId={ResponseId}, QuestionId={QuestionId}, Duration={ElapsedMs}ms",
                            request.ResponseId, answerItem.QuestionId, stopwatch.ElapsedMilliseconds);
                        return Result.Failure($"Question with ID {answerItem.QuestionId} not found in this form");
                    }

                    // Snapshot option texts for choice-type answers
                    List<string>? selectedOptionTextSnapshots = null;
                    if (answerItem.SelectedOptionIds != null && answerItem.SelectedOptionIds.Count > 0)
                    {
                        var optionMap = question.Options.ToDictionary(o => o.Id, o => o.Text);
                        selectedOptionTextSnapshots = new List<string>();
                        foreach (var optionId in answerItem.SelectedOptionIds)
                        {
                            if (optionMap.TryGetValue(optionId, out var optionText))
                            {
                                selectedOptionTextSnapshots.Add(optionText);
                            }
                            else
                            {
                                stopwatch.Stop();
                                return Result.Failure($"Option with ID {optionId} not found for question '{question.QuestionText}'");
                            }
                        }
                    }

                    var existingAnswer = response.GetAnswer(answerItem.QuestionId);
                    if (existingAnswer != null)
                    {
                        var updateResult = response.UpdateAnswer(
                            answerItem.QuestionId,
                            answerItem.TextValue,
                            answerItem.SelectedOptionIds,
                            selectedOptionTextSnapshots,
                            answerItem.BooleanValue);

                        if (updateResult.IsFailure)
                        {
                            stopwatch.Stop();
                            return Result.Failure(updateResult.Error);
                        }
                    }
                    else
                    {
                        // New answer for a question not previously answered
                        var addResult = response.AddAnswer(
                            answerItem.QuestionId,
                            question.QuestionText,
                            answerItem.TextValue,
                            answerItem.SelectedOptionIds,
                            selectedOptionTextSnapshots,
                            answerItem.BooleanValue);

                        if (addResult.IsFailure)
                        {
                            stopwatch.Stop();
                            return Result.Failure(addResult.Error);
                        }
                    }
                }

                _formResponseRepository.Update(response);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateFormResponse COMPLETE: ResponseId={ResponseId}, FormId={FormId}, AnswerCount={AnswerCount}, Duration={ElapsedMs}ms",
                    request.ResponseId, request.FormId, request.Answers.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateFormResponse FAILED: Exception - ResponseId={ResponseId}, FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.ResponseId, request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
