using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using System.Security.Cryptography;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.UpdateFormResponse;

public class UpdateFormResponseCommandHandler : ICommandHandler<UpdateFormResponseCommand>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IEventRepository _eventRepository; // Phase 6A.114: Added for performance optimization
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<UpdateFormResponseCommandHandler> _logger;

    public UpdateFormResponseCommandHandler(
        IFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        IEventRepository eventRepository, // Phase 6A.114: Added for performance optimization
        IMultiContextUnitOfWork unitOfWork,
        FormsDbContext formsContext,
        ILogger<UpdateFormResponseCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _eventRepository = eventRepository; // Phase 6A.114: Added for performance optimization
        _unitOfWork = unitOfWork;
        _formsContext = formsContext;
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
                // Load response
                var response = await _formResponseRepository.GetByIdWithAnswersAsync(request.ResponseId, cancellationToken);

                if (response == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Response not found - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        request.ResponseId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Response with ID {request.ResponseId} not found");
                }

                // Phase 6A.106-110 Fix: PRIORITY-BASED AUTHENTICATION
                // Priority 1: If response has RespondentUserId (logged-in user) → ONLY userId can update (ignore token)
                // Priority 2: If response is anonymous (no userId) → ONLY token can update
                if (response.RespondentUserId.HasValue)
                {
                    // This response was submitted by a logged-in user
                    // ONLY that user can update it (token auth doesn't apply)
                    if (request.RequestingUserId == null || request.RequestingUserId != response.RespondentUserId)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "UpdateFormResponse FAILED: Unauthorized update attempt - Response belongs to user {OwnerId}, requester {RequesterId}, Duration={ElapsedMs}ms",
                            response.RespondentUserId, request.RequestingUserId ?? Guid.Empty, stopwatch.ElapsedMilliseconds);
                        return Result.Failure("You are not authorized to update this response");
                    }

                    _logger.LogInformation(
                        "UpdateFormResponse: Authenticated as logged-in user {UserId}",
                        request.RequestingUserId);
                }
                else
                {
                    // This response was submitted anonymously
                    // ONLY valid access token can update it
                    if (string.IsNullOrEmpty(request.AccessToken))
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "UpdateFormResponse FAILED: Unauthorized update attempt - Anonymous response requires access token, Duration={ElapsedMs}ms",
                            stopwatch.ElapsedMilliseconds);
                        return Result.Failure("Access token is required to update this response");
                    }

                    var tokenHash = ComputeSha256Hash(request.AccessToken);
                    if (response.AccessTokenHash != tokenHash)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "UpdateFormResponse FAILED: Invalid access token - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                            request.ResponseId, stopwatch.ElapsedMilliseconds);
                        return Result.Failure("Invalid access token");
                    }

                    _logger.LogInformation("UpdateFormResponse: Authenticated via access token");
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

                // Phase 6A.114: Load Event entity to pass to domain event
                // Performance optimization: Email handler will use this instead of re-querying
                var @event = await _eventRepository.GetByIdAsync(response.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        response.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Event with ID {response.EventId} not found");
                }

                _logger.LogInformation(
                    "UpdateFormResponse: Event loaded for domain event context - EventId={EventId}, EventTitle={EventTitle}",
                    @event.Id, @event.Title.Value);

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

                // Phase 6A.111: Add performance logging for answer updates
                var answerUpdateStopwatch = Stopwatch.StartNew();
                _logger.LogInformation(
                    "UpdateFormResponse: Starting answer updates - ResponseId={ResponseId}, AnswerCount={AnswerCount}",
                    request.ResponseId, request.Answers.Count);

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

                    // Phase 6A.115: Debug logging for Issue #2 (Number field not updating)
                    _logger.LogInformation(
                        "UpdateFormResponse: Processing answer - QuestionId={QuestionId}, QuestionText={QuestionText}, QuestionType={QuestionType}, TextValue={TextValue}, BooleanValue={BooleanValue}, OptionCount={OptionCount}",
                        answerItem.QuestionId, question.QuestionText, question.QuestionType,
                        answerItem.TextValue ?? "null", answerItem.BooleanValue?.ToString() ?? "null",
                        answerItem.SelectedOptionIds?.Count ?? 0);

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
                        // Phase 6A.115: Log existing value before update
                        _logger.LogInformation(
                            "UpdateFormResponse: Updating existing answer - QuestionId={QuestionId}, OldTextValue={OldTextValue}, NewTextValue={NewTextValue}",
                            answerItem.QuestionId, existingAnswer.TextValue ?? "null", answerItem.TextValue ?? "null");

                        var updateResult = response.UpdateAnswer(
                            answerItem.QuestionId,
                            answerItem.TextValue,
                            answerItem.SelectedOptionIds,
                            selectedOptionTextSnapshots,
                            answerItem.BooleanValue);

                        if (updateResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "UpdateFormResponse: Answer update FAILED - QuestionId={QuestionId}, QuestionText={QuestionText}, Error={Error}",
                                answerItem.QuestionId, question.QuestionText, updateResult.Error);
                            return Result.Failure(updateResult.Error);
                        }
                        else
                        {
                            // Phase 6A.115: Log successful update
                            _logger.LogInformation(
                                "UpdateFormResponse: Answer updated successfully - QuestionId={QuestionId}, QuestionText={QuestionText}",
                                answerItem.QuestionId, question.QuestionText);
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

                // Phase 6A.111: Log answer update performance
                answerUpdateStopwatch.Stop();
                _logger.LogInformation(
                    "UpdateFormResponse: Answer updates complete - ResponseId={ResponseId}, AnswerCount={AnswerCount}, Duration={ElapsedMs}ms",
                    request.ResponseId, request.Answers.Count, answerUpdateStopwatch.ElapsedMilliseconds);

                // Phase 6A.114: Raise updated event with full context (Form + Event)
                // Performance optimization: Pass already-loaded entities to email handler
                // Eliminates 2 duplicate queries (40s → 5-8s improvement)
                var raiseEventResult = response.RaiseUpdatedEventWithContext(form, @event);
                if (raiseEventResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateFormResponse WARNING: Failed to raise updated event - ResponseId={ResponseId}, Error={Error}",
                        request.ResponseId, raiseEventResult.Error);
                    // Continue anyway - email is supplementary, core update succeeded
                }
                else
                {
                    _logger.LogInformation(
                        "UpdateFormResponse: Domain event raised with pre-loaded context - ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}",
                        request.ResponseId, form.Id, @event.Id);
                }

                // Wave 6.5.d: multi-context commit (AppDbContext + FormsDbContext).
                await _formResponseRepository.UpdateAsync(response, cancellationToken);
                await _unitOfWork.CommitAsync(new DbContext[] { _formsContext }, cancellationToken);

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
