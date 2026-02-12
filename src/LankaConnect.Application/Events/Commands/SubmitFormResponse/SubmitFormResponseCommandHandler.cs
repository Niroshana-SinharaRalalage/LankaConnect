using System.Diagnostics;
using System.Security.Cryptography;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.SubmitFormResponse;

public class SubmitFormResponseCommandHandler : ICommandHandler<SubmitFormResponseCommand, SubmitFormResponseResult>
{
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitFormResponseCommandHandler> _logger;

    public SubmitFormResponseCommandHandler(
        IEventFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        IUnitOfWork unitOfWork,
        ILogger<SubmitFormResponseCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SubmitFormResponseResult>> Handle(SubmitFormResponseCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SubmitFormResponse"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SubmitFormResponse START: FormId={FormId}, EventId={EventId}, AnswerCount={AnswerCount}, HasEmail={HasEmail}, HasUserId={HasUserId}",
                request.FormId, request.EventId, request.Answers.Count,
                !string.IsNullOrEmpty(request.RespondentEmail), request.RespondentUserId.HasValue);

            try
            {
                // Load form with questions for validation
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(request.FormId, cancellationToken);

                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SubmitFormResponse FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result<SubmitFormResponseResult>.Failure($"Form with ID {request.FormId} not found");
                }

                if (form.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SubmitFormResponse FAILED: Form does not belong to event - FormId={FormId}, EventId={EventId}, Duration={ElapsedMs}ms",
                        request.FormId, request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<SubmitFormResponseResult>.Failure("Form does not belong to the specified event");
                }

                // Check if form is accepting responses
                if (!form.IsAcceptingResponses())
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SubmitFormResponse FAILED: Form not accepting responses - FormId={FormId}, Status={Status}, Duration={ElapsedMs}ms",
                        request.FormId, form.Status, stopwatch.ElapsedMilliseconds);
                    return Result<SubmitFormResponseResult>.Failure("This form is not currently accepting responses");
                }

                // Check MaxResponses limit
                if (form.MaxResponses.HasValue)
                {
                    var currentCount = await _formResponseRepository.GetCountByFormIdAsync(form.Id, cancellationToken);
                    if (currentCount >= form.MaxResponses.Value)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubmitFormResponse FAILED: Max responses reached - FormId={FormId}, CurrentCount={CurrentCount}, MaxResponses={MaxResponses}, Duration={ElapsedMs}ms",
                            request.FormId, currentCount, form.MaxResponses.Value, stopwatch.ElapsedMilliseconds);
                        return Result<SubmitFormResponseResult>.Failure("This form has reached the maximum number of responses");
                    }
                }

                // Check duplicate response if !AllowMultipleResponses
                if (!form.AllowMultipleResponses)
                {
                    if (request.RespondentUserId.HasValue)
                    {
                        var existingByUser = await _formResponseRepository.GetByFormAndUserAsync(
                            form.Id, request.RespondentUserId.Value, cancellationToken);
                        if (existingByUser != null)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "SubmitFormResponse FAILED: Duplicate response by user - FormId={FormId}, UserId={UserId}, Duration={ElapsedMs}ms",
                                request.FormId, request.RespondentUserId.Value, stopwatch.ElapsedMilliseconds);
                            return Result<SubmitFormResponseResult>.Failure("You have already submitted a response to this form");
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(request.RespondentEmail))
                    {
                        var existingByEmail = await _formResponseRepository.GetByFormAndEmailAsync(
                            form.Id, request.RespondentEmail.Trim(), cancellationToken);
                        if (existingByEmail != null)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "SubmitFormResponse FAILED: Duplicate response by email - FormId={FormId}, Email={Email}, Duration={ElapsedMs}ms",
                                request.FormId, request.RespondentEmail, stopwatch.ElapsedMilliseconds);
                            return Result<SubmitFormResponseResult>.Failure("A response with this email has already been submitted");
                        }
                    }
                }

                // Validate required questions are answered
                var requiredQuestions = form.Questions.Where(q => q.IsRequired).ToList();
                var answeredQuestionIds = request.Answers.Select(a => a.QuestionId).ToHashSet();

                foreach (var requiredQuestion in requiredQuestions)
                {
                    if (!answeredQuestionIds.Contains(requiredQuestion.Id))
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubmitFormResponse FAILED: Required question not answered - FormId={FormId}, QuestionId={QuestionId}, Duration={ElapsedMs}ms",
                            request.FormId, requiredQuestion.Id, stopwatch.ElapsedMilliseconds);
                        return Result<SubmitFormResponseResult>.Failure(
                            $"Required question '{requiredQuestion.QuestionText}' must be answered");
                    }
                }

                // Generate cryptographic access token
                var tokenBytes = RandomNumberGenerator.GetBytes(32);
                var accessToken = Convert.ToBase64String(tokenBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=');

                var tokenHash = ComputeSha256Hash(accessToken);

                // Create response
                var responseResult = FormResponse.Create(
                    form.Id,
                    form.EventId,
                    tokenHash,
                    request.RespondentEmail?.Trim(),
                    request.RespondentName?.Trim(),
                    request.RespondentUserId);

                if (responseResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SubmitFormResponse FAILED: Response creation failed - FormId={FormId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.FormId, responseResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<SubmitFormResponseResult>.Failure(responseResult.Error);
                }

                var response = responseResult.Value;

                // Add answers with question text snapshots
                var questionMap = form.Questions.ToDictionary(q => q.Id);

                foreach (var answerItem in request.Answers)
                {
                    if (!questionMap.TryGetValue(answerItem.QuestionId, out var question))
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubmitFormResponse FAILED: Question not found - FormId={FormId}, QuestionId={QuestionId}, Duration={ElapsedMs}ms",
                            request.FormId, answerItem.QuestionId, stopwatch.ElapsedMilliseconds);
                        return Result<SubmitFormResponseResult>.Failure(
                            $"Question with ID {answerItem.QuestionId} not found in this form");
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
                                _logger.LogWarning(
                                    "SubmitFormResponse FAILED: Option not found - FormId={FormId}, QuestionId={QuestionId}, OptionId={OptionId}, Duration={ElapsedMs}ms",
                                    request.FormId, answerItem.QuestionId, optionId, stopwatch.ElapsedMilliseconds);
                                return Result<SubmitFormResponseResult>.Failure(
                                    $"Option with ID {optionId} not found for question '{question.QuestionText}'");
                            }
                        }
                    }

                    var addAnswerResult = response.AddAnswer(
                        answerItem.QuestionId,
                        question.QuestionText,
                        answerItem.TextValue,
                        answerItem.SelectedOptionIds,
                        selectedOptionTextSnapshots,
                        answerItem.BooleanValue);

                    if (addAnswerResult.IsFailure)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubmitFormResponse FAILED: Answer creation failed - FormId={FormId}, QuestionId={QuestionId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.FormId, answerItem.QuestionId, addAnswerResult.Error, stopwatch.ElapsedMilliseconds);
                        return Result<SubmitFormResponseResult>.Failure(addAnswerResult.Error);
                    }
                }

                // Mark form as having responses (if first response)
                if (!form.HasResponses)
                {
                    form.MarkHasResponses();
                    _eventFormRepository.Update(form);
                }

                // Persist
                await _formResponseRepository.AddAsync(response, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "SubmitFormResponse COMPLETE: ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}, AnswerCount={AnswerCount}, Duration={ElapsedMs}ms",
                    response.Id, request.FormId, request.EventId, request.Answers.Count, stopwatch.ElapsedMilliseconds);

                return Result<SubmitFormResponseResult>.Success(
                    new SubmitFormResponseResult(response.Id, accessToken));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SubmitFormResponse FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
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
