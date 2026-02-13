using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Sends update notification email when a form response is modified.
/// Phase 6A.107: Email notification confirming changes with edit link.
///
/// FAIL-SILENT PATTERN: Email failures are logged but don't throw exceptions
/// to prevent transaction rollback.
///
/// Architect Review: Approved - mirrors signup commitment update pattern.
/// </summary>
public class FormResponseUpdatedEmailHandler : INotificationHandler<DomainEventNotification<FormResponseUpdatedEvent>>
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IEventFormRepository _eventFormRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<FormResponseUpdatedEmailHandler> _logger;

    public FormResponseUpdatedEmailHandler(
        IFormResponseRepository formResponseRepository,
        IEventFormRepository eventFormRepository,
        IEventRepository eventRepository,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        ILogger<FormResponseUpdatedEmailHandler> logger)
    {
        _formResponseRepository = formResponseRepository;
        _eventFormRepository = eventFormRepository;
        _eventRepository = eventRepository;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<FormResponseUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "FormResponseUpdatedEmail"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", domainEvent.FormId))
        using (LogContext.PushProperty("ResponseId", domainEvent.ResponseId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "FormResponseUpdatedEmail START: FormId={FormId}, ResponseId={ResponseId}",
                domainEvent.FormId, domainEvent.ResponseId);

            try
            {
                // Load response with answers
                var response = await _formResponseRepository.GetByIdWithAnswersAsync(domainEvent.ResponseId, cancellationToken);
                if (response == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseUpdatedEmail FAILED: Response not found - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        domainEvent.ResponseId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Skip if no email provided
                if (string.IsNullOrWhiteSpace(response.RespondentEmail))
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "FormResponseUpdatedEmail SKIPPED: No respondent email, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load form with questions
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(domainEvent.FormId, cancellationToken);
                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseUpdatedEmail FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        domainEvent.FormId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load event
                var eventEntity = await _eventRepository.GetByIdAsync(response.EventId, cancellationToken);
                if (eventEntity == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseUpdatedEmail FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        response.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Build response summary
                var responseSummary = BuildResponseSummary(response.Answers, form.Questions, maxQuestions: 5, maxAnswerLength: 100);

                // Build edit URL (no token available in update event - user must use original email or be logged in)
                var editUrl = $"{_emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id).Replace("/details", "")}/events/{response.EventId}/forms/{domainEvent.FormId}";

                // Create email parameters
                var emailParams = FormResponseEmailParams.CreateUpdate(
                    userName: response.RespondentName ?? "User",
                    userEmail: response.RespondentEmail,
                    eventId: eventEntity.Id,
                    eventTitle: eventEntity.Title.Value,
                    formTitle: form.Title,
                    responseSummary: responseSummary,
                    editFormUrl: editUrl,
                    eventStartDate: eventEntity.StartDate,
                    timeZoneId: eventEntity.TimeZoneId,
                    eventLocation: eventEntity.Location?.ToString() ?? "TBA",
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id),
                    updatedAt: domainEvent.OccurredAt
                );

                // Add optional fields
                if (!string.IsNullOrWhiteSpace(form.Description))
                {
                    emailParams.WithFormDescription(form.Description);
                }

                if (form.ResponseDeadline.HasValue)
                {
                    emailParams.WithResponseDeadline(form.ResponseDeadline);
                }

                // Get primary image from Images collection
                var primaryImage = eventEntity.Images.FirstOrDefault(i => i.IsPrimary);
                var imageUrl = primaryImage?.ImageUrl ?? eventEntity.Images.FirstOrDefault()?.ImageUrl ?? "";
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    emailParams.WithEventImage(imageUrl);
                }

                if (!string.IsNullOrWhiteSpace(eventEntity.OrganizerContactName))
                {
                    emailParams.WithOrganizerContact(
                        eventEntity.OrganizerContactName,
                        eventEntity.OrganizerContactEmail,
                        eventEntity.OrganizerContactPhone);
                }

                // Send email (fail-silent)
                var emailResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (emailResult.Success)
                {
                    _logger.LogInformation(
                        "FormResponseUpdatedEmail SUCCESS: Email sent to {RecipientEmail}, Duration={ElapsedMs}ms",
                        response.RespondentEmail, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "FormResponseUpdatedEmail FAILED: Email send failed - RecipientEmail={RecipientEmail}, Error={Error}, Duration={ElapsedMs}ms",
                        response.RespondentEmail, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // FAIL-SILENT: Log error but don't throw
                _logger.LogError(ex,
                    "FormResponseUpdatedEmail EXCEPTION: Unexpected error - FormId={FormId}, ResponseId={ResponseId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.FormId, domainEvent.ResponseId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }

    private string BuildResponseSummary(
        IReadOnlyList<FormAnswer> answers,
        IReadOnlyList<FormQuestion> questions,
        int maxQuestions = 5,
        int maxAnswerLength = 100)
    {
        if (!answers.Any())
            return "No responses provided.";

        var questionMap = questions.ToDictionary(q => q.Id, q => q.QuestionText);
        var displayedAnswers = answers.Take(maxQuestions);

        var summaryParts = displayedAnswers.Select(answer =>
        {
            var questionText = questionMap.TryGetValue(answer.FormQuestionId, out var qText)
                ? qText
                : "Question";

            var answerText = answer.TextValue ??
                            string.Join(", ", answer.SelectedOptionTextSnapshots ?? new List<string>()) ??
                            answer.BooleanValue?.ToString() ?? "";

            if (answerText.Length > maxAnswerLength)
                answerText = $"{answerText.Substring(0, maxAnswerLength)}...";

            return $"{questionText}: {answerText}";
        });

        var summary = string.Join(" | ", summaryParts);

        var remainingCount = answers.Count - maxQuestions;
        if (remainingCount > 0)
            summary += $" | ... and {remainingCount} more response{(remainingCount > 1 ? "s" : "")}";

        return summary;
    }
}
